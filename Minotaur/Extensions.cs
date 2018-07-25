using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Minotaur.IO;

namespace Minotaur
{
    public static class Extensions
    {
        #region IStream extensions

        public static unsafe int WriteAndReset(this IStream stream, Array data, int itemSize)
        {
            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            var p = (byte*)handle.AddrOfPinnedObject();
            var wrote = stream.Write(p, data.Length * itemSize);
            handle.Free();
            stream.Reset();
            return wrote;
        }

        #endregion
    }
}
