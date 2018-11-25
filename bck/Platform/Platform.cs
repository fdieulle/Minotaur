using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Minotaur.Core.Platform
{
    public static unsafe class Platform
    {
        public static readonly bool Is64Bits = IntPtr.Size == sizeof(long);

        public static readonly bool IsLinux = RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
        public static readonly bool IsMacOsX = RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        public static readonly bool IsWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
        public static readonly bool IsPosix = IsLinux || IsMacOsX;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int Move(byte* dest, byte* src, long count)
        {
            return IsPosix
                ? PosixPlatform.Move(dest, src, count)
                : Win32Platform.Move(dest, src, count);
        }
    }


    public interface IPlatform { }
    public struct Win32 : IPlatform { }
    public struct Posix : IPlatform { }
}
