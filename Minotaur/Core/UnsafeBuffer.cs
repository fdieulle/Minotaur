using System;
using System.Runtime.InteropServices;

namespace Minotaur.Core
{
    // Todo: Use IAllocator inside
    public unsafe class UnsafeBuffer : IDisposable
    {
        private GCHandle _handle;

        public int Length => Data.Length;

        public byte[] Data { get; private set; }

        public byte* Ptr { get; private set; }

        public UnsafeBuffer(int size)
        {
            UpdateSize(size);
            Reset();
        }

        public byte* Offset { get; set; }

        public byte* End { get; set; }
        
        public void UpdateSize(int size)
        {
            if (_handle.IsAllocated)
                _handle.Free();

            Data = new byte[size];
            _handle = GCHandle.Alloc(Data, GCHandleType.Pinned);
            Ptr = (byte*)_handle.AddrOfPinnedObject();
        }

        public void Reset() => Offset = End = Ptr;

        #region IDisposable

        public void Dispose()
        {
            if(_handle.IsAllocated)
                _handle.Free();
        }

        #endregion
    }
}