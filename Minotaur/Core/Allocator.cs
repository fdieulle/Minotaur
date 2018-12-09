using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Minotaur.Core
{
    public unsafe interface IAllocator
    {
        byte* Allocate(int length);

        void Free(byte* ptr);
    }

    public static unsafe class AllocatorExtensions
    {
        public static T* Allocate<T>(this IAllocator allocator, int count)
            where T : unmanaged => (T*)allocator.Allocate(sizeof(T) * count);

        public static void Free<T>(this IAllocator allocator, T* ptr)
            where T : unmanaged => allocator.Free((byte*)ptr);
    }

    public unsafe class DummyPinnedAllocator : IAllocator
    {
        private readonly Dictionary<IntPtr, GCHandle> _pointers = new Dictionary<IntPtr, GCHandle>();

        #region Implementation of IAllocator

        public byte* Allocate(int length)
        {
            var data = new byte[length];
            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);

            var ptr = handle.AddrOfPinnedObject();
            _pointers[ptr] = handle;
            return (byte*) ptr;
        }

        public void Free(byte* ptr)
        {
            if(_pointers.TryGetValue((IntPtr)ptr, out var handle) && handle.IsAllocated)
                handle.Free();
            _pointers.Remove((IntPtr) ptr);
        }

        #endregion
    }
}
