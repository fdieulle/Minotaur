using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Minotaur.Core.Platform;
using Minotaur.Native;
using Minotaur.Providers;
using Minotaur.Streams;

namespace Minotaur.Recorders
{
    public unsafe class TimeSeriesRecorder<TPlatform> : ITimeSeriesRecorder, IRowRecorder<ITimeSeriesRecorder>
        where TPlatform : IPlatform
    {
        // Todo: Stream size heuristic is delayed to the Stream

        private readonly string _symbol;
        private readonly IStreamFactory<TPlatform> _streamFactory;
        private readonly IFilePathProvider _filePathProvider;
        private readonly Dictionary<string, Column> _columns = new Dictionary<string, Column>();
        private readonly List<FileMetaData> _metaData = new List<FileMetaData>();

        private readonly GCHandle _bufferHandle;
        private readonly byte* _buffer;

        private DateTime _currentTimestamp;

        public TimeSeriesRecorder(
            string symbol, 
            IStreamFactory<TPlatform> streamFactory,
            IFilePathProvider filePathProvider)
        {
            _symbol = symbol;
            _streamFactory = streamFactory;
            _filePathProvider = filePathProvider;

            var buffer = new byte[Natives.MAX_ENTRY_SIZE];
            _bufferHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            _buffer = (byte*)_bufferHandle.AddrOfPinnedObject();
        }

        #region Implementation of ITimeSeriesRecorder

        public IEnumerable<FileMetaData> MetaData => _metaData;

        public IRowRecorder<ITimeSeriesRecorder> AddRow(DateTime timestamp)
        {
            _currentTimestamp = timestamp;
            *(long*) _buffer = timestamp.Ticks;
            return this;
        }

        public void Flush()
        {
            foreach (var column in _columns.Values)
                column.Stream.Flush();
        }

        public void Clear()
        {
            Flush();
            _columns.Clear();
            _metaData.Clear();
        }

        #endregion

        #region Implementation of IRowRecorder

        public ITimeSeriesRecorder Parent => this;

        public IRowRecorder<ITimeSeriesRecorder> Record<T>(string column, T value) where T : unmanaged
        {
            if (column == null) throw new ArgumentException("Column name can't be null", nameof(column));

            var isNew = false;
            if (!_columns.TryGetValue(column, out var tuple) || !tuple.Meta.Type.IsFongible<T>())
            {
                isNew = true;
                // Todo: How to create file path here ? define an IPathProvider ??
                var meta = new FileMetaData
                {
                    Symbol = _symbol,
                    Column = column,
                    Type = Natives.GetType<T>(),
                    Start = _currentTimestamp,
                    FilePath = _filePathProvider.GetPath(_symbol, column, _currentTimestamp)
                };

                // be sure that there is no hole between timelines
                if (tuple != null)
                    tuple.Meta.End = _currentTimestamp;
                
                _columns[column] = tuple = new Column{ Meta = meta, Stream = _streamFactory.Create(meta) };
            }

            // Keep track of timeline end
            tuple.Meta.End = _currentTimestamp;

            // Write into buffer then push it to the stream.
            Natives.WriteValue(_buffer + sizeof(long), ref value);
            var length = sizeof(long) + sizeof(T);
            if (tuple.Stream.Write(_buffer, length) < length) 
            {
                // We hit the end of stream so we roll it
                _columns.Remove(column);
                tuple.Stream.Flush();

                Record<T>(column, value); // Todo: Maybe check a max retry here to avoid overflow exception
                return this;
            }

            // Insert metadata only if at least 1 point is wrote
            if(isNew) _metaData.Add(tuple.Meta);

            return this;
        }

        #endregion

        #region Implementation of IDisposable

        public void Dispose()
        {
            Clear();
           
            if (_bufferHandle.IsAllocated)
                _bufferHandle.Free();
        }

        #endregion

        private class Column
        {
            public FileMetaData Meta { get; set; }
            public IStream Stream { get; set; }
        }
    }
}