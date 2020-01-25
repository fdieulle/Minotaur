using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Minotaur.Codecs;
using Minotaur.Core;
using Minotaur.Cursors;
using Minotaur.IO;
using Minotaur.Meta;
using Minotaur.Native;
using Minotaur.Providers;
using Minotaur.Recorders;
using Minotaur.Streams;

namespace Minotaur.Db
{
    public class FileTimeSeriesDb : ITimeSeriesDb, ITimeSeriesDbUpdater
    {
        private const int STREAM_CAPACITY = 8192;
        private const int MB = 1000000;
        private const int OPTIMAL_FILE_SIZE_MB = (500 * MB) / STREAM_CAPACITY * STREAM_CAPACITY;
        private const int OPTIMAL_FILE_BLOCK_SLICE = (16 * MB) / STREAM_CAPACITY * STREAM_CAPACITY;

        private readonly IFilePathProvider _filePathProvider;
        private readonly IAllocator _allocator;
        private readonly Schema _schema;
        private readonly ColumnStreamFactory<MinotaurFileStream> _columnFactory = new ColumnStreamFactory<MinotaurFileStream>();

        public FileTimeSeriesDb(IFilePathProvider filePathProvider, IAllocator allocator)
        {
            _filePathProvider = filePathProvider;
            _allocator = allocator;
            _schema = new Schema(filePathProvider);
        }

        #region Implementation of ITimeSeriesDb

        public ICursor GetCursor(string symbol, DateTime start, DateTime? end = null, string[] columns = null)
        {
            end = end ?? start.AddDays(1);

            var cursors = new Dictionary<string, IColumnCursor>();
            using (_schema.OpenToRead(symbol, out var meta))
            {
                foreach (var column in meta.GetColumns(columns))
                {
                    var stream = CreateReader(symbol, column, start, end.Value);
                    cursors[column.Name] = _columnFactory.CreateCursor(column, stream, _allocator);
                }
            }

            return new TimeSeriesCursor(cursors);
        }

        public ITimeSeriesRecorder CreateRecorder(string symbol) 
            => new TimeSeriesRecorder(symbol, this, _allocator);

        public void Insert(string symbol, Dictionary<string, Array> data)
        {
            data = data.ToDictionary(p => p.Key, p => p.Value); // Clone

            if (data.TryGetValue("timestamp", out var timestamps))
            {
                data.Remove("timestamp");

                DateTime[] timeline;
                if (timestamps is DateTime[] times)
                    timeline = times;
                else if (timestamps is long[] longs)
                    timeline = longs.Select(p => new DateTime(p)).ToArray();
                else throw new InvalidDataException($"The timestamp column has to be type of DateTime[] or long[]");

                // Sanity check
                var columns = new List<IArrayRecorder>();
                foreach (var pair in data)
                {
                    if(pair.Value.Length != timeline.Length)
                        throw new InvalidDataException($"The number of rows for the column {pair.Key} has to be equals to the number of timestamps");
                    columns.Add(pair.MakeRecorder());
                }

                var recorder = CreateRecorder(symbol);
                for (var i = 0; i < timeline.Length; i++)
                {
                    var rowRecorder = recorder.AddRow(timeline[i]);

                    foreach (var column in columns)
                        column.Record(rowRecorder, i);
                }

                recorder.Commit();
            }
        }

        public void Delete(string symbol, DateTime start, DateTime end, string[] columns = null)
        {
            using (_schema.OpenToWrite(symbol, out var meta))
            {
                foreach (var column in meta.GetColumns(columns))
                    Delete(symbol, column, start, end);
            }
        }

        #endregion

        #region Implementation of ITimeSeriesDbUpdater

        public IColumnStream CreateColumnWriter(string symbol, ColumnInfo column, DateTime start)
        {
            var tmpFilePath = _filePathProvider.GetTmpFilePath(symbol, column.Name, start);
            return _columnFactory.CreateStream(column, CreateWriter(tmpFilePath));
        }

        public void Commit(string symbol, params ColumnCommit[] columns)
        {
            using (_schema.OpenToWrite(symbol, out var meta))
            {
                foreach (var column in columns)
                {
                    var columnMeta = meta.GetOrCreateColumn(column.Name, column.Type);
                    CommitColumn(symbol, columnMeta, column.Start, column.End);
                }
            }
        }

