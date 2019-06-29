namespace Minotaur.Codecs
{
    public unsafe interface ICodec<T>
        where T : unmanaged
    {
        /// <summary>
        /// Gets the maximum number of Bytes, for a given number of data, needed to encode.
        /// </summary>
        /// <param name="count">Number of data to encode.</param>
        /// <returns>Returns the maximum number of bytes to encode data.</returns>
        int GetMaxEncodedSize(int count);

        /// <summary>
        /// Encodes the data.
        /// </summary>
        /// <param name="src">Data to encode.</param>
        /// <param name="count">Number of data to encode.</param>
        /// <param name="dst">Buffer to store encoded data.</param>
        /// <returns>Returns the size of encoded data in Bytes.</returns>
        int Encode(T* src, int count, byte* dst);

        /// <summary>
        /// Decodes the data.
        /// </summary>
        /// <param name="src">Encoded data pointer.</param>
        /// <param name="len">Size of encoded data available to read.</param>
        /// <param name="dst">Decoded data storage.</param>
        /// <returns>Returns the number of data decoded.</returns>
        int Decode(byte* src, int len, T* dst);
    }
}
