using System;
using System.Runtime.InteropServices;

namespace Minotaur.Cursors
{
    public class PinnedFieldCursor<T> : IFieldCursor<T>
        where T : struct
    {
        private GCHandle _snapHandle;
        private GCHandle _bufHandle;
        private readonly IFieldCursor<T> _underlying;

        public PinnedFieldCursor(int snapshotSize, Func<IntPtr, IFieldCursor<T>> factory)
        {
            var snapshot = new byte[snapshotSize];
            _snapHandle = GCHandle.Alloc(snapshot, GCHandleType.Pinned);
            _underlying = factory(_snapHandle.AddrOfPinnedObject());
        }

        public PinnedFieldCursor(int snapshotSize, int bufferSize, Func<IntPtr, IntPtr, int, IFieldCursor<T>> factory)
        {
            var snapshot = new byte[snapshotSize];
            _snapHandle = GCHandle.Alloc(snapshot, GCHandleType.Pinned);
            var buffer = new byte[bufferSize];
            _bufHandle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            _underlying = factory(_snapHandle.AddrOfPinnedObject(), _bufHandle.AddrOfPinnedObject(), bufferSize);
        }

        public bool Next(long ticks)
        {
            return _underlying.Next(ticks);
        }

        public bool Reset()
        {
            return _underlying.Reset();
        }

        public DateTime Timestamp => _underlying.Timestamp;
        public T Value => _underlying.Value;

        public void Dispose()
        {
            _underlying.Dispose();

            if (_snapHandle.IsAllocated)
                _snapHandle.Free();
            if (_bufHandle.IsAllocated)
                _bufHandle.Free();
        }
    }
}
