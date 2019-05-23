using System;
using System.Runtime.CompilerServices;

namespace Minotaur.Pocs.Codecs
{
    public sealed unsafe class VoidCodecFullStream : ICodecFullStream
    {
        #region Implementation of ICodec

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Encode(ref byte* src, int lSrc, ref byte* dst, int lDst)
        {
            var count = Math.Min(lSrc, lDst);
            Buffer.MemoryCopy(src, dst, count, count);
            src += count;
            dst += count;
            return count;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int DecodeHead(ref byte* src, int len)
        {
            return 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
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