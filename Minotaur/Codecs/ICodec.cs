namespace Minotaur.Codecs
{
    public unsafe interface ICodec
    {
        int GetMaxEncodedSize(int size);

        int Encode(byte* src, int len, byte* dst);

        void Decode(byte* src, int len, byte* dst);
    }
}
