using System.Runtime.InteropServices;

namespace Minotaur.Cursors
{
    [StructLayout(LayoutKind.Sequential)]
    public struct FieldSnapshot
    {
        public FieldEntry Current;
        public FieldEntry Next;
    }
}