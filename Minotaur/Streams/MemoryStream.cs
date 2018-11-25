using System;
using System.IO;

namespace Minotaur.Streams
{
    public unsafe class MemoryStream : IStream
    {
        private byte[] _buffer;
        private int _capacity;
        private int _offset;
        private int _end;

        public int Position => _offset;

        public MemoryStream(int capacity = 8192)
            : this(new byte[capacity])
        {
            _end = 0;
        }

        public MemoryStream(byte[] buffer)
        {
            _buffer = buffer ?? new byte[8192];
            _end = _capacity = _buffer.Length;
        }

        #region Implementation of IStream

        public int Read(byte* p, int length)
        {
            length = Math.Min(_end - _offset, length);

            EnsureCapacity(_offset + length);

            if (_offset >= _capacity) return 0;

            fixed (byte* pt = &_buffer[_offset])
                Buffer.MemoryCopy(pt, p, length, length);

            _offset += length;

            return length;
        }

        public int Write(byte* p, int length)
        {
            EnsureCapacity(_offset + length);

            fixed (byte* pt = &_buffer[_offset])
                Buffer.MemoryCopy(p, pt, length, length);

            _offset += length;
            _end = Math.Max(_end, _offset);

            return length;
        }

        public int Seek(int seek, SeekOrigin origin)
        {
            switch (origin)
            {
                case SeekOrigin.Begin:
                    _offset = seek;
                    break;
                case SeekOrigin.Current:
                    _offset += seek;
                    break;
                case SeekOrigin.End:
                    _offset = _end - seek;
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(origin));
            }

            _end = Math.Max(_end, _offset);
            EnsureCapacity(_end);
            return seek;
        }

        public void Reset()
        {
            _offset = 0;
        }

        public void Flush()
        {
            
        }

        #endregion

        public byte[] ToArray()
        {
            var copy = new byte[_end];
            Array.Copy(_buffer, copy, _end);
            return copy;
        }

        public void SetLength(int length)
        {
            _end = length;
        }

        #region Implementation of IDisposable

        public void Dispose()
        {
            Reset();
        }

        #endregion

        private void EnsureCapacity(int length)
        {
            if (length <= _capacity) return;

            while (length > _capacity)
            {
                var nc = _capacity * 2;
                if (nc < _capacity) throw new OverflowException("Capacity overflow");

                _capacity *= 2;
            }

            var copy = new byte[_capacity];
            fixed (byte* dst = copy)
            fixed (byte* src = _buffer)
                Buffer.MemoryCopy(src, dst, _buffer.Length, _buffer.Length);
            _buffer = copy;
        }
    }
}