        private void CommitColumn(string symbol, Column column, DateTime start, DateTime end)
        {
            var tmpFile = _filePathProvider.GetTmpFilePath(symbol, column.Name, start);
            if (!tmpFile.FileExists())
            {
                // Todo: LogWarn here No file found to be inserted ! Symbol: {symbol}, Column: {column}, Start: {start:yyyy-MM-dd HH:mm:ss.fff}, Path: {newFile}
                return;
            }

            var entriesToRemove = new List<DateTime>();
            var fileToMerge = new List<string>();
            var mergedStart = start;
            var mergedEnd = end;

            // Gets the files to merge with
            foreach (var entry in column.Search(start, end))
            {
                entriesToRemove.Add(entry.Key);

                var filePath = _filePathProvider.GetFilePath(symbol, column.Name, entry.Key);
                if (!filePath.FileExists())
                {
                    // Todo: LogInfo: The file has been deleted so we remove the entry {entry.Value}, Path: {filePath}
                    continue;
                }

                fileToMerge.Add(filePath);
                mergedStart = Time.Min(mergedStart, entry.Key);
                mergedEnd = Time.Max(mergedEnd, entry.Value.End);
            }

            // Remove all entries that will be merged
            foreach (var entry in entriesToRemove)
                column.Delete(entry);

            if (fileToMerge.Count > 0)
            {
                // Merge the files
                var slices = Merge(fileToMerge, new[] { tmpFile }, symbol, column, mergedStart);
                foreach (var slice in slices)
                    column.Insert(slice.Start, slice);
            }
            else // No merge to do
            {
                // Move the generated file from the tmp folder to the data folder.
                var newFile = _filePathProvider.GetFilePath(symbol, column.Name, start);
                tmpFile.MoveFileTo(newFile);
                column.Insert(start, CreateSlice(symbol, column, start, end));
            }
        }

        public void Revert(string symbol, params ColumnCommit[] columns)
        {
            // We don't need to lock the meta here because nothing has been commit
            // And the recorder works on a temporary path.
            foreach(var column in columns)
                _filePathProvider.GetFilePath(symbol, column.Name, column.Start)
                    .DeleteFile();
        }

        #endregion

        public void Delete(string symbol, Column column, DateTime start, DateTime end)
        {
            var filesForDeletion = new List<string>();
            var entries = column.Search(start, end).ToList();

            if (entries.Count == 0) return;

            var firstTimestamp = start;
            var file = _filePathProvider.GetFilePath(symbol, column.Name, entries[0].Key);
            if (file.FileExists())
            {
                filesForDeletion.Add(file);
                if(entries[0].Key < firstTimestamp)
                    firstTimestamp = entries[0].Key;
            }
            if (entries.Count > 1)
            {
                file = _filePathProvider.GetFilePath(symbol, column.Name, entries[entries.Count - 1].Key);
                if (file.FileExists())
                {
                    filesForDeletion.Add(file);
                    if (entries[entries.Count - 1].Key < firstTimestamp)
                        firstTimestamp = entries[0].Key;
                }

                // Delete files
                for (var i = 1; i < entries.Count - 1; i++)
                    _filePathProvider.GetFilePath(symbol, column.Name, entries[i].Key).DeleteFile();
            }

            if (filesForDeletion.Count > 0)
            {
                Merge(filesForDeletion, Enumerable.Empty<string>(), symbol, column, firstTimestamp,
                    ticks => ticks < start.Ticks && ticks > end.Ticks);
            }
        }

        private IEnumerable<FileOffset> GetFiles(string symbol, Column column, DateTime start, DateTime end)
        {
            var revision = column.Revision;
            var lastEnd = start;
            foreach (var entry in column.Search(start, end).ToList())
            {
                if (_schema.HasChanged(symbol) || column.HasChanged(revision))
                {
                    using (_schema.OpenToRead(symbol, out var symbolMeta))
                        column = symbolMeta.GetOrCreateColumn(column.Name);
                    
                    foreach (var file in GetFiles(symbol, column, lastEnd, end))
                        yield return file;
                    break;
                }

                lastEnd = entry.Value.End;
                yield return GetFileOffset(symbol, column.Name, entry.Key, entry.Value.GetOffset(start));
            }
        }

        private FileOffset GetFileOffset(string symbol, string column, DateTime timestamp, long offset)
            => new FileOffset(_filePathProvider.GetFilePath(symbol, column, timestamp), offset);

        private List<FileTimeSlice> Merge(IEnumerable<string> x, IEnumerable<string> y, string symbol, ColumnInfo column, DateTime start, Func<long, bool> filter = null)
        {
            switch (column.Type)
            {
                case FieldType.Float:
                    return Merge<FloatEntry, float>(x, y, symbol, column.Name, start, filter);
                case FieldType.Double:
                    return Merge<DoubleEntry, double>(x, y, symbol, column.Name, start, filter);
                case FieldType.Int32:
                    return Merge<Int32Entry, int>(x, y, symbol, column.Name, start, filter);
                case FieldType.Int64:
                    return Merge<Int64Entry, long>(x, y, symbol, column.Name, start, filter);
                case FieldType.String:
                    return Merge<StringEntry, string>(x, y, symbol, column.Name, start, filter);
                default:
                    throw new InvalidDataException($"Unknown column type during merge. Symbol: {symbol}, Column: {column.Name}, Type: {column.Type}");
            }
        }

