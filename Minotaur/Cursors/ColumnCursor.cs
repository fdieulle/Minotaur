using System;
using System.Runtime.CompilerServices;
using Minotaur.Core;
using Minotaur.Native;
using Minotaur.Streams;

namespace Minotaur.Cursors
{
    public unsafe class ColumnCursor<TEntry, T, TStream> : IColumnCursor<T>
        where TEntry : unmanaged, IFieldEntry<T>
        where TStream : IStream
    {
        private readonly TEntry* _snapshot;
        private readonly TStream _stream;
        private readonly IAllocator _allocator;

        public long Ticks
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => *(long*)_snapshot;
        }

        public long NextTicks
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => *(long*)(_snapshot + 1);
        }

        public ColumnCursor(IAllocator allocator, TStream stream)
        {
            _snapshot = allocator.Allocate<TEntry>(2);
            _allocator = allocator;
            _stream = stream;

            Reset();
        }

        public void MoveNext(long ticks)
        {
            while (ticks >= *(long*)(_snapshot + 1))
            {
                *_snapshot = *(_snapshot + 1); // Todo: Perf can we do this copy once ? instead of each tick jump
                if (_stream.Read((byte*)(_snapshot + 1), sizeof(TEntry)) != sizeof(TEntry))
                    *(long*)(_snapshot + 1) = Time.MaxTicks;
            }
        }

        public void Reset()
        {
            _snapshot->Reset();
            (_snapshot + 1)->Reset();
            _stream.Reset();
        }

        public void Dispose()
        {
            _allocator.Free(_snapshot);
            _stream.Dispose();
        } 

        #region Implementation of IFieldProxy<out T>

        public DateTime Timestamp
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => *(DateTime*)_snapshot;
        }

        public T Value
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _snapshot->Value;
        }

        #endregion
    }
}
