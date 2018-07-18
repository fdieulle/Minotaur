using System;
using System.IO;

namespace Minotaur.IO
{
    public unsafe class MemoryStream : IStream
    {
        private byte[] _buffer;
        private int _capacity;
        private int _offset;
        private int _end;

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
                CopyMemory(p, pt, length);

            _offset += length;

            return length;
        }

        public int Write(byte* p, int length)
        {
            EnsureCapacity(_offset + length);

            fixed (byte* pt = &_buffer[_offset])
                CopyMemory(pt, p, length);

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

        #endregion

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
            fixed (byte* pOut = copy)
            fixed (byte* pIn = _buffer)
                CopyMemory(pOut, pIn, _buffer.Length);
            _buffer = copy;
        }

        private static void CopyMemory(byte* pOut, byte* pIn, int length)
        {
            // Todo: Benchmark with  Buffer.MemoryCopy();
            // Todo: Benchmark with Buffer.Memove() from : https://github.com/dotnet/coreclr/blob/ea9bee5ac2f96a1ea6b202dc4094b8d418d9209c/src/mscorlib/src/System/Buffer.cs
            var nbSteps = length / Kernel.OPTIMAL_MEMCPY_SIZE;
            for (var i = 0; i < nbSteps; i++)
            {
                Buffer.MemoryCopy(pIn, pOut, Kernel.OPTIMAL_MEMCPY_SIZE, Kernel.OPTIMAL_MEMCPY_SIZE);
                pIn += Kernel.OPTIMAL_MEMCPY_SIZE;
                pOut += Kernel.OPTIMAL_MEMCPY_SIZE;
            }
            var remainingSize = length - nbSteps * Kernel.OPTIMAL_MEMCPY_SIZE;
            if (remainingSize > 0)
                Buffer.MemoryCopy(pIn, pOut, Kernel.OPTIMAL_MEMCPY_SIZE, Kernel.OPTIMAL_MEMCPY_SIZE);
        }
    }
}
