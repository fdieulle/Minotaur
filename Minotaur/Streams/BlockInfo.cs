namespace Minotaur.Streams
{
    public unsafe class BlockInfo<T> where T : unmanaged
    {
        public int ShellSize { get; set; }
        public int DataLength { get; set; } 
        public int PayloadLength { get; set; } 
        public int Version { get; set; } 
        public T FirstValue { get; set; }
        public T LastValue { get; set; }

        public int ItemsCount => DataLength / sizeof(T);
        public double DataCompressionRatio => 1d - PayloadLength / (double)DataLength;
        public double FullCompressionRatio => 1d - (PayloadLength + ShellSize) / (double)DataLength;
    }
}