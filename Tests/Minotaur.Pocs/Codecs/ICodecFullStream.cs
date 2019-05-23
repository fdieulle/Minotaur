namespace Minotaur.Pocs.Codecs
{
    /// <summary>
    /// Defines the interface to encode and decode data.
    /// </summary>
    public unsafe interface ICodecFullStream
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
        /// Decode head which correspond to a staring block.
        /// Todo: Maybe we can remove this method but for now we keep it to recognize a block's begin.
        /// </summary>
        /// <param name="src"></param>
        /// <param name="len"></param>
        /// <returns>Returns the number of byte read from src.</returns>
        int DecodeHead(ref byte* src, int len);

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