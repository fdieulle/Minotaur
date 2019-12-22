using System.IO;

namespace Minotaur.IO
{
    public class MinotaurMemoryStream : MemoryStream, IStream
    {
        public MinotaurMemoryStream() { }

        public MinotaurMemoryStream(byte[] buffer) 
            : base(buffer) { }

        public MinotaurMemoryStream(byte[] buffer, bool writable)
            : base(buffer, writable) { }

        public MinotaurMemoryStream(byte[] buffer, int index, int count)
            : base(buffer, index, count) { }

        public MinotaurMemoryStream(byte[] buffer, int index, int count, bool writable)
            : base(buffer, index, count, writable) { }

        public MinotaurMemoryStream(byte[] buffer, int index, int count, bool writable, bool publiclyVisible) 
            : base(buffer, index, count, writable, publiclyVisible) { }

        public MinotaurMemoryStream(int capacity) 
            : base(capacity) { }

        public void Reset() => Seek(0, SeekOrigin.Begin);

        public long Seek(long offset) => Seek(offset, SeekOrigin.Current);
    }
}
