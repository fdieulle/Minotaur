using System;
using System.Runtime.InteropServices;
using Minotaur.IO;

namespace Minotaur
{
    public static class Extensions
    {
        #region IStream extensions

        public static unsafe int Write<TStream>(this TStream stream, Array data, int itemSize)
            where TStream : IStream
        {
            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            var p = (byte*)handle.AddrOfPinnedObject();
            var wrote = stream.Write(p, data.Length * itemSize);
            handle.Free();
            return wrote;
        }

        public static int WriteAndReset<TStream>(this TStream stream, Array data, int itemSize)
            where TStream : IStream
        {
            var wrote = stream.Write(data, itemSize);
            stream.Flush();
            stream.Reset();
            
            return wrote;
        }

        #endregion
    }
}
