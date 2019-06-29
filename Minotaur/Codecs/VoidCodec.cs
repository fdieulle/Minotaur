using System;
using System.Runtime.CompilerServices;

namespace Minotaur.Codecs
{
    public sealed unsafe class VoidCodec<T> : ICodec<T>
        where T : unmanaged
    {
        #region Implementation of ICodec

        public int GetMaxEncodedSize(int count) => count * sizeof(T);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Encode(T* src, int count, byte* dst)
        {
            Buffer.MemoryCopy(src, dst, count * sizeof(T), count * sizeof(T));
            return count * sizeof(T);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int Decode(byte* src, int lSrc, T* dst)
        {
            Buffer.MemoryCopy(src, dst, lSrc, lSrc);
            return lSrc / sizeof(T);
        } 

        #endregion
    }
}
