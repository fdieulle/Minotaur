using System;
using System.Runtime.InteropServices;
using System.Security;

namespace Minotaur.Core.Platform
{
    public static unsafe class Win32Platform
    {
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

        [DllImport("kernel32.dll")]
        public static extern void GetSystemInfo(out SYSTEM_INFO lpSystemInfo);

        [DllImport("msvcrt.dll", EntryPoint = "memmove", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
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
    }
}
