using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Minotaur
{
    public static class Bits
    {
        public const int KILO_BYTE = 1024;
        public const int MEGA_BYTE = KILO_BYTE * KILO_BYTE;
        public const int GIGA_BYTE = KILO_BYTE * MEGA_BYTE;
        public const long TERA_BYTE = KILO_BYTE * (long)GIGA_BYTE;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe void XorSwap(ref byte* x, ref byte* y)
        {
            x = (byte*)((ulong)x ^ (ulong)y);
            y = (byte*)((ulong)y ^ (ulong)x);
            x = (byte*)((ulong)x ^ (ulong)y);
        }

        private static readonly byte[] deBruijnBytePos64 =
        {
            0, 0, 0, 0, 0, 1, 1, 2, 0, 3, 1, 3, 1, 4, 2, 7, 0, 2, 3, 6, 1, 5, 3, 5, 1, 3, 4, 4, 2, 5, 6, 7,
            7, 0, 1, 2, 3, 3, 4, 6, 2, 6, 5, 5, 3, 4, 5, 6, 7, 1, 2, 4, 6, 4, 4, 5, 7, 2, 6, 5, 7, 6, 7, 7
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int TrailingZeroesInBytes(ulong value)
        {
            return deBruijnBytePos64[((value & (ulong)(-(long)value)) * 0x0218A392CDABBD3FUL) >> 58];
        }
    }
}
