using System;
using System.Runtime.InteropServices;
using Minotaur.Streams;

namespace Minotaur.Cursors
{
    /// <summary>
    /// Tis class is more an Helper than a concrete implementation
    /// </summary>
    public unsafe class PinnedColumnCursor<T, TStream> : IColumnCursor<T>
        where T : struct
        where TStream : IStream

    {
        private GCHandle _handle;
        private readonly IColumnCursor<T> _underlying;

        public PinnedColumnCursor(Func<IntPtr, TStream, IColumnCursor<T>> factory, TStream stream)
        {
            var snapshot = new byte[sizeof(FieldSnapshot)];
            _handle = GCHandle.Alloc(snapshot, GCHandleType.Pinned);
            _underlying = factory(_handle.AddrOfPinnedObject(), stream);
        }

        public void MoveNext(long ticks)
        {
            _underlying.MoveNext(ticks);
        }

        public void Reset()
        {
            _underlying.Reset();
        }

        public DateTime Timestamp => _underlying.Timestamp;
        public T Value => _underlying.Value;

        public void Dispose()
        {
            _underlying.Dispose();

            if (_handle.IsAllocated)
                _handle.Free();
        }
    }
}
