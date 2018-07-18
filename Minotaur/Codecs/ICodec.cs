namespace Minotaur.Codecs
{
    /// <summary>
    /// Defines the interface to encode and decode data.
    /// </summary>
    public unsafe interface ICodec
    {
        /// <summary>
        /// Encode data from src to dst.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="lSrc"></param>
        /// <param name="dst"></param>
        /// <param name="lDst"></param>
        /// <returns>Number of data written</returns>
        int Encode(ref byte* src, int lSrc, ref byte* dst, int lDst);

        /// <summary>
        /// Decode data from src to dst.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="lSrc"></param>
        /// <param name="dst"></param>
        /// <param name="lDst"></param>
        /// <returns>Number of data read</returns>
        int Decode(ref byte* src, int lSrc, ref byte* dst, int lDst);
    }
}
