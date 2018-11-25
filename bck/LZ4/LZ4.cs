using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DotNetCross.Memory;
using Minotaur.Core;
using Minotaur.Core.Platform;

namespace Minotaur.Codecs.LZ4
{
    public static unsafe partial class Lz4
    {
        #region Constants

        /*-************************************
        *  Tuning parameter
        **************************************/
        /*!
         * LZ4_MEMORY_USAGE :
         * Memory usage formula : N->2^N Bytes (examples : 10 -> 1KB; 12 -> 4KB ; 16 -> 64KB; 20 -> 1MB; etc.)
         * Increasing memory usage improves compression ratio
         * Reduced memory usage may improve speed, thanks to cache effect
         * Default value is 14, for 16KB, which nicely fits into Intel x86 L1 cache
         */
        private const int LZ4_MEMORY_USAGE = 14;

        private const int ACCELERATION_DEFAULT = 1;

        /*-************************************
         *  Private definitions
         **************************************
         * Do not use these definitions.
         * They are exposed to allow static allocation of `LZ4_stream_t` and `LZ4_streamDecode_t`.
         * Using these definitions will expose code to API and/or ABI break in future versions of the library.
         **************************************/
        private const int LZ4_HASHLOG = LZ4_MEMORY_USAGE - 2;
        private const int LZ4_HASH_SIZE_U32 = 1 << LZ4_HASHLOG;

        private const int LZ4_MAX_INPUT_SIZE = 0x7E000000;   /* 2 113 929 216 bytes */

        public struct LZ4_stream_t
        {
            public fixed uint hashTable[LZ4_HASH_SIZE_U32];
            public uint currentOffset;
            public ushort initCheck;
            public ushort tableType;
            public byte* dictionary;
            public LZ4_stream_t* dictCtx; 
            public uint dictSize;
        }

        /*-************************************
        *  Common Constants
        **************************************/

        private const int MINMATCH = 4;

        private const int WILDCOPYLENGTH = 8;
        private const int LASTLITERALS = 5;
        private const int MFLIMIT = WILDCOPYLENGTH + MINMATCH;
        private const int LZ4_minLength = MFLIMIT + 1;

        private const int _64KB = 64 * Mem.KB;
        private const int _4KB = 4 * Mem.KB;
        private const ulong _2GB = 2ul * Mem.GB;

        private const int MAXD_LOG = 16;
        private const int MAX_DISTANCE = (1 << MAXD_LOG) - 1;

        private const byte ML_BITS = 4;
        private const byte ML_MASK = (1 << ML_BITS) - 1;
        private const byte RUN_BITS = 8 - ML_BITS;
        private const byte RUN_MASK = (1 << RUN_BITS) - 1;

        private const int LZ4_64Klimit = _64KB + (MFLIMIT-1);
        private const int LZ4_skipTrigger = 6;  /* Increase this value ==> compression run slower on incompressible data */

        private static readonly uint[] debruijnBytePos64 = {
            0, 0, 0, 0, 0, 1, 1, 2,
            0, 3, 1, 3, 1, 4, 2, 7,
            0, 2, 3, 6, 1, 5, 3, 5,
            1, 3, 4, 4, 2, 5, 6, 7,
            7, 0, 1, 2, 3, 3, 4, 6,
            2, 6, 5, 5, 3, 4, 5, 6,
            7, 1, 2, 4, 6, 4, 4, 5,
            7, 2, 6, 5, 7, 6, 7, 7 };
        private static readonly uint[] debruijnBytePos32 = {
            0, 0, 3, 0, 3, 1, 3, 0,
            3, 2, 2, 1, 3, 2, 0, 1,
            3, 3, 1, 2, 2, 2, 2, 0,
            3, 1, 2, 0, 1, 0, 1, 1 };

        private static readonly uint[] inc32table = {0, 1, 2, 1, 0, 4, 4, 4};
        private static readonly int[] dec64table = { 0, 0, 0, -1, -4, 1, 2, 3 };

