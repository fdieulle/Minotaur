using System;
using System.Collections.Generic;
using System.IO;
using Minotaur.Core;
using Minotaur.Native;
using Minotaur.Providers;
using Minotaur.Streams;

namespace Minotaur.Recorders
{
    public unsafe class TimeSeriesRecorder : ITimeSeriesRecorder, IRowRecorder<ITimeSeriesRecorder>
    {
        private readonly string _symbol;
        private readonly ITimeSeriesDbUpdater _dbUpdater;
        private readonly IAllocator _allocator;
        private readonly byte* _buffer;
        private readonly Dictionary<string, Column> _columns = new Dictionary<string, Column>();

        private DateTime _currentTimestamp;

        public TimeSeriesRecorder(
            string symbol,
            ITimeSeriesDbUpdater dbUpdater,
            IAllocator allocator)
        {
            _symbol = symbol;
            _dbUpdater = dbUpdater;
            _allocator = allocator;
            _buffer = allocator.Allocate(Natives.MAX_ENTRY_SIZE);
        }

        #region Implementation of ITimeSeriesRecorder

        public IRowRecorder<ITimeSeriesRecorder> AddRow(DateTime timestamp)
        {
            if(timestamp < _currentTimestamp) 
                throw new InvalidDataException($"Timestamp has to be greater than previous. Timestamp: {timestamp:yyyy-MM-dd HH:mm:ss.fffff}, Previous: {_currentTimestamp:yyyy-MM-dd HH:mm:ss.fffff}");

            _currentTimestamp = timestamp;
            *(long*) _buffer = timestamp.Ticks;
            return this;
        }

        public void Commit()
        {
            foreach (var column in _columns.Values)
            {
                column.Stream?.Flush();
                column.Stream?.Dispose();
                _dbUpdater.CommitColumn(_symbol, column, column.Start, column.End);
            }

            _columns.Clear();
        }

        public void Revert()
        {
            foreach (var column in _columns.Values)
            {
                column.Stream?.Dispose();
                _dbUpdater.RevertColumn(_symbol, column.Name, column.Start);
            }

            _columns.Clear();
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
        }

        #endregion

        #region Implementation of IRowRecorder

        public ITimeSeriesRecorder Parent => this;

        public IRowRecorder<ITimeSeriesRecorder> Record<T>(string column, T value) where T : unmanaged
        {
            if (column == null) throw new ArgumentException("Column name can't be null", nameof(column));

            if (!_columns.TryGetValue(column, out var tuple))
            {
                _columns.Add(column, tuple = new Column(_currentTimestamp)
                {
                    Name = column,
                    Type = Natives.GetType<T>()
                });

                tuple.Stream = _dbUpdater.CreateColumnWriter(_symbol, tuple, _currentTimestamp);
            }

            if (!tuple.Type.IsFongible<T>())
                throw new InvalidDataException($"The value of type {typeof(T)} isn't fongible with {tuple.Type}");

            // Keep track of timeline end
            tuple.End = _currentTimestamp;

            // Write into buffer then push it to the stream.
            Natives.WriteValue(_buffer + sizeof(long), ref value);
            var length = sizeof(long) + sizeof(T);
            if (tuple.Stream.Write(_buffer, length) < length) 
            {
                // We hit the end of stream so we roll it
                _columns.Remove(column);
                tuple.Stream.Flush();

                Record(column, value); // Todo: Maybe check a max retry here to avoid overflow exception
                return this;
            }

            return this;
        }

        #endregion

        #region Implementation of IDisposable

        public void Dispose()
        {
            Clear();
           _allocator.Free(_buffer);
        }

        #endregion

        private class Column : ColumnInfo
        {
            public DateTime Start { get; }
            public DateTime End { get; set; }
            public IColumnStream Stream { get; set; }

            public Column(DateTime start) => Start = start;

            public override string ToString() 
                => $"{base.ToString()}, [{Start:yyyy-MM-dd HH:mm:ss.fff}; {End:yyyy-MM-dd HH:mm:ss.fff}]";
        }
    }
}