using System;
using System.Collections.Generic;
using System.IO;
using Minotaur.Core;

namespace Minotaur.Streams
{
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

        public void Reset()
        {
            Seek(0, SeekOrigin.Begin);
        }
    }

    public class MinotaurFileStream : IStream
    {
        private readonly IEnumerator<string> _enumerator;
        private readonly IDisposable _fileLocker;
        private FileStream _current;

        public long Position => _current?.Position ?? 0;
        public long Length => _current?.Length ?? 0;

        /// <summary>
        /// Reader Ctor.
        /// </summary>
        public MinotaurFileStream(IEnumerable<string> filePaths)
        {
            _enumerator = filePaths.GetEnumerator();
        }

        /// <summary>
        /// Writer ctor
        /// </summary>
        public MinotaurFileStream(string filePath)
        {
            filePath.GetFolderPath().CreateFolderIfNotExist();
            _fileLocker = filePath.FileLock();
            _current = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.Read, 1);
            //??_current.SetLength(length);
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            var read = 0;

            while (_current == null || (read = _current.Read(buffer, offset, count)) == 0)
            {
                _current?.Dispose();

                do
                {
                    if (!_enumerator.MoveNext())
                        return read;
                }
                while (!_enumerator.Current.FileExists());

                if (_enumerator.Current != null)
                    _current = new FileStream(_enumerator.Current, FileMode.Open, FileAccess.Read, FileShare.Read, 1);
                else break;
            }

            return read;
        }

        public void Write(byte[] buffer, int offset, int count)
        {
            _current.Write(buffer, offset, count);
        }

        public void Reset()
        {
            // Reset the writer part
            if (_current != null && _current.CanSeek)
                _current.Seek(0, SeekOrigin.Begin);

            // Reset the reader part
            if (_enumerator != null && _current != null)
            {
                _enumerator.Reset();
                _current = null;
            }
        }

        public void Flush()
        {
            _current?.Flush();
        }

        public void Dispose()
        {
            _current?.Dispose();
            _fileLocker?.Dispose();
        }
    }
}