        #region Propagations

        private interface ITableType { }
        private struct ClearedTable : ITableType { }
        private struct ByPtr : ITableType { }
        private struct ByU32 : ITableType { }
        private struct ByU16 : ITableType { }

        private interface ILimitedOutput { }
        private struct NotLimited : ILimitedOutput { }
        private struct LimitedOutput : ILimitedOutput { }
        private struct FillOutput : ILimitedOutput { }

        /**
         * This enum distinguishes several different modes of accessing previous
         * content in the stream.
         *
         * - noDict        : There is no preceding content.
         * - withPrefix64k : Table entries up to ctx->dictSize before the current blob
         *                   blob being compressed are valid and refer to the preceding
         *                   content (of length ctx->dictSize), which is available
         *                   contiguously preceding in memory the content currently
         *                   being compressed.
         * - usingExtDict  : Like withPrefix64k, but the preceding content is somewhere
         *                   else in memory, starting at ctx->dictionary with length
         *                   ctx->dictSize.
         * - usingDictCtx  : Like usingExtDict, but everything concerning the preceding
         *                   content is in a separate context, pointed to by
         *                   ctx->dictCtx. ctx->dictionary, ctx->dictSize, and table
         *                   entries in the current context that refer to positions
         *                   preceding the beginning of the current compression are
         *                   ignored. Instead, ctx->dictCtx->dictionary and ctx->dictCtx
         *                   ->dictSize describe the location and size of the preceding
         *                   content, and matches are found by looking in the ctx
         *                   ->dictCtx->hashTable.
         */
        private interface IDict { }
        private struct NoDict : IDict { }
        private struct WithPrefix64K : IDict { }
        private struct UsingExtDict : IDict { }
        private struct UsingDictCtx : IDict { }

        private interface IDictIssue { }
        private struct NoDictIssue : IDictIssue { }
        private struct DictSmall : IDictIssue { }

        private interface IEndCondition { }
        private struct EndOnOutputSize : IEndCondition { }
        private struct EndOnInputSize : IEndCondition { }

        private interface IEarlyEnd { }
        private struct Full : IEarlyEnd { }
        private struct Partial : IEarlyEnd { }

        private interface IEndian { }
        private struct LittleEndian : IEndian { }
        private struct BigEndian : IEndian { }

        private interface IArch
        {
            bool Not();
            uint NbBytes<TEndian>() where TEndian : IEndian;
        }
        [StructLayout(LayoutKind.Explicit, Size = 4)]
        private struct X32 : IArch
        {
            [FieldOffset(0)]
            // ReSharper disable once FieldCanBeMadeReadOnly.Local
            private uint _value;

            public X32(uint value)
            {
                _value = value;
            }

            #region Implementation of IArch

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Not()
            {
                return _value != 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public uint NbBytes<TEndian>()
                where TEndian : IEndian
            {
                if (typeof(TEndian) == typeof(LittleEndian))
                    return debruijnBytePos32[(uint)((int)_value & -(int)_value) * 0x077CB531u >> 27];

                if (typeof(TEndian) == typeof(BigEndian))
                {
                    return _value >> 16 == 0
                        ? _value >> 8 == 0 ? 3u : 2u
                        : _value >> 24 == 0 ? 1u : 0u;
                }

                throw new NotSupportedException(typeof(TEndian).ToString());
            }

            #endregion

            public static implicit operator uint(X32 x)
            {
                return x._value;
            }

            public static implicit operator int(X32 x)
            {
                return (int)x._value;
            }

            public static explicit operator X32(uint x)
            {
                return new X32(x);
            }

            public static explicit operator X32(int x)
            {
                return new X32((uint)x);
            }

            public static X32 operator ^(X32 x, X32 y)
            {
                return new X32(x._value ^ y._value);
            }
        }

        [StructLayout(LayoutKind.Explicit, Size = 8)]
        private struct X64 : IArch
        {
            [FieldOffset(0)]
            // ReSharper disable once FieldCanBeMadeReadOnly.Local
            private ulong _value;

