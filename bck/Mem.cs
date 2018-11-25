using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Xml.Serialization;

namespace Minotaur.Core
{
    public static unsafe class Mem
    {
        public const int KB = 1024;
        public const int MB = KB * KB;
        public const int GB = KB * MB;
        public const long TB = KB * (long)GB;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ReadU16(void* p) => *(ushort*)p;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static short Read16(void* p) => *(short*)p;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ReadU32(void* p) => *(uint*)p;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Read32(void* p) => *(int*)p;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ReadU64(void* p) => *(ulong*)p;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long Read64(void* p) => *(long*)p;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(void* p, ushort v) => *(ushort*)p = v;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(void* p, short v) => *(short*)p = v;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(void* p, uint v) => *(uint*)p = v;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(void* p, int v) => *(int*)p = v;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(void* p, ulong v) => *(ulong*)p = v;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Write(void* p, long v) => *(long*)p = v;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void WildCopy(byte* dest, byte* src, byte* destEnd)
        {
            do
            {
                ((ulong*)dest)[0] = ((ulong*)src)[0];
                if (dest + 1 * sizeof(ulong) >= destEnd)
                    goto Return;

                ((ulong*)dest)[1] = ((ulong*)src)[1];
                if (dest + 2 * sizeof(ulong) >= destEnd)
                    goto Return;

                ((ulong*)dest)[2] = ((ulong*)src)[2];
                if (dest + 3 * sizeof(ulong) >= destEnd)
                    goto Return;

                ((ulong*)dest)[3] = ((ulong*)src)[3];

                dest += 4 * sizeof(ulong);
                src += 4 * sizeof(ulong);
            }
            while (dest < destEnd);

            Return:
            // ReSharper disable once RedundantJumpStatement
            return;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy8(void* dst, void* src) => *(ulong*)dst = *(ulong*)src;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Copy16(byte* dst, byte* src)
        {
            *(ulong*)(dst + 0) = *(ulong*)(src + 0);
            *(ulong*)(dst + 8) = *(ulong*)(src + 0);
        }

        /// <summary>
        /// Fill block of memory with zeroes.
        /// </summary>
        /// <param name="target">Address.</param>
        /// <param name="length">Length.</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void Zero(byte* target, int length)
        {
            while (length >= sizeof(ulong))
            {
                *(ulong*)target = 0;
                target += sizeof(ulong);
                length -= sizeof(ulong);
            }

            if (length >= sizeof(uint))
            {
                *(uint*)target = 0;
                target += sizeof(uint);
                length -= sizeof(uint);
            }

            if (length >= sizeof(ushort))
            {
                *(ushort*)target = 0;
                target += sizeof(ushort);
                length -= sizeof(ushort);
            }

            if (length > 0)
            {
                *target = 0;
            }
        }
    }
}
