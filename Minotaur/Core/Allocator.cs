using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Minotaur.Core
{
    public unsafe interface IAllocator : IDisposable
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

        #region Implementation of IDisposable

        public void Dispose()
        {
            foreach (var handle in _pointers.Values)
                if(handle.IsAllocated)
                    handle.Free();
            _pointers.Clear();
        }

        #endregion
    }

    public unsafe class DummyUnmanagedAllocator : IAllocator
    {
        private readonly HashSet<IntPtr> _pointers = new HashSet<IntPtr>();

        #region Implementation of IAllocator

        public byte* Allocate(int length)
        {
            var ptr = Marshal.AllocHGlobal(length);
            _pointers.Add(Marshal.AllocHGlobal(length));
            return (byte*) ptr;
        }

        public void Free(byte* ptr)
        {
            var ip = (IntPtr) ptr;

            if (ip != IntPtr.Zero && _pointers.Remove(ip))
                Marshal.FreeHGlobal(ip);
        }

        #endregion

        #region Implementation of IDisposable

        public void Dispose()
        {
            foreach (var ptr in _pointers)
                if (ptr != IntPtr.Zero)
                    Marshal.FreeHGlobal(ptr);
            _pointers.Clear();
        }

        #endregion
    }
}