            public X64(ulong value)
            {
                _value = value;
            }

            #region Implementation of IArch

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public bool Not()
            {
                return _value != 0;
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public uint NbBytes<TEndian>()
                where TEndian : IEndian
            {
                if (typeof(TEndian) == typeof(LittleEndian))
                    return debruijnBytePos64[(ulong)((long)_value & -(long)_value) * 0x0218A392CDABBD3FL >> 58];

                if (typeof(TEndian) == typeof(BigEndian))
                {
                    return _value >> 32 == 0
                        ? _value >> 16 == 0 ? _value >> 8 == 0 ? 7u : 6u : 4u
                        : _value >> 48 == 0 ? _value >> 64 == 0 ? 3u : 2u : 0u;
                }

                throw new NotSupportedException(typeof(TEndian).ToString());
            }

            #endregion

            public static implicit operator ulong(X64 x)
            {
                return x._value;
            }

            public static implicit operator long(X64 x)
            {
                return (int)x._value;
            }

            public static explicit operator X64(ulong x)
            {
                return new X64(x);
            }

            public static explicit operator X64(long x)
            {
                return new X64((uint)x);
            }

            public static X64 operator ^(X64 x, X64 y)
            {
                return new X64(x._value ^ y._value);
            }
        }

        #endregion

        #region Environment

        private static readonly bool is64Bits = IntPtr.Size == sizeof(ulong);
        private static readonly bool isLittleEndian = GetIsLittleEndian();
        private static bool GetIsLittleEndian() 
        {
            var v = new EndianChecker(){ i = 1 };
            return v.b[0] != 0;
        }

        [StructLayout(LayoutKind.Explicit, Size = 4)]
        private struct EndianChecker
        {
            [FieldOffset(0)]
            public uint i;
            [FieldOffset(0)]
            public fixed byte b[4];
        }

        #endregion

        #endregion

        /// <summary>Gets maximum the length of the output.</summary>
        /// <param name="size">Length of the input.</param>
        /// <returns>Maximum number of bytes needed for compressed buffer.</returns>
        public static long MaximumOutputLength(long size)
        {
            return size + (size / 255) + 16;
        }

        public static int Encode(ref byte* input, int inputLength, ref byte* output, int outputLength)
        {
            LZ4_stream_t stream;
            int inputConsumed = 0;
            int wrote;
            if (outputLength >= MaximumOutputLength(inputLength))
            {
                if (is64Bits)
                {
                    wrote = isLittleEndian
                        ? LZ4_compress_generic<LimitedOutput, ByU32, NoDict, NoDictIssue, LittleEndian, X64>(&stream, input, output, inputLength, ref inputConsumed, outputLength, 1)
                        : LZ4_compress_generic<LimitedOutput, ByU32, NoDict, NoDictIssue, BigEndian, X64>(&stream, input, output, inputLength, ref inputConsumed, outputLength, 1);
                }

                wrote = isLittleEndian
                    ? LZ4_compress_generic<LimitedOutput, ByU32, NoDict, NoDictIssue, LittleEndian, X32>(&stream, input, output, inputLength, ref inputConsumed, outputLength, 1)
                    : LZ4_compress_generic<LimitedOutput, ByU32, NoDict, NoDictIssue, BigEndian, X32>(&stream, input, output, inputLength, ref inputConsumed, outputLength, 1);
            }
            else
            {
                if (is64Bits)
                {
                    wrote = isLittleEndian
                        ? LZ4_compress_generic<FillOutput, ByU32, NoDict, NoDictIssue, LittleEndian, X64>(&stream, input, output, inputLength, ref inputConsumed, outputLength, 1)
                        : LZ4_compress_generic<FillOutput, ByU32, NoDict, NoDictIssue, BigEndian, X64>(&stream, input, output, inputLength, ref inputConsumed, outputLength, 1);
                }

                wrote = isLittleEndian
                    ? LZ4_compress_generic<FillOutput, ByU32, NoDict, NoDictIssue, LittleEndian, X32>(&stream, input, output, inputLength, ref inputConsumed, outputLength, 1)
                    : LZ4_compress_generic<FillOutput, ByU32, NoDict, NoDictIssue, BigEndian, X32>(&stream, input, output, inputLength, ref inputConsumed, outputLength, 1);
            }

            input += inputConsumed;
            output += wrote;
            return wrote;
        }

