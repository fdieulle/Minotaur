namespace Minotaur.Codecs
{
    public unsafe interface ICodec<T>
        where T : unmanaged
    {
        int GetMaxEncodedSize(int size);

        int Encode(T* src, int count, byte* dst);

        int Decode(byte* src, int len, T* dst);
    }
}
