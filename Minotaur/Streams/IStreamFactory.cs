using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Minotaur.Codecs;
using Minotaur.Core;
using Minotaur.Cursors;
using Minotaur.Native;
using Minotaur.Providers;
using Minotaur.Recorders;

namespace Minotaur.Streams
{
    // Todo: remove it
    public interface IStreamFactory<TStream> where TStream : IStream
    {
        TStream CreateReader(string filePath);
        TStream CreateWriter(string filePath);
    }
   
    public class ColumnStreamFactory<TStream>
        where TStream : IStream
    {
        public IColumnCursor CreateCursor(ColumnInfo column, TStream stream, IAllocator allocator)
        {
            switch (column.Type)
            {
                case FieldType.Float:
                    return CreateCursor<FloatEntry, float>(stream, allocator);
                case FieldType.Double:
                    return CreateCursor<DoubleEntry, double>(stream, allocator);
                case FieldType.Int32:
                    return CreateCursor<Int32Entry, int>(stream, allocator);
                case FieldType.Int64:
                    return CreateCursor<Int64Entry, long>(stream, allocator);
                case FieldType.String:
                    return CreateCursor<StringEntry, string>(stream, allocator);
                default:
                    throw new NotSupportedException($"Type not supported: {column.Type}, for column : {column.Name}");
            }
        }

        private ColumnCursor<TEntry, T, ColumnStream<TEntry>> CreateCursor<TEntry, T>(IStream stream, IAllocator allocator)
            where TEntry : unmanaged, IFieldEntry<T> 
            => new ColumnCursor<TEntry, T, ColumnStream<TEntry>>(allocator, CreateColumnStream<TEntry, T>(stream));

        public IColumnStream CreateStream(ColumnInfo column, TStream stream)
        {
            switch (column.Type)
            {
                case FieldType.Float:
                    return CreateColumnStream<FloatEntry, float>(stream);
                case FieldType.Double:
                    return CreateColumnStream<DoubleEntry, double>(stream);
                case FieldType.Int32:
                    return CreateColumnStream<Int32Entry, int>(stream);
                case FieldType.Int64:
                    return CreateColumnStream<Int64Entry, long>(stream);
                case FieldType.String:
                    return CreateColumnStream<StringEntry, string>(stream);
                default:
                    throw new NotSupportedException($"Type not supported: {column.Type}, for column : {column.Name}");
            }
        }

        public ColumnStream<TEntry> CreateColumnStream<TEntry, T>(IStream stream) 
            where TEntry : unmanaged, IFieldEntry<T>
            => new ColumnStream<TEntry>(stream, new VoidCodec<TEntry>());
    }

    public interface ITimeSeriesDb
    {
        ICursor GetCursor(string symbol, DateTime start, DateTime? end = null, string[] columns = null);

        ITimeSeriesRecorder CreateRecorder(string symbol);

        void Insert(string symbol, Dictionary<string, Array> data);

        void Delete(string symbol, DateTime start, DateTime end, string[] columns = null);
    }

    public interface ITimeSeriesDbUpdater
    {
        IColumnStream CreateColumnWriter(string symbol, ColumnInfo column, DateTime start);
        void CommitColumn(string symbol, ColumnInfo column, DateTime start, DateTime end);
        void RevertColumn(string symbol, string column, DateTime start);
    }

    public class FileTimeSeriesDb : ITimeSeriesDb, ITimeSeriesDbUpdater
    {
        private const int STREAM_CAPACITY = 8192;
        private const int OPTIMAL_FILE_SIZE_MB = 500;

        private readonly IFilePathProvider _filePathProvider;
        private readonly IAllocator _allocator;
        private readonly Dictionary<string, ColumnTimeSlices> _columns = new Dictionary<string, ColumnTimeSlices>();
        private readonly ColumnStreamFactory<MinotaurFileStream> _columnFactory = new ColumnStreamFactory<MinotaurFileStream>();

        public FileTimeSeriesDb(IFilePathProvider filePathProvider, IAllocator allocator)
        {
            _filePathProvider = filePathProvider;
            _allocator = allocator;
        }

        #region Implementation of ITimeSeriesDb