        public static int Decode(ref byte* input, int inputLength, ref byte* output, int remainOutputLength, int targetOutput)
        {
            return isLittleEndian 
                ? LZ4_decompress_generic<EndOnInputSize, Partial, NoDict, LittleEndian>(ref input, ref output, inputLength, remainOutputLength, targetOutput, output - _64KB, null, _64KB)
                : LZ4_decompress_generic<EndOnInputSize, Partial, NoDict, BigEndian>(ref input, ref output, inputLength, remainOutputLength, targetOutput, output - _64KB, null, _64KB);
        }

        private static int LZ4_compress_fast_continue(LZ4_stream_t* stream, byte* source, byte* dest, int inputSize, int maxOutputSize, int acceleration = 1)
        {
            if (is64Bits)
            {
                return isLittleEndian
                    ? LZ4_compress_fast_continue<LittleEndian, X64>(stream, source, dest, inputSize, maxOutputSize, acceleration)
                    : LZ4_compress_fast_continue<BigEndian, X64>(stream, source, dest, inputSize, maxOutputSize, acceleration);
            }

            return isLittleEndian
                ? LZ4_compress_fast_continue<LittleEndian, X32>(stream, source, dest, inputSize, maxOutputSize, acceleration)
                : LZ4_compress_fast_continue<BigEndian, X32>(stream, source, dest, inputSize, maxOutputSize, acceleration);
        }

