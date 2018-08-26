using System;
using System.Data;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DotNetCross.Memory;
using Minotaur.IO;
using Minotaur.Native;

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

    [StructLayout(LayoutKind.Sequential)]
    public struct FieldSnapshot
    {
        public FieldEntry Current;
        public FieldEntry Next;
    }

    [StructLayout(LayoutKind.Sequential)]
    public abstract unsafe class FieldCursor<TStream> : IFieldCursor
        where TStream : IStream
    {
        private readonly FieldSnapshot* _snaphot;
        private readonly int _sizeOfFieldEntry;
        private readonly TStream _stream;

        public long Ticks
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _snaphot->Current.Ticks;
        }

        public long NextTicks
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _snaphot->Next.Ticks;
        }

        protected FieldCursor(FieldSnapshot* snaphot, TStream stream, int sizeOfField)
        {
            _snaphot = snaphot;
            _stream = stream;
            _sizeOfFieldEntry = sizeof(long) + sizeOfField;

            if(_sizeOfFieldEntry > sizeof(FieldEntry))
                throw new ArgumentOutOfRangeException(nameof(sizeOfField), "field type can't never be greater than 8 Bytes");

            Reset();
        }

        public T GetValue<T>() where T : struct
        {
            // Todo: Check perf
            //return Unsafe.Read<T>((ulong*) _snaphot + sizeof(long));
            return _snaphot->Current.GetValue<T>();
        }

        public void MoveNext(long ticks)
        {
            while (ticks >= _snaphot->Next.Ticks)
            {
                _snaphot->Current = _snaphot->Next;
                if (_stream.Read((byte*) Unsafe.AsPointer(ref _snaphot->Next), _sizeOfFieldEntry) != _sizeOfFieldEntry)
                    _snaphot->Next.Ticks = Time.MaxTicks;
            }
        }

        public void Reset()
        {
            _snaphot->Current.Ticks = Time.MinTicks;
            _snaphot->Current.Value = GetDefaultValue();
            _snaphot->Next.Ticks = Time.MinTicks;
            _snaphot->Next.Value = GetDefaultValue();
            _stream.Reset();
        }

        protected abstract ulong GetDefaultValue();

        public void Dispose()
        {
            _stream.Dispose();
        }
    }

    public unsafe class FieldCursor<T, TStream> : FieldCursor<TStream>, IFieldCursor<T>
        where T : struct
        where TStream : IStream
    {
        public FieldCursor(FieldSnapshot* snpashot, TStream stream)
            : base(snpashot, stream, Marshal.SizeOf<T>()) { }

        #region Implementation of IFieldProx<T>

        public DateTime Timestamp
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new DateTime(Ticks);
        }

        public T Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetValue<T>();
        }

        #endregion

        #region Overrides of FieldCursor<TStream>

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected sealed override ulong GetDefaultValue()
        {
            if (typeof(T) == typeof(double))
            {
                var v = double.NaN;
                return *(ulong*)&v;
            }

            if (typeof(T) == typeof(float))
            {
                var v = float.NaN;
                return *(ulong*)&v;
            }

            return 0;
        }

        #endregion
    }
}
