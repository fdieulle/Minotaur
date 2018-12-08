using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DotNetCross.Memory;
using Minotaur.Streams;

namespace Minotaur.Cursors
{
    public abstract unsafe class ColumnCursor<TStream> : IColumnCursor
        where TStream : IStream
    {
        private readonly FieldSnapshot* _snapshot;
        private readonly int _sizeOfFieldEntry;
        private readonly TStream _stream;

        public long Ticks
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _snapshot->Current.Ticks;
        }

        public long NextTicks
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _snapshot->Next.Ticks;
        }

        protected ColumnCursor(FieldSnapshot* snapshot, TStream stream, int sizeOfField)
        {
            _snapshot = snapshot;
            _stream = stream;
            _sizeOfFieldEntry = sizeof(long) + sizeOfField;

            if(_sizeOfFieldEntry > sizeof(FieldEntry))
                throw new ArgumentOutOfRangeException(nameof(sizeOfField), "field type can't never be greater than 8 Bytes");

            Reset();
        }

        public T GetValue<T>() where T : struct
        {
            // Todo: Check perf
            //return Unsafe.Read<T>((ulong*) _snapshot + sizeof(long));
            return _snapshot->Current.GetValue<T>();
        }

        public void MoveNext(long ticks)
        {
            while (ticks >= _snapshot->Next.Ticks)
            {
                _snapshot->Current = _snapshot->Next;
                if (_stream.Read((byte*) Unsafe.AsPointer(ref _snapshot->Next), _sizeOfFieldEntry) != _sizeOfFieldEntry)
                    _snapshot->Next.Ticks = Time.MaxTicks;
            }
        }

        public void Reset()
        {
            _snapshot->Current.Ticks = Time.MinTicks;
            _snapshot->Current.Value = GetDefaultValue();
            _snapshot->Next.Ticks = Time.MinTicks;
            _snapshot->Next.Value = GetDefaultValue();
            _stream.Reset();
        }

        protected abstract ulong GetDefaultValue();

        public void Dispose()
        {
            _stream.Dispose();
        }
    }

    public unsafe class ColumnCursor<T, TStream> : ColumnCursor<TStream>, IColumnCursor<T>
        where T : struct
        where TStream : IStream
    {
        public ColumnCursor(FieldSnapshot* snapshot, TStream stream)
            : base(snapshot, stream, Marshal.SizeOf<T>()) { }

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

        #region Overrides of ColumnCursor<TStream>

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