        private static int LZ4_compress_fast_continue<TEndian, TArch>(LZ4_stream_t* stream, byte* source, byte* dest, int inputSize, int maxOutputSize, int acceleration = 1)
            where TEndian : struct, IEndian
            where TArch : unmanaged, IArch
        {
            var dictEnd = stream->dictionary + stream->dictSize;

            Debug.WriteLine($"LZ4_compress_fast_continue (inputSize={inputSize})");
            if (stream->initCheck != 0) return 0; /* Uninitialized structure detected */
            LZ4_renormDictT(stream, inputSize);   /* avoid index overflow */
            acceleration = Math.Max(acceleration, 1);

            /* invalidate tiny dictionaries */
            if (stream->dictSize - 1 < 4   /* intentional underflow */
              && dictEnd != source ) {
                Debug.WriteLine($"LZ4_compress_fast_continue: dictSize({stream->dictSize}) at addr:{(ulong)stream->dictionary} is too small");
                stream->dictSize = 0;
                stream->dictionary = source;
                dictEnd = source;
            }

            /* Check overlapping input/dictionary space */
            {
                var sourceEnd = source + inputSize;
                if ((sourceEnd > stream->dictionary) && (sourceEnd < dictEnd))
                {
                    stream->dictSize = (uint)(dictEnd - sourceEnd);
                    if (stream->dictSize > _64KB) stream->dictSize = _64KB;
                    if (stream->dictSize < 4) stream->dictSize = 0;
                    stream->dictionary = dictEnd - stream->dictSize;
                }
            }

            /* prefix mode : source data follows dictionary */
            if (dictEnd == source)
            {
                var _ = 0;
                if (stream->dictSize < _64KB && stream->dictSize < stream->currentOffset)
                    return LZ4_compress_generic<LimitedOutput, ByU32, WithPrefix64K, DictSmall, TEndian, TArch>(stream, source, dest, inputSize, ref _, maxOutputSize, acceleration);
                return LZ4_compress_generic<LimitedOutput, ByU32, WithPrefix64K, NoDictIssue, TEndian, TArch>(stream, source, dest, inputSize, ref _, maxOutputSize, acceleration);
            }

            /* external dictionary mode */
            {
                int result;
                var _ = 0;
                if (stream->dictCtx != null)
                {
                    /* We depend here on the fact that dictCtx'es (produced by
                     * LZ4_loadDict) guarantee that their tables contain no references
                     * to offsets between dictCtx->currentOffset - 64 KB and
                     * dictCtx->currentOffset - dictCtx->dictSize. This makes it safe
                     * to use noDictIssue even when the dict isn't a full 64 KB.
                     */
                    if (inputSize > _4KB) {
                        /* For compressing large blobs, it is faster to pay the setup
                         * cost to copy the dictionary's tables into the active context,
                         * so that the compression loop is only looking into one table.
                         */
                        Unsafe.CopyBlock(stream, stream->dictCtx, (uint)sizeof(LZ4_stream_t));
                        result = LZ4_compress_generic<LimitedOutput, ByU32, UsingExtDict, NoDictIssue, TEndian, TArch>(stream, source, dest, inputSize, ref _, maxOutputSize, acceleration);
                    } else {
                        result = LZ4_compress_generic<LimitedOutput, ByU32, UsingDictCtx, NoDictIssue, TEndian, TArch>(stream, source, dest, inputSize, ref _, maxOutputSize, acceleration);
                    }
                }
                else
                {
                    if ((stream->dictSize < _64KB) && (stream->dictSize < stream->currentOffset)) {
                        result = LZ4_compress_generic<LimitedOutput, ByU32, UsingExtDict, DictSmall, TEndian, TArch>(stream, source, dest, inputSize, ref _, maxOutputSize, acceleration);
                    } else {
                        result = LZ4_compress_generic<LimitedOutput, ByU32, UsingExtDict, NoDictIssue, TEndian, TArch>(stream, source, dest, inputSize, ref _, maxOutputSize, acceleration);
                    }
                }
                stream->dictionary = source;
                stream->dictSize = (uint)inputSize;
                return result;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int LZ4_compressBound(int isize) =>
            isize > LZ4_MAX_INPUT_SIZE ? 0 : isize + isize / 255 + 16;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void LZ4_resetStream(LZ4_stream_t* state) => Mem.Zero((byte*)state, sizeof(LZ4_stream_t));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void LZ4_putPosition<TTableType, TEndian, TArch>(byte* p, void* tableBase, byte* srcBase)
            where TTableType : struct, ITableType
            where TEndian : struct, IEndian
            where TArch : struct, IArch
        =>  LZ4_putPositionOnHash<TTableType>(p, LZ4_hashPosition<TTableType, TEndian, TArch>(p), tableBase, srcBase);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void LZ4_putPositionOnHash<TTableType>(byte* p, uint h, void* tableBase, byte* srcBase)
            where TTableType : struct, ITableType
        {
            if (typeof(TTableType) == typeof(ByPtr))
            {
                ((byte**) tableBase)[h] = p;
                return;
            }

            if (typeof(TTableType) == typeof(ByU32))
            {
                // Todo: Compare perf
                // *((uint*)tableBase + h) = (uint)(p - srcBase);
                ((uint*)tableBase)[h] = (uint)(p - srcBase);
                return;
            }

            if (typeof(TTableType) == typeof(ByU16))
            {
                // Todo: Compare perf
                // *((ushort*)tableBase + h) = (ushort)(p - srcBase);
                ((ushort*)tableBase)[h] = (ushort)(p - srcBase);
                return;
            }

            throw new NotSupportedException($"Illegal table type: {typeof(TTableType).Name}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void LZ4_putIndexOnHash<TTableType>(uint idx, uint h, void* tableBase)
            where TTableType : struct, ITableType
        {
            if (typeof(TTableType) == typeof(ByPtr))
                throw new NotSupportedException($"Illegal table type: {typeof(TTableType).Name}");

            if (typeof(TTableType) == typeof(ByU32))
                *((uint*)tableBase + h) = idx;

            if (typeof(TTableType) == typeof(ByU16))
                *((ushort*)tableBase + h) = (ushort)idx;

            // fallthrough
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint LZ4_hashPosition<TTableType, TEndian, TArch>(byte* p)
            where TTableType : struct, ITableType
            where TEndian : struct, IEndian
            where TArch : struct, IArch
        {
            if(typeof(TArch) == typeof(X64) && typeof(TTableType) != typeof(ByU16))
                return LZ4_hash5<TTableType, TEndian>(LZ4_read_ARCH<TArch>(p));
            return LZ4_hash4<TTableType>(Mem.ReadU32(p));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint LZ4_hash4<TTableType>(uint sequence)
            where TTableType : struct, ITableType
            => (sequence * 2654435761u) >> (MINMATCH * 8 - (typeof(TTableType) == typeof(ByU16) ? LZ4_HASHLOG + 1 : LZ4_HASHLOG));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint LZ4_hash5<TTableType, TEndian>(ulong sequence)
            where TTableType : struct, ITableType
            where TEndian : struct, IEndian
        {
            if (typeof(TEndian) == typeof(LittleEndian))
                return (uint)(((sequence << 24) * 889523592379ul) >> (64 - (typeof(TTableType) == typeof(ByU16) ? LZ4_HASHLOG + 1 : LZ4_HASHLOG)));

            if (typeof(TEndian) == typeof(BigEndian))
                return (uint)(((sequence >> 24) * 11400714785074694791ul) >> (64 - (typeof(TTableType) == typeof(ByU16) ? LZ4_HASHLOG + 1 : LZ4_HASHLOG)));

            throw new NotSupportedException($"Unknown Endian: {typeof(TEndian).Name}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte* LZ4_getPosition<TTableType, TEndian, TArch>(byte* p, void* tableBase, byte* srcBase)
            where TTableType : struct, ITableType
            where TEndian : struct, IEndian
            where TArch : struct, IArch
        => LZ4_getPositionOnHash<TTableType>(LZ4_hashPosition<TTableType, TEndian, TArch>(p), tableBase, srcBase);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static byte* LZ4_getPositionOnHash<TTableType>(uint h, void* tableBase, byte* srcBase)
            where TTableType : struct, ITableType
        {
            if (typeof(TTableType) == typeof(ByPtr))
                return ((byte**) tableBase)[h];
            if (typeof(TTableType) == typeof(ByU32))
                return ((uint*) tableBase)[h] + srcBase;
            
            return ((ushort*)tableBase)[h] + srcBase;
        }

        /* LZ4_getIndexOnHash() :
         * Index of match position registered in hash table.
         * hash position must be calculated by using base+index, or dictBase+index.
         * Assumption 1 : only valid if tableType == byU32 or byU16.
         * Assumption 2 : h is presumed valid (within limits of hash table)
         */
        private static uint LZ4_getIndexOnHash<TTableType>(uint h, void* tableBase)
        {
            //LZ4_STATIC_ASSERT(LZ4_MEMORY_USAGE > 2);
            if (typeof(TTableType) == typeof(ByU32))
            {
                assert(h<(1U << (LZ4_MEMORY_USAGE-2)));
                return ((uint*)tableBase)[h];
            }
            if (typeof(TTableType) == typeof(ByU16)) {
                assert(h<(1U << (LZ4_MEMORY_USAGE-1)));
                return ((ushort*)tableBase)[h];
            }
            assert(false); return 0;  /* forbidden case */
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint LZ4_count<TEndian, TArch>(byte* pIn, byte* pMatch, byte* pInLimit)
            where TEndian : struct, IEndian
            where TArch : unmanaged, IArch
        {
            var pStart = pIn;
            
            while (pIn < pInLimit - (sizeof(TArch) - 1))
            {
                var diff = LZ4_xor<TArch>(pMatch, pIn);
                if (diff.Not())
                    return (uint)(pIn + diff.NbBytes<TEndian>() - pStart);

                pIn += sizeof(TArch);
                pMatch += sizeof(TArch);
            }

            if (typeof(TArch) == typeof(X64) && pIn < pInLimit - 3 && Mem.ReadU32(pMatch) == Mem.ReadU32(pIn))
            {
                pIn += 4;
                pMatch += 4;
            }

            if (pIn < pInLimit - 1 && Mem.ReadU16(pMatch) == Mem.ReadU16(pIn))
            {
                pIn += 2;
                pMatch += 2;
            }

            if (pIn < pInLimit && *pMatch == *pIn) pIn++;
            return (uint) (pIn - pStart);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong LZ4_read_ARCH<TArch>(void* p)
            where TArch : struct, IArch
        {
            if (typeof(TArch) == typeof(X32))
                return Mem.ReadU32(p);

            if (typeof(TArch) == typeof(X64))
                return Mem.ReadU64(p);
        
            throw new NotSupportedException($"Unknown arch: {typeof(TArch).Name}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ushort LZ4_readLE16<TEndian>(void* memPtr)
            where TEndian : struct, IEndian
        {
            if (typeof(TEndian) == typeof(LittleEndian))
                return Mem.ReadU16(memPtr);

            if (typeof(TEndian) == typeof(BigEndian))
            {
                ushort result = *(byte*) memPtr;
                return (ushort)(result + (*((byte*)memPtr + 1) << 8));
            }

            throw new NotSupportedException($"Unknown Endian: {typeof(TEndian).Name}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void LZ4_writeLE16<TEndian>(void* memPtr, ushort value)
            where TEndian : struct , IEndian
        {
            if (typeof(TEndian) == typeof(LittleEndian))
            {
                Mem.Write(memPtr, value);
                return;
            }

            if (typeof(TEndian) == typeof(BigEndian))
            {
                *(byte*) memPtr = (byte) value;
                *((byte*) memPtr + 1) = (byte) (value >> 8);
                return;
            }

            throw new NotSupportedException($"Unknown Endian: {typeof(TEndian).Name}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ushort ToU16<TTableType>()
            where TTableType : struct, ITableType
        {
            if (typeof(TTableType) == typeof(ClearedTable))
                return 0;

            if (typeof(TTableType) == typeof(ByPtr))
                return 1;

            if (typeof(TTableType) == typeof(ByU32))
                return 2;

            if (typeof(TTableType) == typeof(ByU16))
                return 3;

            throw new NotSupportedException($"Unknown table type: {typeof(TTableType).Name}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static TArch LZ4_xor<TArch>(byte* x, byte* y)
            where TArch : struct, IArch
        {
            if (typeof(TArch) == typeof(X32))
                return (TArch)(object)new X32(*(uint*)x ^ *(uint*)y);
            if (typeof(TArch) == typeof(X64))
                return (TArch)(object)new X64(*(ulong*)x ^ *(ulong*)y);

            throw new NotSupportedException(typeof(TArch).ToString());
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void LZ4_renormDictT(LZ4_stream_t* stream, int nextSize)
        {
            if (stream->currentOffset + nextSize > 0x80000000)
            {   /* potential ptrdiff_t overflow (32-bits mode) */
                /* rescale hash table */
                var delta = stream->currentOffset - _64KB;
                var dictEnd = stream->dictionary + stream->dictSize;
                int i;
                Debug.WriteLine("LZ4_renormDictT");
                for (i = 0; i < LZ4_HASH_SIZE_U32; i++)
                {
                    if (stream->hashTable[i] < delta) stream->hashTable[i] = 0;
                    else stream->hashTable[i] -= delta;
                }
                stream->currentOffset = _64KB;
                if (stream->dictSize > _64KB) stream->dictSize = _64KB;
                stream->dictionary = dictEnd - stream->dictSize;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static void assert(bool assertion)
        {
            if(!assertion) throw new Exception();
        }

        
    }
}
