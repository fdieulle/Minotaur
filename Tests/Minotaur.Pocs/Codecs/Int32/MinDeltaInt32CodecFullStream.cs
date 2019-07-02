using System;
using Minotaur.Codecs;
using Minotaur.Native;

namespace Minotaur.Pocs.Codecs.Int32
{
    public unsafe class MinDeltaInt32CodecFullStream : ICodecFullStream
    {
        private long _minTicks;
        private int _maxNegValue;
        private int _minPosValue;

        #region Implementation of ICodec

        public int Encode(ref byte* src, int lSrc, ref byte* dst, int lDst)
        {
            var data = (Int32Entry*)src;
            var count = lSrc / sizeof(Int32Entry) * sizeof(Int32Entry);

            var minTicks = long.MaxValue;
            var maxNegValue = int.MinValue;
            var minPosValue = int.MaxValue;
            for (var i = 0; i < count; i++)
            {
                minTicks = Math.Min(minTicks, (data + i)->ticks);
                if ((data + i)->value < 0)
                    maxNegValue = Math.Max(maxNegValue, (data + i)->value);
                else minPosValue = Math.Min(minPosValue, (data + i)->value);
            }

            maxNegValue = -maxNegValue;
            var start = dst;

            // Encode header
            Codec.EncodeInt64(minTicks, ref dst);
            Codec.EncodeInt32(maxNegValue, ref dst);
            Codec.EncodeInt32(minPosValue, ref dst);

            for (var i = 0; i < count; i++)
            {
                Codec.EncodeInt64((data + i)->ticks - minTicks, ref dst);

                if ((data + i)->value < 0)
                    Codec.EncodeInt32(true, -(data + i)->value - maxNegValue, ref dst);
                else Codec.EncodeInt32(false, (data + i)->value - minPosValue, ref dst);
            }

            return (int)(dst - start);
        }

        public int DecodeHead(ref byte* src, int len)
        {
            var start = src;
            _minTicks = Codec.DecodeInt64(ref src);
            _maxNegValue = Codec.DecodeInt32(ref src);
            _minPosValue = Codec.DecodeInt32(ref src);
            return (int)(src - start);
        }

        public int Decode(ref byte* src, int lSrc, ref byte* dst, int lDst)
        {
            var start = dst;
            var data = (Int32Entry*) dst;
            var end = src + lSrc;
            while (src < end)
            {
                data->ticks = _minTicks + Codec.DecodeInt64(ref src);

                data->value = Codec.DecodeInt32(ref src, out var isNegative);
                if (isNegative) data->value = -(_maxNegValue + data->value);
                else data->value += _minPosValue;

                data++;
            }

            return (int)((byte*)data - start);
        }

        #endregion
    }
}