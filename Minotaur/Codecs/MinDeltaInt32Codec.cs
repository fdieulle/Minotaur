using System;
using Minotaur.Native;

namespace Minotaur.Codecs
{
    /// <summary>
    /// This codec encode by distinguish ticks and values.
    /// For both we compute first the delta and track which is the minimum delta.
    /// After this minimum found we store the the delta between 2 consecutive value minus the minimum.
    /// The  goal of this compression algorithm is to keep the minimum value and store them with leading zero compression.
    /// </summary>
    public unsafe class MinDeltaInt32Codec : ICodec<Int32Entry>
    {
        #region Implementation of ICodec2

        public int GetMaxEncodedSize(int count)
        {
            return Codec.MAX_INT64_LENGTH * (count + 1) 
                 + Codec.MAX_INT32_LENGTH * (count + 2);
        }

        public int Encode(Int32Entry* src, int count, byte* dst)
        {
            if (count <= 0) return count;

            var minDeltaTicks = long.MaxValue;
            var maxNegDeltaValue = int.MinValue;
            var minPosDeltaValue = int.MaxValue;

            for (var i = 1; i < count; i++)
            {
                minDeltaTicks = Math.Min(minDeltaTicks, (src + i)->ticks - (src + i - 1)->ticks);

                var delta = (src + i)->value - (src + i - 1)->value;
                if (delta < 0) maxNegDeltaValue = Math.Max(maxNegDeltaValue, delta);
                else minPosDeltaValue = Math.Min(minPosDeltaValue, delta);
            }

            var start = dst;
            maxNegDeltaValue = -maxNegDeltaValue;

            // Encodes header
            Codec.EncodeInt64(src->ticks, ref dst);
            Codec.EncodeInt64(minDeltaTicks, ref dst);

            Codec.EncodeInt32(src->value, ref dst);
            Codec.EncodeInt32(maxNegDeltaValue, ref dst);
            Codec.EncodeInt32(minPosDeltaValue, ref dst);

            // Encodes data block
            for (var i = 1; i < count; i++)
            {
                Codec.EncodeInt64(((src + i)->ticks - (src + i - 1)->ticks) - minDeltaTicks, ref dst);

                var delta = (src + i)->value - (src + i - 1)->value;
                if (delta < 0) Codec.EncodeInt32(true, -delta - maxNegDeltaValue, ref dst);
                else Codec.EncodeInt32(false, delta - minPosDeltaValue, ref dst);
            }

            return (int)(dst - start);
        }

        public int Decode(byte* src, int len, Int32Entry* dst)
        {
            if (len <= 0) return len;

            var end = src + len;

            // Decodes header
            dst->ticks = Codec.DecodeInt64(ref src);
            var minDeltaTicks = Codec.DecodeInt64(ref src);

            dst->value = Codec.DecodeInt32(ref src);
            var maxNegDeltaValue = Codec.DecodeInt32(ref src);
            var minPosDeltaValue = Codec.DecodeInt32(ref src);

            // Decodes data block
            var start = dst;
            dst++;
            while (src < end)
            {
                dst->ticks = (dst - 1)->ticks + Codec.DecodeInt64(ref src) + minDeltaTicks;

                var delta = Codec.DecodeInt32(ref src, out var isNegative);
                if(isNegative) dst->value = (dst - 1)->value - (delta + maxNegDeltaValue);
                else dst->value = (dst - 1)->value + delta + minPosDeltaValue;

                dst++;
            }

            return (int)(dst - start);
        }

        #endregion
    }
}