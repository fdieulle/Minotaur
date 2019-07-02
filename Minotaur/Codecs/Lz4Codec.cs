using K4os.Compression.LZ4;

namespace Minotaur.Codecs
{
    public unsafe class Lz4Codec<T> : ICodec<T> 
        where T : unmanaged
    {
        #region Implementation of ICodec<T>

        public int GetMaxEncodedSize(int count) => LZ4Codec.MaximumOutputSize(count * sizeof(T));

        public int Encode(T* src, int count, byte* dst)
        {
            *(int*) dst = count;
            dst += sizeof(int);
            return LZ4Codec.Encode((byte*)src, count * sizeof(T), dst, GetMaxEncodedSize(count), LZ4Level.L12_MAX);
        } 

        public int Decode(byte* src, int len, T* dst)
        {
            var count = *(int*) src;
            src += sizeof(int);
            return LZ4Codec.Decode(src, len, (byte*) dst, count * sizeof(T)) / sizeof(T);
        } 

        #endregion
    }
}
