using System.Runtime.InteropServices;
using System.Security;

namespace Minotaur.Core.Platform
{
    public static unsafe class PosixPlatform
    {
        private const string LIBC_6 = "libc";

        [DllImport(LIBC_6, EntryPoint = "memmove", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        [SecurityCritical]
        public static extern int Move(byte* dest, byte* src, long count);
    }
}
