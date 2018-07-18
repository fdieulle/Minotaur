using System;

namespace Minotaur.Codecs
{
    public unsafe class VoidCodec : ICodec
    {
        #region Implementation of ICodec

        public int Encode(ref byte* src, int lSrc, ref byte* dst, int lDst)
        {
            var count = Math.Min(lSrc, lDst);
            Buffer.MemoryCopy(src, dst, count, count);
            src += count;
            dst += count;
            return count;
        }

        public int DecodeHead(ref byte* src, int len)
        {
            return 0;
        }

        public int Decode(ref byte* src, int lSrc, ref byte* dst, int lDst)
        {
            var count = Math.Min(lSrc, lDst);
            Buffer.MemoryCopy(src, dst, count, count);
            src += count;
            dst += count;
            return count;
        }

        #endregion
    }
}