        private unsafe List<FileTimeSlice> Merge<TEntry, T>(
            IEnumerable<string> x, 
            IEnumerable<string> y, 
            string symbol, 
            string column, 
            DateTime start,
            Func<long, bool> filter = null)
            where TEntry : unmanaged, IFieldEntry<T>
        {
            filter = filter ?? (p => true);

            var xTmp = x.MoveToTmpFiles().Select(p => new FileOffset(p, 0)).ToArray();
            var yTmp = y.MoveToTmpFiles().Select(p => new FileOffset(p, 0)).ToArray();

            var xf = new MinotaurFileStream(xTmp);
            var yf = new MinotaurFileStream(yTmp);

            var xs = new ColumnStream<TEntry>(xf, new VoidCodec<TEntry>());
            var xc = new ColumnCursor<TEntry, T, ColumnStream<TEntry>>(_allocator, xs);

            var ys = new ColumnStream<TEntry>(yf, new VoidCodec<TEntry>());
            var yc = new ColumnCursor<TEntry, T, ColumnStream<TEntry>>(_allocator, ys);

            const int maxFileSize = OPTIMAL_FILE_SIZE_MB / STREAM_CAPACITY * STREAM_CAPACITY; // Important keep integer division here

            var mf = CreateWriter(symbol, column, start);
            var writer = _columnFactory.CreateColumnStream<TEntry, T>(mf);

            var ticks = start.Ticks;
            var sliceTicks = new List<long> { ticks };
            xc.MoveNext(ticks);
            yc.MoveNext(ticks);
            while (xc.NextTicks != Time.MaxTicks && yc.NextTicks != Time.MaxTicks)
            {
                if (xc.Ticks == ticks)
                {
                    if(filter(ticks))
                        writer.Write((byte*) xc.Entry, sizeof(TEntry));
                    xc.MoveNext(xc.NextTicks);
                }

                if (yc.Ticks == ticks)
                {
                    if (filter(ticks))
                        writer.Write((byte*) yc.Entry, sizeof(TEntry));
                    yc.MoveNext(yc.NextTicks);
                }

                ticks = Math.Min(xc.Ticks, yc.Ticks);

                if (mf.Position >= maxFileSize)
                {
                    writer.Flush();
                    writer.Dispose();

                    sliceTicks.Add(ticks);
                    mf = CreateWriter(symbol, column, new DateTime(ticks));
                    writer = _columnFactory.CreateColumnStream<TEntry, T>(mf);
                }
            }

            writer.Flush();
            writer.Dispose();

            var slices = new List<FileTimeSlice>();
            for (var i = 1; i < sliceTicks.Count; i++)
                slices.Add(CreateSlice<TEntry, T>(symbol, column, sliceTicks[i - 1], sliceTicks[i]));
            slices.Add(CreateSlice<TEntry, T>(symbol, column, sliceTicks[sliceTicks.Count - 1], ticks));

            foreach (var tmp in xTmp)
                tmp.FilePath.DeleteFile();
            foreach (var tmp in yTmp)
                tmp.FilePath.DeleteFile();

            return slices;
        }

        private FileTimeSlice CreateSlice(string symbol, Column column, DateTime start, DateTime end)
        {
            switch (column.Type)
            {
                case FieldType.Float:
                    return CreateSlice<FloatEntry, float>(symbol, column.Name, start.Ticks, end.Ticks);
                case FieldType.Double:
                    return CreateSlice<DoubleEntry, double>(symbol, column.Name, start.Ticks, end.Ticks);
                case FieldType.Int32:
                    return CreateSlice<Int32Entry, int>(symbol, column.Name, start.Ticks, end.Ticks);
                case FieldType.Int64:
                    return CreateSlice<Int64Entry, long>(symbol, column.Name, start.Ticks, end.Ticks);
                case FieldType.String:
                    return CreateSlice<StringEntry, string>(symbol, column.Name, start.Ticks, end.Ticks);
                default:
                    throw new InvalidDataException($"Unknown column type during slice creation. Symbol: {symbol}, Column: {column.Name}, Type: {column.Type}");
            }
        }

        private FileTimeSlice CreateSlice<TEntry, T>(string symbol, string column, long startTicks, long endTicks)
            where TEntry : unmanaged, IFieldEntry<T>
        {
            var slice = new FileTimeSlice { Start = new DateTime(startTicks), End = new DateTime(endTicks) };

            var fs = new MinotaurFileStream(new[] { GetFileOffset(symbol, column, slice.Start, 0) });
            var cs = new ColumnStream<TEntry>(fs, new VoidCodec<TEntry>());
            var blocks = cs.ReadBlockInfos();

            slice.Blocks = blocks.Sample<TEntry, T>(OPTIMAL_FILE_BLOCK_SLICE);

            cs.Dispose();
            fs.Dispose();
            return slice;
        }

        private MinotaurFileStream CreateWriter(string symbol, string column, DateTime start)
            => CreateWriter(_filePathProvider.GetFilePath(symbol, column, start));
        private MinotaurFileStream CreateWriter(string filePath)
            => new MinotaurFileStream(filePath);

        private MinotaurFileStream CreateReader(string symbol, Column column, DateTime start, DateTime end)
            => new MinotaurFileStream(GetFiles(symbol, column, start, end));
    }
}