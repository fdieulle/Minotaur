using System;
using System.Runtime.CompilerServices;

namespace Minotaur.Codecs
{
    public sealed unsafe class VoidCodec : ICodec
    {
        #region Implementation of ICodec

        public int GetMaxEncodedSize(int size) => size;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Encode(byte* src, int lSrc, byte* dst)
        {
            Buffer.MemoryCopy(src, dst, lSrc, lSrc);
            return lSrc;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Decode(byte* src, int lSrc, byte* dst) 
            => Buffer.MemoryCopy(src, dst, lSrc, lSrc);

        #endregion
    }
}