        public ICursor GetCursor(string symbol, DateTime start, DateTime? end = null, string[] columns = null)
        {
            columns = columns ?? GetAllColumns(symbol);
            end = end ?? start.AddDays(1);

            var cursors = new Dictionary<string, IColumnCursor>();
            foreach (var column in columns.Select(p => GetMeta(symbol, p)))
            {
                var stream = CreateReader(symbol, column.Name, start, end.Value);
                cursors[column.Name] = _columnFactory.CreateCursor(column, stream, _allocator);
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
            columns = columns ?? GetAllColumns(symbol);
            foreach (var column in columns)
                Delete(symbol, column, start, end);
        }

        #endregion

        #region Implementation of ITimeSeriesDbUpdater

        public IColumnStream CreateColumnWriter(string symbol, ColumnInfo column, DateTime start)
        {
            var writer = CreateWriter(symbol, column.Name, start);
            return _columnFactory.CreateStream(column, writer);
        }

        public void CommitColumn(string symbol, ColumnInfo column, DateTime start, DateTime end)
        {
            var newFile = _filePathProvider.GetFilePath(symbol, column.Name, start);
            if (!newFile.FileExists())
            {
                // Todo: LogWarn here No file found to be inserted ! Symbol: {symbol}, Column: {column}, Start: {start:yyyy-MM-dd HH:mm:ss.fff}, Path: {newFile}
                return;
            }

            var metaFile = _filePathProvider.GetMetaFilePath(symbol, string.Empty);
            using (metaFile.FileLock())
            {
                var meta = GetMeta(symbol, column.Name, column.Type);

                var entriesToRemove = new List<DateTime>();
                var fileToMerge = new List<string>();
                var mergedStart = start;
                var mergedEnd = end;

                // Gets the file to merge with
                foreach (var entry in meta.BTree.Search(start, end))
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
                    meta.BTree.Delete(entry);

                if (fileToMerge.Count > 0)
                {
                    var slices = Merge(fileToMerge, new[] { newFile }, symbol, meta, mergedStart);
                    foreach (var slice in slices)
                        meta.BTree.Insert(slice.Start, slice);
                }
                else
                    meta.BTree.Insert(start, new TimeSlice { Start = start, End = end });

                PersistMeta(symbol, meta);
            }
        }

        public void RevertColumn(string symbol, string column, DateTime start) 
            => _filePathProvider.GetFilePath(symbol, column, start)
                .DeleteFile();

        #endregion

        public void Delete(string symbol, string column, DateTime start, DateTime end)
        {
            var meta = GetMeta(symbol, column);

            var filesForDeletion = new List<string>();
            var entries = meta.BTree.Search(start, end).ToList();

            if (entries.Count == 0) return;

            var firstTimestamp = start;
            var file = _filePathProvider.GetFilePath(symbol, column, entries[0].Key);
            if (file.FileExists())
            {
                filesForDeletion.Add(file);
                if(entries[0].Key < firstTimestamp)
                    firstTimestamp = entries[0].Key;
            }
            if (entries.Count > 1)
            {
                file = _filePathProvider.GetFilePath(symbol, column, entries[entries.Count - 1].Key);
                if (file.FileExists())
                {
                    filesForDeletion.Add(file);
                    if (entries[entries.Count - 1].Key < firstTimestamp)
                        firstTimestamp = entries[0].Key;
                }

                // Delete files
                for (var i = 1; i < entries.Count - 1; i++)
                    _filePathProvider.GetFilePath(symbol, column, entries[i].Key).DeleteFile();
            }

            if (filesForDeletion.Count > 0)
            {
                Merge(filesForDeletion, Enumerable.Empty<string>(), symbol, meta, firstTimestamp,
                    ticks => ticks < start.Ticks && ticks > end.Ticks);
            }
        }

        private IEnumerable<string> GetFiles(string symbol, string column, DateTime start, DateTime end)
        {
            var meta = GetMeta(symbol, column);
            foreach (var entry in meta.BTree.Search(start, end))
                yield return _filePathProvider.GetFilePath(symbol, column, entry.Key);
        }

        private static string GetKey(string symbol, string column) => $"{symbol}_{column}";

        private List<TimeSlice> Merge(IEnumerable<string> x, IEnumerable<string> y, string symbol, ColumnInfo column, DateTime start, Func<long, bool> filter = null)
        {
            switch (column.Type)
            {
                case FieldType.Float:
                    return Merge<FloatEntry, float>(x, y, symbol, column.Name, start);
                case FieldType.Double:
                    return Merge<DoubleEntry, double>(x, y, symbol, column.Name, start);
                case FieldType.Int32:
                    return Merge<Int32Entry, int>(x, y, symbol, column.Name, start);
                case FieldType.Int64:
                    return Merge<Int64Entry, long>(x, y, symbol, column.Name, start);
                case FieldType.String:
                    return Merge<StringEntry, string>(x, y, symbol, column.Name, start);
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

        private MinotaurFileStream CreateReader(string symbol, string column, DateTime start, DateTime end)
            => new MinotaurFileStream(GetFiles(symbol, column, start, end));

        #region Meta persistency

        // ReSharper disable once StaticMemberInGenericType
        private static readonly XmlSerializer serializer = new XmlSerializer(typeof(SymbolMeta));

        private ColumnTimeSlices GetMeta(string symbol, string column, FieldType type = FieldType.Unknown)
        {
            if (!_columns.TryGetValue(GetKey(symbol, column), out var timeSlices))
            {
                var metaFilePath = _filePathProvider.GetMetaFilePath(symbol, string.Empty);
                using (metaFilePath.FileLock())
                {
                    var meta = serializer.Deserialize<SymbolMeta>(metaFilePath);

                    if (meta?.Columns != null)
                    {
                        foreach (var columnMeta in meta.Columns)
                            _columns[GetKey(symbol, columnMeta.Name)] = new ColumnTimeSlices(columnMeta);
                    }

                    var key = GetKey(symbol, column);
                    if (!_columns.TryGetValue(key, out timeSlices))
                    {
                        if (type == FieldType.Unknown)
                            throw new InvalidDataException($"Column type Unknown isn't supported and has to be defined for {column}.");

                        _columns.Add(key, timeSlices = CreateColumnStub(column, type));
                    }
                }
            }

            if (type != FieldType.Unknown && timeSlices.Type != type)
                throw new InvalidDataException($"Column type doesn't match for {column}. Request for {type} but it's store as {timeSlices.Type}");

            return timeSlices;
        }
        
        private static ColumnTimeSlices CreateColumnStub(string name, FieldType type) 
            => new ColumnTimeSlices(new ColumnMeta { Name = name, Type = type });

        private void PersistMeta(string symbol, ColumnTimeSlices column)
        {
            var metaFilePath = _filePathProvider.GetMetaFilePath(symbol, string.Empty);
            using (metaFilePath.FileLock())
            {
                var meta = serializer.Deserialize<SymbolMeta>(metaFilePath) ?? new SymbolMeta { Symbol = symbol };

                meta.Columns = meta.Columns?
                    .Where(p => !string.Equals(p.Name, column.Name))
                    .ToList() ?? new List<ColumnMeta>();
                meta.Columns.Add(CreateColumnMeta(column));

                serializer.Serialize(metaFilePath, meta);
            }
        }

        private static ColumnMeta CreateColumnMeta(ColumnTimeSlices column)
            => new ColumnMeta
            {
                Name = column.Name,
                Type = column.Type,
                Timeline = column.BTree.Select(p => p.Value).ToList()
            };

        private string[] GetAllColumns(string symbol)
        {
            var metaFilePath = _filePathProvider.GetMetaFilePath(symbol, string.Empty);
            using (metaFilePath.FileLock())
            {
                return serializer.Deserialize<SymbolMeta>(metaFilePath)
                           ?.Columns?.Select(p => p.Name)?.ToArray() ?? new string[0];
            }
        }

        #endregion

        private class ColumnTimeSlices : ColumnInfo
        {
            public BTree<DateTime, TimeSlice> BTree { get; } = new BTree<DateTime, TimeSlice>(50);

            public ColumnTimeSlices(ColumnMeta meta)
            {
                Name = meta.Name;
                Type = meta.Type;
                if (meta.Timeline != null)
                {
                    foreach (var slice in meta.Timeline.Where(p => p != null))
                        BTree.Insert(slice.Start, slice);
                }
            }
        }
    }

    public class SymbolMeta
    {
        public string Symbol { get; set; }

        public List<ColumnMeta> Columns { get; set; } = new List<ColumnMeta>();
    }

    public class ColumnMeta : ColumnInfo
    {
        public List<TimeSlice> Timeline { get; set; }
    }

    public class TimeSlice
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        public override string ToString()
        {
            return $"{nameof(Start)}: {Start:yyyy-MM-dd HH:mm:ss.fff}, {nameof(End)}: {End:yyyy-MM-dd HH:mm:ss.fff}";
        }
    }
}