using System;
using System.IO;

namespace Minotaur.IO
{
    /// <summary>
    /// This interface describe a generic stream.
    /// </summary>
    public unsafe interface IStream : IDisposable
    {
        /// <summary>
        /// Read bytes from the stream to th given pointer and for a given length.
        /// </summary>
        /// <param name="p">Pointer to store read bytes.</param>
        /// <param name="length">Number of bytes to read.</param>
        /// <returns>Returns the number of byte read.</returns>
        int Read(byte* p, int length);

        /// <summary>
        /// Write bytes into stream from the given pointer and for a give length.
        /// </summary>
        /// <param name="p">Pointer of data to write.</param>
        /// <param name="length">Number of bytes to write.</param>
        /// <returns>Returns the number of bytes wrote.</returns>
        int Write(byte* p, int length);

        /// <summary>
        /// Move the stream cursor.
        /// </summary>
        /// <param name="seek">Number of bytes to seek.</param>
        /// <param name="origin">Where the move take its origin.</param>
        /// <returns>Returns the number of bytes seek.</returns>
        int Seek(int seek, SeekOrigin origin);

        /// <summary>
        /// Reset the stream.
        /// </summary>
        void Reset();
    }
}
