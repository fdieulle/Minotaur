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
        private const int OPTIMAL_FILE_SIZE_MB = 500;

        private readonly IFilePathProvider _filePathProvider;
        private readonly IAllocator _allocator;
        private readonly MetaManager _metaManager;
        private readonly ColumnStreamFactory<MinotaurFileStream> _columnFactory = new ColumnStreamFactory<MinotaurFileStream>();

        public FileTimeSeriesDb(IFilePathProvider filePathProvider, IAllocator allocator)
        {
            _filePathProvider = filePathProvider;
            _allocator = allocator;
            _metaManager = new MetaManager(filePathProvider);
        }

        #region Implementation of ITimeSeriesDb

        public ICursor GetCursor(string symbol, DateTime start, DateTime? end = null, string[] columns = null)
        {
            end = end ?? start.AddDays(1);

            var cursors = new Dictionary<string, IColumnCursor>();
            using (_metaManager.OpenMetaToRead(symbol, out var meta))
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
            throw new NotImplementedException();
        }

        public void Delete(string symbol, DateTime start, DateTime end, string[] columns = null)
        {
            using (_metaManager.OpenMetaToWrite(symbol, out var meta))
            {
                foreach (var column in meta.GetColumns(columns))
                    Delete(symbol, column, start, end);
            }
        }

        #endregion

        #region Implementation of ITimeSeriesDbUpdater

        public IColumnStream CreateColumnWriter(string symbol, ColumnInfo column, DateTime start)
        {
            var writer = CreateWriter(symbol, column.Name, start);
            return _columnFactory.CreateStream(column, writer);
        }

        public void Commit(string symbol, params ColumnCommit[] columns)
        {
            using (_metaManager.OpenMetaToWrite(symbol, out var meta))
            {
                foreach (var column in columns)
                {
                    var columnMeta = meta.GetOrCreateColumn(column.Name, column.Type);
                    CommitColumn(symbol, columnMeta, column.Start, column.End);
                }
            }
        }

        private void CommitColumn(string symbol, ColumnMeta column, DateTime start, DateTime end)
        {
            var newFile = _filePathProvider.GetFilePath(symbol, column.Name, start);
            if (!newFile.FileExists())
            {
                // Todo: LogWarn here No file found to be inserted ! Symbol: {symbol}, Column: {column}, Start: {start:yyyy-MM-dd HH:mm:ss.fff}, Path: {newFile}
                return;
            }

            var entriesToRemove = new List<DateTime>();
            var fileToMerge = new List<string>();
            var mergedStart = start;
            var mergedEnd = end;

            // Gets the file to merge with
            foreach (var entry in column.Timeline.Search(start, end))
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
                column.Timeline.Delete(entry);

            if (fileToMerge.Count > 0)
            {
                var slices = Merge(fileToMerge, new[] { newFile }, symbol, column, mergedStart);
                foreach (var slice in slices)
                    column.Timeline.Insert(slice.Start, slice);
            }
            else
                column.Timeline.Insert(start, new TimeSlice { Start = start, End = end });
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

        public void Delete(string symbol, ColumnMeta column, DateTime start, DateTime end)
        {
            var filesForDeletion = new List<string>();
            var entries = column.Timeline.Search(start, end).ToList();

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

        private IEnumerable<string> GetFiles(string symbol, ColumnMeta column, DateTime start, DateTime end)
        {
            foreach (var entry in column.Timeline.Search(start, end))
                yield return _filePathProvider.GetFilePath(symbol, column.Name, entry.Key);
        }

        private List<TimeSlice> Merge(IEnumerable<string> x, IEnumerable<string> y, string symbol, ColumnInfo column, DateTime start, Func<long, bool> filter = null)
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

        private unsafe List<TimeSlice> Merge<TEntry, T>(
            IEnumerable<string> x, 
            IEnumerable<string> y, 
            string symbol, 
            string column, 
            DateTime start,
            Func<long, bool> filter = null)
            where TEntry : unmanaged, IFieldEntry<T>
        {
            filter = filter ?? (p => true);

            var xTmp = x.MoveToTmpFiles().ToArray();
            var yTmp = y.MoveToTmpFiles().ToArray();

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
                        writer.Write((byte*)yc.Entry, sizeof(TEntry));
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

            var slices = new List<TimeSlice>();
            for (var i = 1; i < sliceTicks.Count; i++)
                slices.Add(CreateSlice(sliceTicks[i - 1], sliceTicks[i]));
            slices.Add(CreateSlice(sliceTicks[sliceTicks.Count - 1], ticks));

            foreach (var tmp in xTmp)
                tmp.DeleteFile();
            foreach (var tmp in yTmp)
                tmp.DeleteFile();

            return slices;
        }

        private static TimeSlice CreateSlice(long startTicks, long endTicks)
            => new TimeSlice { Start = new DateTime(startTicks), End = new DateTime(endTicks) };

        private MinotaurFileStream CreateWriter(string symbol, string column, DateTime start)
            => new MinotaurFileStream(_filePathProvider.GetFilePath(symbol, column, start));

        private MinotaurFileStream CreateReader(string symbol, ColumnMeta column, DateTime start, DateTime end)
            => new MinotaurFileStream(GetFiles(symbol, column, start, end));
    }
}