using System;
using System.IO;

namespace Minotaur.Streams
{
    /// <inheritdoc />
    /// <summary>
    /// This interface describe a generic stream.
    /// </summary>
    public interface IStream : IDisposable
    {
        bool CanSeek { get; }

        int Read(byte[] buffer, int offset, int count);

        void Write(byte[] buffer, int offset, int count);

        long Seek(long offset, SeekOrigin origin);

        void Flush();
    }    
}
