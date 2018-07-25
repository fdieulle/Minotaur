using System;
using System.Collections.Generic;
using System.Linq;

namespace Minotaur.Cursors
{
    public unsafe class MultiFieldsCursor : ICursor
    {
        private readonly Dictionary<int, FieldCursor> _fields;
        private readonly FieldCursor[] _cursors;

        private long _currentTicks;
        private long _nextTicks;

        public MultiFieldsCursor(Dictionary<int, FieldCursor> fields)
        {
            _fields = fields ?? new Dictionary<int, FieldCursor>();
            _cursors = _fields.Values.ToArray();

            Reset();
        }

        public DateTime Timestamp => new DateTime(_currentTicks);

        public DateTime MoveNext(DateTime timestamp)
        {
            if (timestamp.Ticks < _nextTicks) return new DateTime(_nextTicks);

            var ticks = timestamp.Ticks;
            unchecked
            {
                _nextTicks = Time.MaxTicks;
                for (var i = 0; i < _cursors.Length; i++)
                {
                    if (_cursors[i].Next(ticks) && _cursors[i].Ticks > _currentTicks)
                        _currentTicks = _cursors[i].Ticks;

                    if (*_cursors[i].NextTicks < _nextTicks)
                        _nextTicks = *_cursors[i].NextTicks;
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
            _currentTicks = Time.MinTicks;
            _nextTicks = Time.MaxTicks;

            for (var i = 0; i < _cursors.Length; i++)
            {
                if (_cursors[i].Reset() && *_cursors[i].NextTicks < _nextTicks)
                    _nextTicks = *_cursors[i].NextTicks;
            }
        }

        public IFieldProxy<T> GetProxy<T>(int fieldId) where T : struct
        {
            if (_fields.TryGetValue(fieldId, out var cursor))
                return cursor as IFieldProxy<T>;

            return null;
        }

        public void Dispose()
        {
            for (var i = 0; i < _cursors.Length; i++)
                _cursors[i].Dispose();

            _fields.Clear();
            Array.Clear(_cursors, 0, _cursors.Length);
        }
    }
}
