using System;
using Minotaur.Codecs;

namespace Minotaur.Pocs.Codecs
{
    public static unsafe class CodecExt
    {
        #region MinDelta U64

        public static int GetMaxEncodedSizeForMinDeltaU64(int count)
            => Codec.MAX_UINT64_LENGTH * (count + 1) + sizeof(int);

        public static void EncodeMinDeltaU64(byte* src, int len, int skip, ref byte* dst)
        {
            var start = src;
            var end = src + len;
            var next = sizeof(ulong) + skip;

            var minDelta = ulong.MaxValue;
            src += next;
            while (src < end)
            {
                minDelta = Math.Min(minDelta, *(ulong*)src - *(ulong*)(src - next));
                src += next;
            }

            src = start;
            start = dst;
            dst += sizeof(int);
            Codec.EncodeUInt64(*(ulong*)src, ref dst);
            Codec.EncodeUInt64(minDelta, ref dst);

            src += next;
            while (src < end)
            {
                Codec.EncodeUInt64(*(ulong*)src - *(ulong*)(src - next) - minDelta, ref dst);
                src += next;
            }

            *(int*)start = (int)(dst - start);
        }

        public static void DecodeMinDeltaU64(ref byte* src, int skip, byte* dst)
        {
            var len = *(int*)src;
            var end = src + len;
            src += sizeof(int);
            var next = sizeof(ulong) + skip;

            *(ulong*)dst = Codec.DecodeUInt64(ref src);
            var minDelta = Codec.DecodeUInt64(ref src);
            dst += next;

            while (src < end)
            {
                *(ulong*)dst = *(ulong*)(dst - next) + minDelta + Codec.DecodeUInt64(ref src);
                dst += next;
            }
        }

        #endregion

        #region MinDelta U32

        public static int GetMaxEncodedSizeForMinDeltaU32(int count)
            => Codec.MAX_UINT32_LENGTH * (count + 1) + sizeof(int);

        public static void EncodeMinDeltaU32(byte* src, int len, int skip, ref byte* dst)
        {
            var start = src;
            var end = src + len;
            var next = sizeof(uint) + skip;

            var minDelta = uint.MaxValue;
            src += next;
            while (src < end)
            {
                minDelta = Math.Min(minDelta, *(uint*)src - *(uint*)(src - next));
                src += next;
            }

            src = start;
            start = dst;
            dst += sizeof(int);
            Codec.EncodeUInt32(*(uint*)src, ref dst);
            Codec.EncodeUInt32(minDelta, ref dst);

            src += next;
            while (src < end)
            {
                Codec.EncodeUInt32(*(uint*)src - *(uint*)(src - next) - minDelta, ref dst);
                src += next;
            }

            *(int*)start = (int)(dst - start);
        }

        public static void DecodeMinDeltaU32(ref byte* src, int skip, byte* dst)
        {
            var len = *(int*)src;
            var end = src + len;
            src += sizeof(int);
            var next = sizeof(uint) + skip;

            *(long*)dst = Codec.DecodeInt64(ref src);
            var minDelta = Codec.DecodeInt64(ref src);
            dst += next;

            while (src < end)
            {
                *(long*)dst = *(long*)(dst - next) + minDelta + Codec.DecodeInt64(ref src);
                dst += next;
            }
        }

        #endregion

        #region MinDelta 32

        public static int GetMaxEncodedSizeForMinDelta32(int count)
            => Codec.MAX_INT32_LENGTH * (count + 1) + sizeof(int);

        public static void EncodeMinDelta32(byte* src, int len, int skip, ref byte* dst)
        {
            var start = src;
            var end = src + len;
            var next = sizeof(int) + skip;

            var maxNegDelta = int.MinValue;
            var minPosDelta = int.MaxValue;

            src += next;
            while (src < end)
            {
                var delta = *(int*)src - *(int*)(src - next);
                if (delta < 0) maxNegDelta = Math.Max(maxNegDelta, delta);
                else minPosDelta = Math.Min(minPosDelta, delta);

                src += next;
            }

            src = start;
            start = dst;
            maxNegDelta = -maxNegDelta;

            dst += sizeof(int);
            Codec.EncodeInt32(*(int*)src, ref dst);
            Codec.EncodeInt32(maxNegDelta, ref dst);
            Codec.EncodeInt32(minPosDelta, ref dst);

            src += next;
            while (src < end)
            {
                var delta = *(int*)src - *(int*)(src - next);
                if (delta < 0) Codec.EncodeInt32(true, -delta - maxNegDelta, ref dst);
                else Codec.EncodeInt32(false, delta - minPosDelta, ref dst);

                src += next;
            }

            *(int*)start = (int)(dst - start);
        }

        public static void DecodeMinDelta32(ref byte* src, int skip, byte* dst)
        {
            var len = *(int*)src;
            var end = src + len;
            src += sizeof(int);
            var next = sizeof(int) + skip;

            *(int*)dst = Codec.DecodeInt32(ref src);
            var maxNegDelta = Codec.DecodeInt32(ref src);
            var minPosDelta = Codec.DecodeInt32(ref src);

            dst += next;
            while (src < end)
            {
                var delta = Codec.DecodeInt32(ref src, out var isNegative);
                if (isNegative) *(int*)dst = *(int*)(dst - next) - (delta + maxNegDelta);
                else *(int*)dst = *(int*)(dst - next) + delta + minPosDelta;

                dst += next;
            }
        }

        #endregion
    }
}
