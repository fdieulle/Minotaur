using System;
using System.Collections.Generic;
using System.Linq;
using Minotaur.Streams;

namespace Minotaur.Cursors
{
    public class TimeSeriesCursor<TStream> : ICursor
        where TStream : IStream
    {
        private readonly Dictionary<string, ColumnCursor<TStream>> _columns;
        private readonly ColumnCursor<TStream>[] _cursors;

        private long _ticks;
        private long _nextTicks;

        public TimeSeriesCursor(Dictionary<string, ColumnCursor<TStream>> columns)
        {
            _columns = columns ?? new Dictionary<string, ColumnCursor<TStream>>();
            _cursors = _columns.Values.ToArray();

            Reset();
        }

        public DateTime Timestamp => new DateTime(_ticks);

        public DateTime MoveNext(DateTime timestamp)
        {
            // Should not need a while here an if is enough
            while (timestamp.Ticks >= _nextTicks)
            {
                _nextTicks = Time.MaxTicks;
                for (var i = 0; i < _cursors.Length; i++)
                {
                    _cursors[i].MoveNext(timestamp.Ticks);

                    if (_cursors[i].Ticks > _ticks)
                        _ticks = _cursors[i].Ticks;

                    if (_cursors[i].NextTicks < _nextTicks)
                        _nextTicks = _cursors[i].NextTicks;
                }
            }

            return new DateTime(_nextTicks);
        }

        public DateTime MoveNextTick()
        {
            return MoveNext(new DateTime(_nextTicks));
        }

        public void Reset()
        {
            _ticks = Time.MinTicks;
            _nextTicks = Time.MinTicks;

            for (var i = 0; i < _cursors.Length; i++)
                _cursors[i].Reset();
        }

        public IFieldProxy<T> GetProxy<T>(string column) where T : struct
        {
            if (_columns.TryGetValue(column, out var cursor))
                return cursor as IFieldProxy<T>;

            return null;
        }

        public void Dispose()
        {
            _columns.Clear();
            Array.Clear(_cursors, 0, _cursors.Length);
        }
    }
}
