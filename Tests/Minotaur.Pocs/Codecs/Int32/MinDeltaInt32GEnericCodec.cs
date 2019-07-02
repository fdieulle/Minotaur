using System;
using System.Collections.Generic;
using System.Text;
using Minotaur.Codecs;
using Minotaur.Native;

namespace Minotaur.Pocs.Codecs.Int32
{
    public unsafe class MinDeltaInt32GenericCodec : ICodec<Int32Entry>
    {
        #region Implementation of ICodec2

        public int GetMaxEncodedSize(int count)
        {
            return CodecExt.GetMaxEncodedSizeForMinDeltaU64(count)
                 + Codec.MAX_INT32_LENGTH * (count + 2);
        }

        public int Encode(Int32Entry* src, int count, byte* dst)
        {
            var start = dst;

            *(int*)dst = count;
            dst += sizeof(int);

            CodecExt.EncodeMinDeltaU64((byte*) src, count * sizeof(Int32Entry), sizeof(int), ref dst);
            CodecExt.EncodeMinDelta32((byte*) (src + sizeof(long)), count * sizeof(Int32Entry) - sizeof(long), sizeof(long), ref dst);

            return (int)(dst - start);
        }

        public int Decode(byte* src, int len, Int32Entry* dst)
        {
            var count = *(int*) src;
            src += sizeof(int);

            // Decodes header
            CodecExt.DecodeMinDeltaU64(ref src, sizeof(int), (byte*)dst);
            CodecExt.DecodeMinDelta32(ref src, sizeof(long), (byte*) (dst + sizeof(long)));

            return count;
        }

        #endregion
    }
}
