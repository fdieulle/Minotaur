using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Minotaur.IO;
using Minotaur.Native;

namespace Minotaur.Cursors
{
    public unsafe class FieldCursor : IFieldCursor
    {
        private const int INDEX_SIZE = sizeof(long);

        private readonly long* _snapshot;
        protected readonly byte* snapshot;
        private readonly int _entrySize;
        private readonly byte* _buffer;
        private readonly int _bufferSize;
        private IStream _stream;

        private byte* _offset;
        private byte* _bufferEnd;

        public DateTime Timestamp => *(DateTime*)_snapshot;

        public long Ticks => *_snapshot;
        public long* NextTicks => (long*)_offset;

        public FieldCursor(
            byte* snapshot, int entrySize,
            byte* buffer, int bufferSize,
            IStream stream)
        {
            _snapshot = (long*)snapshot;
            this.snapshot = snapshot + INDEX_SIZE;
            _entrySize = entrySize;
            _buffer = buffer;
            _bufferSize = bufferSize;
            _stream = stream;

            if (bufferSize % entrySize != 0)
                throw new InvalidConstraintException("Buffer size has to be a multiple of entry size");

            Reset();
        }

        public bool Next(long ticks)
        {
            // Todo: maybe it's better to copy nextIndex into snapshot instead of always read buffer
            if (ticks < *(long*)_offset) return _offset < _bufferEnd;

            do
            {
                // Todo: Maybe It's better to copy snapshot memory with Buffer.CopyMemory call
                var from = _offset;

                *_snapshot = *(long*)from;
                from += INDEX_SIZE;

                _offset += _entrySize;
                var i = -1;
                while (from < _offset)
                    *(snapshot + ++i) = *from++;

                if (_offset >= _bufferEnd && !Read())
                    return true;

            } while (ticks >= *(long*)_offset);

            return true;
        }

        public bool Reset()
        {
            _bufferEnd = _offset = _buffer;
            *_snapshot = Time.MinTicks;
            WriteWrongValue();

            _stream.Reset();
            return Read();
        }

        private bool Read()
        {
            _offset = _buffer;
            var read = _stream.Read(_offset, _bufferSize);
            if (read == 0)
            {
                *(long*)_offset = Time.MaxTicks;
                return false;
            }

            //if (read % this.entrySize != 0)
            //	throw new CorruptedDataException("Data read isn't a multiple of entry size");

            _bufferEnd = _buffer + read;
            return true;
        }

        public void Dispose()
        {
            Reset();
        }

        protected virtual void WriteWrongValue()
        {
            var valueSize = _entrySize - INDEX_SIZE;
            switch (valueSize)
            {
                case 2:
                    *(ushort*)snapshot = 0;
                    break;
                case 4:
                    *(uint*)snapshot = 0;
                    break;
                case 8:
                    *(ulong*)snapshot = 0;
                    break;
                default:
                    for (var i = 0; i < valueSize; i++)
                        *(snapshot + i) = 0;
                    break;
            }
        }

        public void Swith(IStream stream)
        {
            _stream = stream; // Todo be sure that it's never null
            Read();
        }
    }

    public unsafe class Int32Cursor : FieldCursor, IFieldCursor<int>
    {
        public Int32Cursor(byte* snapshot, byte* buffer, int bufferSize, IStream stream)
            : base(snapshot, sizeof(Int32Entry), buffer, bufferSize, stream)
        {
        }

        public int Value => *(int*)snapshot;
    }

    public unsafe class FloatCursor : FieldCursor, IFieldCursor<float>
    {
        public FloatCursor(byte* snapshot, byte* buffer, int bufferSize, IStream stream)
            : base(snapshot, sizeof(FloatEntry), buffer, bufferSize, stream)
        {

        }

        public float Value => *(float*)snapshot;

        protected override void WriteWrongValue()
        {
            *(float*)snapshot = float.NaN;
        }
    }

    public unsafe class Int64Cursor : FieldCursor, IFieldCursor<long>
    {
        public Int64Cursor(byte* snapshot, byte* buffer, int bufferSize, IStream stream)
            : base(snapshot, sizeof(Int64Entry), buffer, bufferSize, stream)
        {
        }

        public long Value => *(long*)snapshot;
    }

    public unsafe class DoubleCursor : FieldCursor, IFieldCursor<double>
    {
        public DoubleCursor(byte* snapshot, byte* buffer, int bufferSize, IStream stream)
            : base(snapshot, sizeof(DoubleEntry), buffer, bufferSize, stream)
        {

        }

        public double Value => *(double*)snapshot;

        protected override void WriteWrongValue()
        {
            *(double*)snapshot = double.NaN;
        }
    }

    public unsafe class DateTimeCursor : FieldCursor, IFieldCursor<DateTime>
    {
        protected DateTimeCursor(byte* snapshot, byte* buffer, int bufferSize, IStream stream)
            : base(snapshot, sizeof(long), buffer, bufferSize, stream)
        {

        }

        public DateTime Value => new DateTime(*(long*)snapshot);

        protected override void WriteWrongValue()
        {
            *(long*)snapshot = DateTime.MinValue.Ticks;
        }
    }
}
