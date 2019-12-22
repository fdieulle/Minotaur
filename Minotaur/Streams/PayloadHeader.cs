using System.Runtime.InteropServices;

namespace Minotaur.Streams
{
    [StructLayout(LayoutKind.Explicit, Size = 9)]
    public struct PayloadHeader
    {
        [field: FieldOffset(0)]
        public int PayloadLength { get; set; }

        [field: FieldOffset(4)]
        public int DataLength { get; set; }

        [field: FieldOffset(8)]
        public byte Version { get; set; }
    }
}