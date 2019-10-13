using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Win32.SafeHandles;

namespace Minotaur.Streams
{
    public class MultiStream<TStream> : IStream
        where TStream : IStream
    {
        private readonly IEnumerator<TStream> _enumerator;
        private TStream _current;

        public MultiStream(IEnumerable<TStream> streams)
        {
            _enumerator = streams.GetEnumerator();
        }

        #region Implementation of IStream
        
        public bool CanSeek { get; } = true;

        public int Read(byte[] buffer, int offset, int count)
        {
            var read = 0;

            while (_current == null || (read = _current.Read(buffer, offset, count)) == 0)
            {
                _current?.Dispose();

                if (_enumerator.MoveNext())
                    _current = _enumerator.Current;
                else break;
            }

            return read;
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            throw new NotImplementedException();
        }

        public long Seek(long offset, SeekOrigin origin)
        {
            long seek = 0;
            if(origin == SeekOrigin.Begin)
                _enumerator.Reset();

            if (origin != SeekOrigin.End)
            {
                while (_current == null || (seek += _current.Seek(offset - seek, origin)) < offset)
                {
                    _current?.Dispose();

                    if (_enumerator.MoveNext())
                        _current = _enumerator.Current;
                    else break;
                }
            }
            else
            {
                _enumerator.Reset();
                var queue = new Queue<TStream>();
                while (_enumerator.MoveNext())
                    queue.Enqueue(_enumerator.Current);
                while (queue.Count > 0)
                {
                    _current = queue.Dequeue();
                    seek += _current.Seek(offset - seek, origin);
                    if(seek >= offset) break;
                }
            }

            return seek;
        }

        public void Flush() => _current.Flush();

        #endregion

        #region Implementation of IDisposable

        public void Dispose()
        {
            _current.Dispose();
            _enumerator.Dispose();
        }

        #endregion
    }

    

    public class MinotaurMemoryStream : System.IO.MemoryStream, IStream
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
    }

    public class MinotaurFileStream : FileStream, IStream {
        public MinotaurFileStream(SafeFileHandle handle, FileAccess access) 
            : base(handle, access) { }

        public MinotaurFileStream(SafeFileHandle handle, FileAccess access, int bufferSize) 
            : base(handle, access, bufferSize) { }

        public MinotaurFileStream(SafeFileHandle handle, FileAccess access, int bufferSize, bool isAsync) 
            : base(handle, access, bufferSize, isAsync) { }

        public MinotaurFileStream(string path, FileMode mode) 
            : base(path, mode) { }

        public MinotaurFileStream(string path, FileMode mode, FileAccess access) 
            : base(path, mode, access) { }

        public MinotaurFileStream(string path, FileMode mode, FileAccess access, FileShare share) 
            : base(path, mode, access, share) { }

        public MinotaurFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize) 
            : base(path, mode, access, share, bufferSize) { }

        public MinotaurFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, bool useAsync) 
            : base(path, mode, access, share, bufferSize, useAsync) { }

        public MinotaurFileStream(string path, FileMode mode, FileAccess access, FileShare share, int bufferSize, FileOptions options) 
            : base(path, mode, access, share, bufferSize, options) { }
    }
}
