using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace Minotaur.Core.Platform
{
    public static class Win32ProcessorArchitecture
    {
        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool GetLogicalProcessorInformation(IntPtr buffer, ref uint returnLength);

        public enum LOGICAL_PROCESSOR_RELATIONSHIP
        {
            RelationProcessorCore = 0,
            RelationNumaNode = 1,
            RelationCache = 2,
            RelationProcessorPackage = 3,
            RelationGroup = 4,
            RelationAll = 0xffff
        }

        public unsafe struct GROUP_AFFINITY
        {
            /// <summary>
            /// A bitmap that specifies the affinity for zero or more processors within the specified group.
            /// </summary>
            public UIntPtr Mask;
            /// <summary>
            /// The processor group number.
            /// </summary>
            public ushort Group;
            /// <summary>
            /// This member is reserved.
            /// </summary>
            public fixed ushort Reserved[3];
        }

        public unsafe struct PROCESSOR_RELATIONSHIP
        {
            /// <summary>
            /// If the Relationship member of the <see cref="SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX"/> structure is RelationProcessorCore, this member is LTP_PC_SMT if the core has more than one logical processor, or 0 if the core has one logical processor.
            /// If the Relationship member of the <see cref="SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX"/> structure is RelationProcessorPackage, this member is always 0.
            /// </summary>
            public byte Flags;
            /// <summary>
            /// f the Relationship member of the <see cref="SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX"/> structure is RelationProcessorCore, EfficiencyClass specifies the intrinsic tradeoff between performance and power for the applicable core. A core with a higher value for the efficiency class has intrinsically greater performance and less efficiency than a core with a lower value for the efficiency class. EfficiencyClass is only nonzero on systems with a heterogeneous set of cores.
            /// If the Relationship member of the <see cref="SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX"/> structure is RelationProcessorPackage, EfficiencyClass is always 0.
            /// The minimum operating system version that supports this member is Windows 10.
            /// </summary>
            public byte EfficiencyClass;
            /// <summary>
            /// This member is reserved.
            /// </summary>
            public fixed byte Reserved[20];
            /// <summary>
            /// This member specifies the number of entries in the GroupMask array. For more information, see Remarks.
            /// </summary>
            public ushort GroupCount;
            /// <summary>
            /// An array of <see cref="GROUP_AFFINITY"/> structures. The GroupCount member specifies the number of structures in the array. Each structure in the array specifies a group number and processor affinity within the group.
            /// </summary>
            public GROUP_AFFINITY[] GroupMask;
        }

        public unsafe struct NUMA_NODE_RELATIONSHIP
        {
            /// <summary>
            /// The number of the NUMA node.
            /// </summary>
            public uint NodeNumber;
            /// <summary>
            /// This member is reserved.
            /// </summary>
            public fixed byte Reserved[20];
            /// <summary>
            /// A <see cref="GROUP_AFFINITY"/> structure that specifies a group number and processor affinity within the group.
            /// </summary>
            public GROUP_AFFINITY GroupMask;
        }

        public enum CacheLevel : byte
        {
            Unknown = 0,
            L1 = 1,
            L2 = 2,
            L3 = 3
        }

        public enum PROCESSOR_CACHE_TYPE
        {
            CacheUnified,
            CacheInstruction,
            CacheData,
            CacheTrace
        }

        public unsafe struct CACHE_RELATIONSHIP
        {
            /// <summary>
            /// The cache level.
            /// </summary>
            public CacheLevel Level;
            /// <summary>
            /// The cache associativity. If this member is CACHE_FULLY_ASSOCIATIVE (0xFF), the cache is fully associative.
            /// </summary>
            public byte Associativity;
            /// <summary>
            /// The cache line size, in bytes.
            /// </summary>
            public int LineSize;
            /// <summary>
            /// The cache size, in bytes.
            /// </summary>
            public uint CacheSize;
            /// <summary>
            /// The cache type. This member is a <see cref="PROCESSOR_CACHE_TYPE"/> value
            /// </summary>
            public PROCESSOR_CACHE_TYPE Type;
            /// <summary>
            /// This member is reserved.
            /// </summary>
            public fixed byte Reserved[20];
            /// <summary>
            /// A <see cref="GROUP_AFFINITY"/> structure that specifies a group number and processor affinity within the group.
            /// </summary>
            public GROUP_AFFINITY GroupMask;
        }

        public unsafe struct PROCESSOR_GROUP_INFO
        {
            public byte MaximumProcessorCount;
            public byte ActiveProcessorCount;
            public fixed byte Reserved[38];
            public UIntPtr ActiveProcessorMask;
        }

        public unsafe struct GROUP_RELATIONSHIP
        {
            /// <summary>
            /// The maximum number of processor groups on the system.
            /// </summary>
            public ushort MaximumGroupCount;
            /// <summary>
            /// The number of active groups on the system. This member indicates the number of <see cref="PROCESSOR_GROUP_INFO"/> structures in the GroupInfo array.
            /// </summary>
            public ushort ActiveGroupCount;
            /// <summary>
            /// This member is reserved.
            /// </summary>
            public fixed byte Reserved[20];
            /// <summary>
            /// An array of <see cref="PROCESSOR_GROUP_INFO"/> structures. Each structure represents the number and affinity of processors in an active group on the system.
            /// </summary>
            public PROCESSOR_GROUP_INFO[] GroupInfo;
        }

        [StructLayout(LayoutKind.Explicit)]
        public struct SYSTEM_LOGICAL_PROCESSOR_INFORMATION_EX
        {
            [FieldOffset(0)] public LOGICAL_PROCESSOR_RELATIONSHIP Relationship;
            [FieldOffset(4)] public uint Size;
            [FieldOffset(8)] public PROCESSOR_RELATIONSHIP Processor;
            [FieldOffset(8)] public NUMA_NODE_RELATIONSHIP NumaNode;
            [FieldOffset(8)] public CACHE_RELATIONSHIP Cache;
            [FieldOffset(8)] public GROUP_RELATIONSHIP Group;
        }
    }
}
