using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Security;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace Minotaur.Core.Platform
{
    public static unsafe class Win32Platform
    {
        private const string KERNEL32 = "kernel32.dll";
        private const string MSVCRT = "msvcrt.dll";

        /// <summary>
        /// The processor architecture of the installed operating system. This member can be one of <see cref="PROCESSOR_ARCHITECTURE"/> values.
        /// </summary>
        public static readonly PROCESSOR_ARCHITECTURE ProcessorArchitecture;
        /// <summary>
        /// The number of logical processors in the current group. To retrieve this value, use the GetLogicalProcessorInformation function. => https://msdn.microsoft.com/en-us/library/windows/desktop/ms683194(v=vs.85).aspx
        /// </summary>
        public static readonly uint NumberOfProcessors;

        /// <summary>
        /// The page size and the granularity of page protection and commitment. This is the page size used by the VirtualAlloc function.
        /// </summary>
        public static readonly uint PageSize;
        /// <summary>
        /// A pointer to the lowest memory address accessible to applications and dynamic-link libraries (DLLs).
        /// </summary>
        public static readonly IntPtr MinimumApplicationAddress;
        /// <summary>
        /// A pointer to the highest memory address accessible to applications and DLLs.
        /// </summary>
        public static readonly IntPtr MaximumApplicationAddress;
        /// <summary>
        /// The granularity for the starting address at which virtual memory can be allocated. For more information, see VirtualAlloc. => https://msdn.microsoft.com/en-us/library/windows/desktop/aa366887(v=vs.85).aspx
        /// </summary>
        public static readonly uint AllocationGranularity;
        
        static Win32Platform()
        {
            GetSystemInfo(out var systemInfo);

            ProcessorArchitecture = systemInfo.processorArchitecture;
            NumberOfProcessors = systemInfo.numberOfProcessors;
            PageSize = systemInfo.pageSize;
            MinimumApplicationAddress = systemInfo.minimumApplicationAddress;
            MaximumApplicationAddress = systemInfo.maximumApplicationAddress;
            AllocationGranularity = systemInfo.allocationGranularity;
        }

        [DllImport(KERNEL32)]
        public static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);

        [DllImport(MSVCRT, EntryPoint = "memmove", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        [SecurityCritical]
        public static extern int Move(byte* dest, byte* src, long count);

        // ReSharper disable InconsistentNaming
        public enum PROCESSOR_ARCHITECTURE : ushort
        {
            Unknown = 0xffff,
            x86 = 0, // Intel x86
            ARM = 5,
            IA64 = 6, // Intel Itanium-based
            x64 = 9, // AMD or Intel
            ARM64 = 12,
        }

        /// <summary>
        /// https://msdn.microsoft.com/en-us/library/windows/desktop/ms724958(v=vs.85).aspx
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct SYSTEM_INFO
        {
            public PROCESSOR_ARCHITECTURE processorArchitecture;

            // ReSharper disable once FieldCanBeMadeReadOnly.Local
            ushort reserved;

            // The page size and the granularity of page protection and commitment. This is the page size used by the VirtualAlloc function.
            public uint pageSize;
            public IntPtr minimumApplicationAddress;
            public IntPtr maximumApplicationAddress;
            // A mask representing the set of processors configured into the system. Bit 0 is processor 0; bit 31 is processor 31.
            public IntPtr activeProcessorMask;
            // The number of logical processors in the current group. To retrieve this value, use the GetLogicalProcessorInformation function.
            public uint numberOfProcessors;
            public uint processorType;
            // The granularity for the starting address at which virtual memory can be allocated. For more information, see VirtualAlloc.
            public uint allocationGranularity;
            public ushort processorLevel;
            public ushort processorRevision;
        }

        #region File

        [DllImport(KERNEL32, SetLastError = true)]
        public static extern SafeFileHandle CreateFile(
            string filePath,
            FileAccess desiredAccess,
            FileShare shareMode,
            IntPtr securityAttributes,
            FileMode creationDisposition,
            FileOptions flagsAndAttributes,
            IntPtr templateFile);

        [DllImport(KERNEL32, SetLastError = true)]
        public static extern ulong SetFilePointer(
            SafeFileHandle handle, ulong lo, ulong* hi, SeekOrigin origin);

        [DllImport(KERNEL32, SetLastError = true)]
        public static extern bool WriteFile(
            SafeHandle hFile,
            byte* lpBuffer,
            int nNumberOfBytesToWrite,
            int* lpNumberOfBytesWritten,
            NativeOverlapped* lpOverlapped);

        [DllImport(KERNEL32, SetLastError = true)]
        public static extern bool ReadFile(
            SafeHandle hFile,
            byte* lpBuffer,
            int nNumberOfBytesToRead,
            int* lpNumberOfBytesRead,
            NativeOverlapped* lpOverlapped);

        [DllImport(KERNEL32, SetLastError = true)]
        public static extern bool SetEndOfFile(SafeFileHandle handle);

        #endregion
    }
}
