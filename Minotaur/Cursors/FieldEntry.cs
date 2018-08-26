using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DotNetCross.Memory;

namespace Minotaur.Cursors
{
    [StructLayout(LayoutKind.Explicit, Size = 16)]
    public unsafe struct FieldEntry
    {
        [FieldOffset(0)]
        private long _ticks;
        [FieldOffset(8)]
        private ulong _value;

        public long Ticks
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _ticks;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _ticks = value;
        }

        public ulong Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _value;
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            set => _value = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetValue<T>() where T : struct
        {
            return Unsafe.Read<T>(Unsafe.AsPointer(ref _value));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue<T>(T value) where T : struct
        {
            Unsafe.Write(Unsafe.AsPointer(ref _value), value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetValue<T>(ref T value) where T : struct
        {
            Unsafe.Write(Unsafe.AsPointer(ref _value), ref value);
        }
    }
}