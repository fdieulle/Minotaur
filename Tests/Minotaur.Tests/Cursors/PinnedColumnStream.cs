using System.IO;
using System.Runtime.InteropServices;
using Minotaur.Codecs;
using Minotaur.IO;
using MemoryStream = Minotaur.IO.MemoryStream;

namespace Minotaur.Tests.Cursors
{
    public unsafe class PinnedColumnStream : IStream
    {
        private readonly ColumnStream<MemoryStream, ICodec> _underlying;
        private GCHandle _handle;

        public PinnedColumnStream(MemoryStream stream, ICodec codec, int bufferLength)
        {
            var buffer = new byte[bufferLength];
            _handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);

            var ptr = (byte*)_handle.AddrOfPinnedObject();
            _underlying = new ColumnStream<MemoryStream, ICodec>(stream, codec, ptr, bufferLength);
        }

        public int Read(byte* p, int length)
        {
            return _underlying.Read(p, length);
        }

        public int Write(byte* p, int length)
        {
            return _underlying.Write(p, length);
        }

        public int Seek(int seek, SeekOrigin origin)
        {
            return _underlying.Seek(seek, origin);
        }

        public void Reset()
        {
            _underlying.Reset();
        }

        public void Dispose()
        {
            _underlying.Dispose();

            if (_handle.IsAllocated)
                _handle.Free();
        }
    }
}
