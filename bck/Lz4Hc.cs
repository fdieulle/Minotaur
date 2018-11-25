//using System;
//using System.Diagnostics;
//using System.Net;
//using System.Runtime.CompilerServices;
//using System.Runtime.InteropServices;
//using DotNetCross.Memory;

//namespace Minotaur.Codecs
//{
//    public unsafe class Lz4Hc
//    {
//        /* Defines constants */
//        private const int MINMATCH = 4;

//        private const int WILDCOPYLENGTH = 8;
//        private const int LASTLITERALS = 5;
//        private const int MFLIMIT = WILDCOPYLENGTH + LASTLITERALS;
//        private const int LZ4_MIN_LENGTH = MFLIMIT + 1;

//        private const int ML_BITS = 4;
//        private const int ML_MASK = (1 << ML_BITS) - 1;
//        private const int RUN_BITS = 8 - ML_BITS;
//        private const int RUN_MASK = (1 << RUN_BITS) - 1;

//        private const int OPTIMAL_ML = (ML_MASK - 1) + MINMATCH;

//        /*-************************************
//        *  Tuning parameter
//        **************************************/
//        /*!
//         * LZ4_MEMORY_USAGE :
//         * Memory usage formula : N->2^N Bytes (examples : 10 -> 1KB; 12 -> 4KB ; 16 -> 64KB; 20 -> 1MB; etc.)
//         * Increasing memory usage improves compression ratio
//         * Reduced memory usage may improve speed, thanks to cache effect
//         * Default value is 14, for 16KB, which nicely fits into Intel x86 L1 cache
//         */
//        private const int LZ4_MEMORY_USAGE = 14;
//        private const int LZ4_STREAMSIZE_U64 = (1 << (LZ4_MEMORY_USAGE - 3)) + 4;

//        private const int LZ4_HASHLOG = LZ4_MEMORY_USAGE - 2;
//        private const int LZ4_HASH_SIZE_U32 = 1 << LZ4_HASHLOG;

//        private const int LZ4_MAX_INPUT_SIZE = 0x7E000000;
//        private const int LZ4_64Klimit = 64 * Bits.KILO_BYTE + (MFLIMIT-1);

//        private const int LZ4HC_DICTIONARY_LOGSIZE = 16;
//        private const int LZ4HC_MAXD = 1 << LZ4HC_DICTIONARY_LOGSIZE;
//        private const int LZ4HC_MAXD_MASK = LZ4HC_MAXD - 1;

//        private const int LZ4HC_HASH_LOG = 15;
//        private const int LZ4HC_HASHTABLESIZE = 1 << LZ4HC_HASH_LOG;
//        private const int LZ4HC_HASH_MASK = LZ4HC_HASHTABLESIZE - 1;

//        private const int MAXD_LOG = 16;
//        private const int MAX_DISTANCE = (1 << MAXD_LOG) - 1;

//        private static readonly bool IsArch64 = IntPtr.Size == 8;
//        private static readonly int STEPSIZE = IsArch64 ? sizeof(ulong) : sizeof(uint);
//        private static readonly bool IsLittleEndian = LZ4_IsLittleEndian();

//        /* Defines directive templates */
//        private interface ILimitedOutput { }
//        private struct NoLimit : ILimitedOutput { }
//        private struct LimitedOutput : ILimitedOutput { }
//        private struct LimitedDestSize : ILimitedOutput { } /* Same as FillOutput */
        
//        private interface IDictCtx { }
//        private struct NoDictCtx : IDictCtx { }
//        private struct WithPrefix64K : IDictCtx { }
//        private struct UsingExtDict : IDictCtx { }
//        private struct UsingDictCtx : IDictCtx { }

//        private interface IHCFavor { }
//        private struct FavorCompressionRatio : IHCFavor { }
//        private struct FavorDecompressionRatio : IHCFavor { }

//        private enum RepeatState
//        {
//            Untested,
//            Not,
//            Confirmed
//        }

//        private interface IArch
//        {
//            bool Not();
//            int NbBytes<TEndian>() where TEndian : IEndian;
//        }

//        [StructLayout(LayoutKind.Explicit, Size = 4)]
//        private struct X32 : IArch
//        {
//            [FieldOffset(0)]
//            // ReSharper disable once FieldCanBeMadeReadOnly.Local
//            private int _value;

//            #region Implementation of IArch

//            [MethodImpl(MethodImplOptions.AggressiveInlining)]
//            public bool Not()
//            {
//                return _value != 0;
//            }

//            [MethodImpl(MethodImplOptions.AggressiveInlining)]
//            public int NbBytes<TEndian>()
//                where TEndian : IEndian
//            {
//                if (typeof(TEndian) == typeof(LittleEndian))
//                    return DebruijnBytePos32[(uint) (_value & -_value) * 0x077CB531u >> 27];

//                if (typeof(TEndian) == typeof(BigEndian))
//                {
//                    return _value >> 16 == 0
//                        ? _value >> 8 == 0 ? 3 : 2
//                        : _value >> 24 == 0 ? 1 : 0;
//                }

//                throw new NotSupportedException(typeof(TEndian).ToString());
//            }

//            #endregion
//        }

//        [StructLayout(LayoutKind.Explicit, Size = 8)]
//        private struct X64 : IArch
//        {
//            [FieldOffset(0)]
//            // ReSharper disable once FieldCanBeMadeReadOnly.Local
//            private long _value;

//            #region Implementation of IArch

//            [MethodImpl(MethodImplOptions.AggressiveInlining)]
//            public bool Not()
//            {
//                return _value != 0;
//            }

//            [MethodImpl(MethodImplOptions.AggressiveInlining)]
//            public int NbBytes<TEndian>()
//                where TEndian : IEndian
//            {
//                if (typeof(TEndian) == typeof(LittleEndian))
//                    return DebruijnBytePos64[(ulong)(_value & -_value) * 0x0218A392CDABBD3FL >> 58];

//                if (typeof(TEndian) == typeof(BigEndian))
//                {
//                    return _value >> 32 == 0
//                        ? _value >> 16 == 0 ? _value >> 8 == 0 ? 7 : 6 : 4
//                        : _value >> 48 == 0 ? _value >> 64 == 0 ? 3 : 2 : 0;
//                }

//                throw new NotSupportedException(typeof(TEndian).ToString());
//            }

//            #endregion
//        }

//        private interface IEndian { }
//        private struct LittleEndian : IEndian { }
//        private struct BigEndian : IEndian { }

//        private interface ITableType { ushort Id { get; } }
//        private struct ClearTable : ITableType { public ushort Id => 0; }
//        private struct ByPtr : ITableType { public ushort Id => 1; }
//        private struct ByU32 : ITableType { public ushort Id => 2; }
//        private struct ByU16 : ITableType { public ushort Id => 3; }

//        private struct LZ4_stream_t
//        {
//            public fixed ulong table[LZ4_STREAMSIZE_U64];

//        }

//        private struct LZ4_stream_t_internal
//        {
//            public fixed uint hashTable[LZ4_HASH_SIZE_U32];
//            public uint currentOffset;
//            public ushort initCheck;
//            public ushort tableType;
//            public byte* dictionary;
//            public LZ4_stream_t_internal* dictCtx;
//            public uint dictSize;
//        }

//        private struct LZ4HC_CCtx
//        {
//            public fixed uint hashTable[LZ4HC_HASHTABLESIZE];
//            public fixed ushort chainTable[LZ4HC_MAXD];
//            public byte* end;
//            public byte* @base;
//            public byte* dictBase;
//            public uint dictLimit;
//            public uint lowLimit;
//            public uint nextToUpdate;
//            public short compressionLevel;
//            public short favorDecSpeed;
//            public LZ4HC_CCtx* dictCtx;
//        }

//        private static readonly int[] DebruijnBytePos64 = new []{
//            0, 0, 0, 0, 0, 1, 1, 2,
//            0, 3, 1, 3, 1, 4, 2, 7,
//            0, 2, 3, 6, 1, 5, 3, 5,
//            1, 3, 4, 4, 2, 5, 6, 7,
//            7, 0, 1, 2, 3, 3, 4, 6,
//            2, 6, 5, 5, 3, 4, 5, 6,
//            7, 1, 2, 4, 6, 4, 4, 5,
//            7, 2, 6, 5, 7, 6, 7, 7 };
//        private static readonly int[] DebruijnBytePos32 = new[]{
//            0, 0, 3, 0, 3, 1, 3, 0,
//            3, 2, 2, 1, 3, 2, 0, 1,
//            3, 3, 1, 2, 2, 2, 2, 0,
//            3, 1, 2, 0, 1, 0, 1, 1 };

//        public static int LZ4_compressBound(int iSize)
//        {
//            return iSize > LZ4_MAX_INPUT_SIZE ? 0 : iSize + ((iSize / 255) + 16);
//        }

//        /// <summary>
//        /// Reverse the logic : compresses as much data as possible from 'src' buffer
//        /// into already allocated buffer 'dst' of size 'targetDestSize'.
//        /// This function either compresses the entire 'src' content into 'dst' if it's large enough,
//        /// or fill 'dst' buffer completely with as much data as possible from 'src'.
//        ///     *srcSizePtr : will be modified to indicate how many bytes where read from 'src' to fill 'dst'.
//        ///                   New value is necessarily lower or equals than old value.
//        /// </summary>
//        /// <returns>Returns Nb bytes written into 'dst'. 0 if compression failed.</returns>
//        public static int LZ4_Compress_DestSize(byte* src, byte* dst, ref int srcSize, int targetDstSize)
//        {
//            LZ4_stream_t ctxBody;
//            return LZ4_compress_destSize_extState(&ctxBody, src, dst, ref srcSize, targetDstSize);
//        }

//        /* Note!: This function leaves the stream in an unclean/broken state!
//        * It is not safe to subsequently use the same state with a _fastReset() or
//        * _continue() call without resetting it. */
//        private static int LZ4_compress_destSize_extState(LZ4_stream_t* state, byte* src, byte* dst, ref int srcSize, int targetDstSize)
//        {
//            LZ4_resetStream(state);

//            if (targetDstSize >= LZ4_compressBound(srcSize)) /* compression success is guaranteed */
//                return LZ4_compress_fast_extState(state, src, dst, ref srcSize, targetDstSize, 1);

//            if (srcSize < LZ4_64Klimit)
//            {
//                return LZ4_compress_generic(&state->internal_donotuse, src, dst, ref srcSize, srcSize, targetDstSize, fillOutput, byU16, noDict, noDictIssue, 1);
//            }
//            else
//            {
//                tableType_t const tableType = ((sizeof(void*) == 4) && ((uptrval)src > MAX_DISTANCE)) ? byPtr : byU32;
//                return LZ4_compress_generic(&state->internal_donotuse, src, dst, *srcSizePtr, srcSizePtr, targetDstSize, fillOutput, tableType, noDict, noDictIssue, 1);
//            }
//        }

//        private static int LZ4_compress_generic<TLimitedOutput, TTableType, TDict, TDictIssue, TArch, TEndian>(
//            LZ4_stream_t_internal* cctx,
//            byte* source, byte* dest,
//            int inputSize, ref int inputConsumed,
//            int maxOutputSize, int acceleration = 1)
//        {
//            var ip = source;

//            var startIndex = cctx->currentOffset;
//            var @base = source - startIndex;
//            byte* lowLimit;

//            var dictCtx = cctx->dictCtx;
//            var dictionary = typeof(TDict) == typeof(UsingDictCtx) ? dictCtx->dictionary : cctx->dictionary;
//            var dictSize = typeof(TDict) == typeof(UsingDictCtx) ? dictCtx->dictSize : cctx->dictSize;
//            var dictDelta = typeof(TDict) == typeof(UsingDictCtx) ? startIndex - dictCtx->currentOffset : 0;   /* make indexes in dictCtx comparable with index in current context */

//            int const maybe_extMem = (dictDirective == usingExtDict) || (dictDirective == usingDictCtx);
//            var prefixIdxLimit = startIndex - dictSize;   /* used when dictDirective == dictSmall */
//            var dictEnd = dictionary + dictSize;
//            var anchor = source;
//            var iend = ip + inputSize;
//            var mflimitPlusOne = iend - MFLIMIT + 1;
//            var matchlimit = iend - LASTLITERALS;

//            /* the dictCtx currentOffset is indexed on the start of the dictionary,
//             * while a dictionary in the current context precedes the currentOffset */
//            var dictBase = typeof(TDict) == typeof(UsingDictCtx) ?
//                dictionary + dictSize - dictCtx->currentOffset :
//                dictionary + dictSize - startIndex;

//            var op = dest;
//            var olimit = op + maxOutputSize;

//            var offset = 0u;
//            uint forwardH;

//            // Todo: DEBUGLOG(5, "LZ4_compress_generic: srcSize=%i, tableType=%u", inputSize, tableType);
//            /* Init conditions */
//            if (typeof(TLimitedOutput) == typeof(LimitedDestSize) && maxOutputSize < 1) return 0; /* Impossible to store anything */
//            if (inputSize > LZ4_MAX_INPUT_SIZE) return 0;   /* Unsupported inputSize, too large (or negative) */
//            if (typeof(ITableType) == typeof(ByU16) && inputSize >= LZ4_64Klimit) return 0;  /* Size too large (not within 64K limit) */
//            if (typeof(ITableType) == typeof(ByPtr)) Debug.Assert(typeof(TDict) == typeof(NoDictCtx));      /* only supported use case with byPtr */
//            Debug.Assert(acceleration >= 1);

//            lowLimit = source - (typeof(TDict) == typeof(WithPrefix64K) ? dictSize : 0);

//            /* Update context state */
//            if (typeof(TDict) == typeof(UsingDictCtx))
//            {
//                /* Subsequent linked blocks can't use the dictionary. */
//                /* Instead, they use the block we just compressed. */
//                cctx->dictCtx = null;
//                cctx->dictSize = (uint)inputSize;
//            }
//            else
//            {
//                cctx->dictSize += (uint)inputSize;
//            }
//            cctx->currentOffset += (uint)inputSize;
//            cctx->tableType = (ushort)tableType;

//            if (inputSize < LZ4_minLength) goto _last_literals;        /* Input too small, no compression (all literals) */

//            /* First Byte */
//            LZ4_putPosition(ip, cctx->hashTable, tableType, base);
//            ip++; forwardH = LZ4_hashPosition(ip, tableType);

//            /* Main Loop */
//            for (; ; )
//            {
//                const BYTE* match;
//                BYTE* token;

//                /* Find a match */
//                if (tableType == byPtr)
//                {
//                    const BYTE* forwardIp = ip;
//                    unsigned step = 1;
//                    unsigned searchMatchNb = acceleration << LZ4_skipTrigger;
//                    do
//                    {
//                        U32 const h = forwardH;
//                        ip = forwardIp;
//                        forwardIp += step;
//                        step = (searchMatchNb++ >> LZ4_skipTrigger);

//                        if (unlikely(forwardIp > mflimitPlusOne)) goto _last_literals;
//                        assert(ip < mflimitPlusOne);

//                        match = LZ4_getPositionOnHash(h, cctx->hashTable, tableType, base);
//                        forwardH = LZ4_hashPosition(forwardIp, tableType);
//                        LZ4_putPositionOnHash(ip, h, cctx->hashTable, tableType, base);

//                    } while ((match + MAX_DISTANCE < ip)
//                           || (LZ4_read32(match) != LZ4_read32(ip)));

//                }
//                else
//                {   /* byU32, byU16 */

//                    const BYTE* forwardIp = ip;
//                    unsigned step = 1;
//                    unsigned searchMatchNb = acceleration << LZ4_skipTrigger;
//                    do
//                    {
//                        U32 const h = forwardH;
//                        U32 const current = (U32)(forwardIp - base);
//                        U32 matchIndex = LZ4_getIndexOnHash(h, cctx->hashTable, tableType);
//                        assert(matchIndex <= current);
//                        assert(forwardIp - base < (ptrdiff_t)(2 GB - 1));
//                    ip = forwardIp;
//                    forwardIp += step;
//                    step = (searchMatchNb++ >> LZ4_skipTrigger);

//                    if (unlikely(forwardIp > mflimitPlusOne)) goto _last_literals;
//                    assert(ip < mflimitPlusOne);

//                    if (dictDirective == usingDictCtx)
//                    {
//                        if (matchIndex < startIndex)
//                        {
//                            /* there was no match, try the dictionary */
//                            assert(tableType == byU32);
//                            matchIndex = LZ4_getIndexOnHash(h, dictCtx->hashTable, byU32);
//                            match = dictBase + matchIndex;
//                            matchIndex += dictDelta;   /* make dictCtx index comparable with current context */
//                            lowLimit = dictionary;
//                        }
//                        else
//                        {
//                            match = base + matchIndex;
//                            lowLimit = (const BYTE*)source;
//        }
//    } else if (dictDirective==usingExtDict) {
//                    if (matchIndex<startIndex) {
//                        DEBUGLOG(7, "extDict candidate: matchIndex=%5u  <  startIndex=%5u", matchIndex, startIndex);
//    assert(startIndex - matchIndex >= MINMATCH);
//    match = dictBase + matchIndex;
//                        lowLimit = dictionary;
//                    } else {
//                        match = base + matchIndex;
//                        lowLimit = (const BYTE*)source;
//                    }
//                } else {   /* single continuous memory segment */
//                    match = base + matchIndex;
//                }
//                forwardH = LZ4_hashPosition(forwardIp, tableType);
//LZ4_putIndexOnHash(current, h, cctx->hashTable, tableType);

//                if ((dictIssue == dictSmall) && (matchIndex<prefixIdxLimit)) continue;    /* match outside of valid area */
//                assert(matchIndex<current);
//                if ((tableType != byU16) && (matchIndex+MAX_DISTANCE<current)) continue;  /* too far */
//                if (tableType == byU16) assert((current - matchIndex) <= MAX_DISTANCE);     /* too_far presumed impossible with byU16 */

//                if (LZ4_read32(match) == LZ4_read32(ip)) {
//                    if (maybe_extMem) offset = current - matchIndex;
//                    break;   /* match found */
//                }

//            } while(1);
//        }

//        /* Catch up */
//        while (((ip>anchor) & (match > lowLimit)) && (unlikely(ip[-1]==match[-1]))) { ip--; match--; }

//        /* Encode Literals */
//        {   unsigned const litLength = (unsigned)(ip - anchor);
//token = op++;
//            if ((outputLimited == limitedOutput) &&  /* Check output buffer overflow */
//                (unlikely(op + litLength + (2 + 1 + LASTLITERALS) + (litLength/255) > olimit)))
//                return 0;
//            if ((outputLimited == fillOutput) &&
//                (unlikely(op + (litLength+240)/255 /* litlen */ + litLength /* literals */ + 2 /* offset */ + 1 /* token */ + MFLIMIT - MINMATCH /* min last literals so last match is <= end - MFLIMIT */ > olimit))) {
//                op--;
//                goto _last_literals;
//            }
//            if (litLength >= RUN_MASK) {
//                int len = (int)litLength - RUN_MASK;
//                * token = (RUN_MASK << ML_BITS);
//                for(; len >= 255 ; len-=255) * op++ = 255;
//                * op++ = (BYTE) len;
//            }
//            else * token = (BYTE)(litLength << ML_BITS);

///* Copy Literals */
//LZ4_wildCopy(op, anchor, op+litLength);
//op+=litLength;
//            DEBUGLOG(6, "seq.start:%i, literals=%u, match.start:%i",
//                        (int)(anchor-(const BYTE*)source), litLength, (int) (ip-(const BYTE*)source));
//        }

//_next_match:
//        /* at this stage, the following variables must be correctly set :
//         * - ip : at start of LZ operation
//         * - match : at start of previous pattern occurence; can be within current prefix, or within extDict
//         * - offset : if maybe_ext_memSegment==1 (constant)
//         * - lowLimit : must be == dictionary to mean "match is within extDict"; must be == source otherwise
//         * - token and *token : position to write 4-bits for match length; higher 4-bits for literal length supposed already written
//         */

//        if ((outputLimited == fillOutput) &&
//            (op + 2 /* offset */ + 1 /* token */ + MFLIMIT - MINMATCH /* min last literals so last match is <= end - MFLIMIT */ > olimit)) {
//            /* the match was too close to the end, rewind and go to last literals */
//            op = token;
//            goto _last_literals;
//        }

//        /* Encode Offset */
//        if (maybe_extMem) {   /* static test */
//            DEBUGLOG(6, "             with offset=%u  (ext if > %i)", offset, (int)(ip - (const BYTE*)source));
//            assert(offset <= MAX_DISTANCE && offset > 0);
//LZ4_writeLE16(op, (U16) offset); op+=2;
//        } else  {
//            DEBUGLOG(6, "             with offset=%u  (same segment)", (U32)(ip - match));
//            assert(ip-match <= MAX_DISTANCE);
//LZ4_writeLE16(op, (U16)(ip - match)); op+=2;
//        }

//        /* Encode MatchLength */
//        {   unsigned matchCode;

//            if ((dictDirective==usingExtDict || dictDirective==usingDictCtx)
//              && (lowLimit==dictionary) /* match within extDict */ ) {
//                const BYTE* limit = ip + (dictEnd - match);
//assert(dictEnd > match);
//                if (limit > matchlimit) limit = matchlimit;
//                matchCode = LZ4_count(ip+MINMATCH, match+MINMATCH, limit);
//ip += MINMATCH + matchCode;
//                if (ip==limit) {
//                    unsigned const more = LZ4_count(limit, (const BYTE *)source, matchlimit);
//                    matchCode += more;
//                    ip += more;
//                }
//                DEBUGLOG(6, "             with matchLength=%u starting in extDict", matchCode+MINMATCH);
//            } else {
//                matchCode = LZ4_count(ip+MINMATCH, match+MINMATCH, matchlimit);
//ip += MINMATCH + matchCode;
//                DEBUGLOG(6, "             with matchLength=%u", matchCode+MINMATCH);
//            }

//            if ((outputLimited) &&    /* Check output buffer overflow */
//                (unlikely(op + (1 + LASTLITERALS) + (matchCode>>8) > olimit)) ) {
//                if (outputLimited == limitedOutput)
//                  return 0;
//                if (outputLimited == fillOutput) {
//                    /* Match description too long : reduce it */
//                    U32 newMatchCode = 15 /* in token */ - 1 /* to avoid needing a zero byte */ + ((U32)(olimit - op) - 2 - 1 - LASTLITERALS) * 255;
//ip -= matchCode - newMatchCode;
//                    matchCode = newMatchCode;
//                }
//            }
//            if (matchCode >= ML_MASK) {
//                * token += ML_MASK;
//                matchCode -= ML_MASK;
//                LZ4_write32(op, 0xFFFFFFFF);
//                while (matchCode >= 4*255) {
//                    op+=4;
//                    LZ4_write32(op, 0xFFFFFFFF);
//matchCode -= 4*255;
//                }
//                op += matchCode / 255;
//                * op++ = (BYTE) (matchCode % 255);
//            } else
//                * token += (BYTE) (matchCode);
//        }

//        anchor = ip;

//        /* Test end of chunk */
//        if (ip >= mflimitPlusOne) break;

//        /* Fill table */
//        LZ4_putPosition(ip-2, cctx->hashTable, tableType, base);

//        /* Test next position */
//        if (tableType == byPtr) {

//            match = LZ4_getPosition(ip, cctx->hashTable, tableType, base);
//LZ4_putPosition(ip, cctx->hashTable, tableType, base);
//            if ((match+MAX_DISTANCE >= ip)
//              && (LZ4_read32(match) == LZ4_read32(ip)) )
//            { token=op++; * token = 0; goto _next_match; }

//        } else {   /* byU32, byU16 */

//            U32 const h = LZ4_hashPosition(ip, tableType);
//U32 const current = (U32)(ip - base);
//U32 matchIndex = LZ4_getIndexOnHash(h, cctx->hashTable, tableType);
//assert(matchIndex<current);
//            if (dictDirective == usingDictCtx) {
//                if (matchIndex<startIndex) {
//                    /* there was no match, try the dictionary */
//                    matchIndex = LZ4_getIndexOnHash(h, dictCtx->hashTable, byU32);
//match = dictBase + matchIndex;
//                    lowLimit = dictionary;   /* required for match length counter */
//                    matchIndex += dictDelta;
//                } else {
//                    match = base + matchIndex;
//                    lowLimit = (const BYTE*)source;  /* required for match length counter */
//                }
//            } else if (dictDirective==usingExtDict) {
//                if (matchIndex<startIndex) {
//                    match = dictBase + matchIndex;
//                    lowLimit = dictionary;   /* required for match length counter */
//                } else {
//                    match = base + matchIndex;
//                    lowLimit = (const BYTE*)source;   /* required for match length counter */
//                }
//            } else {   /* single memory segment */
//                match = base + matchIndex;
//            }
//            LZ4_putIndexOnHash(current, h, cctx->hashTable, tableType);
//assert(matchIndex<current);
//            if (((dictIssue==dictSmall) ? (matchIndex >= prefixIdxLimit) : 1)
//              && ((tableType==byU16) ? 1 : (matchIndex+MAX_DISTANCE >= current))
//              && (LZ4_read32(match) == LZ4_read32(ip)) ) {
//                token=op++;
//                * token = 0;
//                if (maybe_extMem) offset = current - matchIndex;
//DEBUGLOG(6, "seq.start:%i, literals=%u, match.start:%i",
//            (int)(anchor-(const BYTE*)source), 0, (int) (ip-(const BYTE*)source));
//                goto _next_match;
//            }
//        }

//        /* Prepare next loop */
//        forwardH = LZ4_hashPosition(++ip, tableType);

//    }

//_last_literals:
//    /* Encode Last Literals */
//    {   size_t lastRun = (size_t)(iend - anchor);
//        if ((outputLimited) &&  /* Check output buffer overflow */
//            (op + lastRun + 1 + ((lastRun+255-RUN_MASK)/255) > olimit)) {
//            if (outputLimited == fillOutput) {
//                /* adapt lastRun to fill 'dst' */
//                lastRun  = (olimit-op) - 1;
//                lastRun -= (lastRun+240)/255;
//            }
//            if (outputLimited == limitedOutput)
//                return 0;
//        }
//        if (lastRun >= RUN_MASK) {
//            size_t accumulator = lastRun - RUN_MASK;
//            * op++ = RUN_MASK << ML_BITS;
//            for(; accumulator >= 255 ; accumulator-=255) * op++ = 255;
//            * op++ = (BYTE) accumulator;
//        } else {
//            * op++ = (BYTE) (lastRun<<ML_BITS);
//        }
//        memcpy(op, anchor, lastRun);
//ip = anchor + lastRun;
//        op += lastRun;
//    }

//    if (outputLimited == fillOutput) {
//        * inputConsumed = (int)(((const char*)ip)-source);
//    }
//    DEBUGLOG(5, "LZ4_compress_generic: compressed %i bytes into %i bytes", inputSize, (int)(((char*) op) - dest));
//    return (int) (((char*) op) - dest);
//        }

//        private static void LZ4_resetStream(LZ4_stream_t* stream)
//        {
//            Unsafe.InitBlock(stream, 0, (uint)sizeof(LZ4_stream_t));
//        }

//        private static int Lz4HC_compress_hashChain<TLimitedOutput, TDictCtx>(
//            LZ4HC_CCtx* ctx,
//            ref byte* source,
//            ref byte* dest,
//            int inputSize,
//            int maxOutputSize,
//            int maxNbAttempts)
//            where TLimitedOutput : ILimitedOutput
//            where TDictCtx : IDictCtx
//        {
//            if (IsArch64)
//            {
//                return IsLittleEndian
//                    ? Lz4HC_compress_hashChain<TLimitedOutput, TDictCtx, X64, LittleEndian>(ctx, ref source, ref dest, inputSize, maxOutputSize, maxNbAttempts)
//                    : Lz4HC_compress_hashChain<TLimitedOutput, TDictCtx, X64, BigEndian>(ctx, ref source, ref dest, inputSize, maxOutputSize, maxNbAttempts);
//            }
//            else
//            {
//                return IsLittleEndian
//                    ? Lz4HC_compress_hashChain<TLimitedOutput, TDictCtx, X32, LittleEndian>(ctx, ref source, ref dest, inputSize, maxOutputSize, maxNbAttempts)
//                    : Lz4HC_compress_hashChain<TLimitedOutput, TDictCtx, X32, BigEndian>(ctx, ref source, ref dest, inputSize, maxOutputSize, maxNbAttempts);
//            } 
//        }

//        private static int Lz4HC_compress_hashChain<TLimitedOutput, TDictCtx, TArch, TEndian>(
//            LZ4HC_CCtx* ctx,
//            ref byte* source,
//            ref byte* dest,
//            int inputSize,
//            int maxOutputSize,
//            int maxNbAttempts)
//            where TLimitedOutput : ILimitedOutput
//            where TDictCtx : IDictCtx
//            where TArch : struct, IArch
//            where TEndian : IEndian
//        {
//            var patternAnalysis = (maxNbAttempts > 128);

//            var ip = source;
//            var anchor = ip;
//            var iend = ip + inputSize;
//            var mflimit = iend - MFLIMIT;
//            var matchlimit = iend - LASTLITERALS;

//            var optr = dest;
//            var op = dest;
//            var oend = op + maxOutputSize;

//            int ml0, ml, ml2, ml3;
//            byte* start0, ref0;
//            byte* @ref = null;
//            byte* start2 = null;
//            byte* ref2 = null;
//            byte* start3 = null;
//            byte* ref3 = null;

//            /* init */
//            if (typeof(TLimitedOutput) == typeof(LimitedDestSize)) oend -= LASTLITERALS; /* Hack for support LZ4 format restriction */
//            if (inputSize < LZ4_MIN_LENGTH) goto _last_literals;

//            /* Main Loop */
//            while (ip <= mflimit)
//            {
//                ml = LZ4HC_InsertAndFindBestMatch<TDictCtx, TArch, TEndian>(ctx, ip, matchlimit, &@ref, maxNbAttempts, patternAnalysis);
//                if (ml < MINMATCH) { ip++; continue; }

//                /* saved, in case we would skip too much */
//                start0 = ip; ref0 = @ref; ml0 = ml;

//                _Search2:
//                {
//                    if (ip + ml <= mflimit)
//                    {
//                        ml2 = LZ4HC_InsertAndGetWiderMatch<TDictCtx, FavorCompressionRatio, TArch, TEndian>(ctx,
//                            ip + ml - 2, ip + 0, matchlimit, ml, &ref2, &start2,
//                            maxNbAttempts, patternAnalysis, 0);
//                    }
//                    else ml2 = ml;

//                    if (ml2 == ml)
//                    {   /* No better match => encode ML1 */
//                        optr = op;
//                        if (LZ4HC_encodeSequence<TLimitedOutput>(&ip, &op, &anchor, ml, @ref, oend))
//                            goto _dest_overflow;
//                        continue;
//                    }

//                    if (start0 < ip)
//                    {
//                        /* first match was skipped at least once */
//                        if (start2 < ip + ml0)
//                        {   /* squeezing ML1 between ML0(original ML1) and ML2 */
//                            ip = start0;
//                            @ref = ref0;
//                            /* restore initial ML1 */
//                            ml = ml0;
//                        }
//                    }

//                    /* Here, start0==ip */
//                    if (start2 - ip < 3)
//                    {  /* First Match too small : removed */
//                        ml = ml2;
//                        ip = start2;
//                        @ref = ref2;
//                        goto _Search2;
//                    }
//                }

//                _Search3:
//                {
//                    /* At this stage, we have :
//                    *  ml2 > ml1, and
//                    *  ip1+3 <= ip2 (usually < ip1+ml1) */
//                    if ((start2 - ip) < OPTIMAL_ML)
//                    {
//                        var new_ml = ml;
//                        if (new_ml > OPTIMAL_ML) new_ml = OPTIMAL_ML;
//                        if (ip + new_ml > start2 + ml2 - MINMATCH) new_ml = (int)(start2 - ip) + ml2 - MINMATCH;
//                        var correction = new_ml - (int)(start2 - ip);
//                        if (correction > 0)
//                        {
//                            start2 += correction;
//                            ref2 += correction;
//                            ml2 -= correction;
//                        }
//                    }
//                    /* Now, we have start2 = ip+new_ml, with new_ml = min(ml, OPTIMAL_ML=18) */

//                    if (start2 + ml2 <= mflimit)
//                    {
//                        ml3 = LZ4HC_InsertAndGetWiderMatch<TDictCtx, FavorCompressionRatio, TArch, TEndian>(ctx,
//                            start2 + ml2 - 3, start2, matchlimit, ml2, &ref3, &start3,
//                            maxNbAttempts, patternAnalysis, 0);
//                    }
//                    else
//                    {
//                        ml3 = ml2;
//                    }

//                    if (ml3 == ml2)
//                    {  /* No better match => encode ML1 and ML2 */
//                        /* ip & ref are known; Now for ml */
//                        if (start2 < ip + ml) ml = (int)(start2 - ip);
//                        /* Now, encode 2 sequences */
//                        optr = op;
//                        if (LZ4HC_encodeSequence<TLimitedOutput>(&ip, &op, &anchor, ml, @ref, oend)) goto _dest_overflow;
//                        ip = start2;
//                        optr = op;
//                        if (LZ4HC_encodeSequence<TLimitedOutput>(&ip, &op, &anchor, ml2, ref2, oend)) goto _dest_overflow;
//                        continue;
//                    }

//                    if (start3 < ip + ml + 3)
//                    {  /* Not enough space for match 2 : remove it */
//                        if (start3 >= (ip + ml))
//                        {  /* can write Seq1 immediately ==> Seq2 is removed, so Seq3 becomes Seq1 */
//                            if (start2 < ip + ml)
//                            {
//                                var correction = (int)(ip + ml - start2);
//                                start2 += correction;
//                                ref2 += correction;
//                                ml2 -= correction;
//                                if (ml2 < MINMATCH)
//                                {
//                                    start2 = start3;
//                                    ref2 = ref3;
//                                    ml2 = ml3;
//                                }
//                            }

//                            optr = op;
//                            if (LZ4HC_encodeSequence<TLimitedOutput>(&ip, &op, &anchor, ml, @ref, oend)) goto _dest_overflow;
//                            ip = start3;
//                            @ref = ref3;
//                            ml = ml3;

//                            start0 = start2;
//                            ref0 = ref2;
//                            ml0 = ml2;
//                            goto _Search2;
//                        }

//                        start2 = start3;
//                        ref2 = ref3;
//                        ml2 = ml3;
//                        goto _Search3;
//                    }

//                    /*
//                    * OK, now we have 3 ascending matches;
//                    * let's write the first one ML1.
//                    * ip & ref are known; Now decide ml.
//                    */
//                    if (start2 < ip + ml)
//                    {
//                        if ((start2 - ip) < OPTIMAL_ML)
//                        {
//                            if (ml > OPTIMAL_ML) ml = OPTIMAL_ML;
//                            if (ip + ml > start2 + ml2 - MINMATCH) ml = (int)(start2 - ip) + ml2 - MINMATCH;
//                            var correction = ml - (int)(start2 - ip);
//                            if (correction > 0)
//                            {
//                                start2 += correction;
//                                ref2 += correction;
//                                ml2 -= correction;
//                            }
//                        }
//                        else
//                        {
//                            ml = (int)(start2 - ip);
//                        }
//                    }
//                    optr = op;
//                    if (LZ4HC_encodeSequence<TLimitedOutput>(&ip, &op, &anchor, ml, @ref, oend)) goto _dest_overflow;

//                    /* ML2 becomes ML1 */
//                    ip = start2; @ref = ref2; ml = ml2;

//                    /* ML3 becomes ML2 */
//                    start2 = start3; ref2 = ref3; ml2 = ml3;

//                    /* let's find a new ML3 */
//                    goto _Search3;
//                }
//            }

//            _last_literals:
//            { /* Encode Last Literals */
//                var lastRunSize = (int) (iend - anchor); /* literals */
//                var litLength = (lastRunSize + 255 - RUN_MASK) / 255;
//                var totalSize = 1 + litLength + lastRunSize;

//                if (typeof(TLimitedOutput) == typeof(LimitedDestSize)) oend += LASTLITERALS; /* restore correct value */
//                // Todo: Check Jitter constant propagation with typeof(TLimitedOutput) != typeof(NoLimit) instead of
//                // Todo: typeof(TLimitedOutput) == typeof(LimitedDestSize) || typeof(TLimitedOutput) == typeof(LimitedOutput)
//                if ((typeof(TLimitedOutput) == typeof(LimitedDestSize) ||
//                     typeof(TLimitedOutput) == typeof(LimitedOutput)) && op + totalSize > oend)
//                {
//                    if (typeof(TLimitedOutput) == typeof(LimitedOutput)) return 0; /* Check output limit */
//                    /* adapt lastRunSize to fill 'dest' */
//                    lastRunSize = (int)(oend - op) - 1;
//                    litLength = (lastRunSize + 255 - RUN_MASK) / 255;
//                    lastRunSize -= litLength;
//                }

//                ip = anchor + lastRunSize;

//                if (lastRunSize >= RUN_MASK)
//                {
//                    var accumulator = lastRunSize - RUN_MASK;
//                    *op++ = (RUN_MASK << ML_BITS);
//                    for (; accumulator >= 255; accumulator -= 255) *op++ = 255;
//                    *op++ = (byte)accumulator;
//                }
//                else
//                {
//                    *op++ = (byte)(lastRunSize << ML_BITS);
//                }
//                Unsafe.CopyBlock(op, anchor, (uint)lastRunSize);
//                op += lastRunSize;
//            }

//            /* End */
//            var result = (int)(op - dest);
//            source = ip;
//            dest = op;
//            return result;

//            _dest_overflow:
//            {
//                if (typeof(TLimitedOutput) == typeof(LimitedDestSize))
//                {
//                    op = optr; /* restore correct out pointer */
//                    goto _last_literals;
//                }

//                return 0;
//            }
//        }

//        #region Helpers

//        private static int LZ4HC_InsertAndFindBestMatch<TDictCtx, TArch, TEndian>(LZ4HC_CCtx* hc4,
//            byte* ip, byte* iLimit, byte** matchpos, int maxNbAttempts, bool patternAnalysis)
//            where TDictCtx : IDictCtx
//            where TArch : struct, IArch
//            where TEndian : IEndian
//        {
//            var uselessPtr = ip;
//            /* note : LZ4HC_InsertAndGetWiderMatch() is able to modify the starting position of a match (*startpos),
//            * but this won't be the case here, as we define iLowLimit==ip,
//            * so LZ4HC_InsertAndGetWiderMatch() won't be allowed to search past ip */
//            return LZ4HC_InsertAndGetWiderMatch<TDictCtx, FavorCompressionRatio, TArch, TEndian>(hc4, ip, ip, iLimit, MINMATCH - 1, matchpos, &uselessPtr, 
//                maxNbAttempts, patternAnalysis, 0 /*chainSwap*/);
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private static int LZ4HC_InsertAndGetWiderMatch<TDictCtx, THCFavor, TArch, TEndian>(
//            LZ4HC_CCtx* hc4,
//            byte* ip, byte* iLowLimit, byte* iHighLimit,
//            int longest, byte** matchpos, byte** startpos,
//            int maxNbAttempts, bool patternAnalysis, int chainSwap)
//            where TDictCtx : IDictCtx
//            where THCFavor : IHCFavor
//            where TArch : struct, IArch
//            where TEndian : IEndian
//        {
//            var chainTable = hc4->chainTable;
//            var hashTable = hc4->hashTable;
//            var dictCtx = hc4->dictCtx;
//            var @base = hc4->@base;
//            var dictLimit = hc4->dictLimit;
//            var lowPrefixPtr = @base + dictLimit;
//            var ipIndex = (uint) (ip - @base);
//            var lowestMatchIndex = (hc4->lowLimit + 64 * Bits.KILO_BYTE > ipIndex) ? hc4->lowLimit : ipIndex - MAX_DISTANCE;
//            var dictBase = hc4->dictBase;
//            var lookBackLength = (int) (ip - iLowLimit);
//            var nbAttempts = maxNbAttempts;
//            var matchChainPos = 0;
//            var pattern = LZ4_read32(ip);
//            uint matchIndex, dictMatchIndex;
//            var repeat = RepeatState.Untested;
//            var srcPatternLength = 0;

//            /* First Match */
//            LZ4HC_Insert(hc4, ip);
//            matchIndex = hashTable[LZ4HC_hashPtr(ip)];

//            while (matchIndex >= lowestMatchIndex && nbAttempts != 0)
//            {
//                var matchLength = 0;
//                nbAttempts--;
//                Debug.Assert(matchIndex < ipIndex);
//                if (typeof(THCFavor) == typeof(FavorCompressionRatio) && (ipIndex - matchIndex < 8))
//                {
//                    /* do nothing */
//                }
//                else if (matchIndex >= dictLimit)
//                {   /* within current Prefix */
//                    var matchPtr = @base + matchIndex;
//                    Debug.Assert(matchPtr >= lowPrefixPtr);
//                    Debug.Assert(matchPtr < ip);
//                    Debug.Assert(longest >= 1);
//                    if (LZ4_read16(iLowLimit + longest - 1) == LZ4_read16(matchPtr - lookBackLength + longest - 1))
//                    {
//                        if (LZ4_read32(matchPtr) == pattern)
//                        {
//                            var back = lookBackLength != 0 ? LZ4HC_countBack(ip, matchPtr, iLowLimit, lowPrefixPtr) : 0;
//                            matchLength = MINMATCH + LZ4_count<TArch, TEndian>(ip + MINMATCH, matchPtr + MINMATCH, iHighLimit);
//                            matchLength -= back;
//                            if (matchLength > longest)
//                            {
//                                longest = matchLength;
//                                *matchpos = matchPtr + back;
//                                *startpos = ip + back;
//                            }
//                        }
//                    }
//                }
//                else
//                {   /* lowestMatchIndex <= matchIndex < dictLimit */
//                    var matchPtr = dictBase + matchIndex;
//                    if (LZ4_read32(matchPtr) == pattern)
//                    {
//                        var dictStart = dictBase + hc4->lowLimit;
//                        var back = 0;
//                        var vLimit = ip + (dictLimit - matchIndex);
//                        if (vLimit > iHighLimit) vLimit = iHighLimit;
//                        matchLength = LZ4_count<TArch, TEndian>(ip + MINMATCH, matchPtr + MINMATCH, vLimit) + MINMATCH;
//                        if ((ip + matchLength == vLimit) && (vLimit < iHighLimit))
//                            matchLength += LZ4_count<TArch, TEndian>(ip + matchLength, lowPrefixPtr, iHighLimit);
//                        back = lookBackLength != 0 ? LZ4HC_countBack(ip, matchPtr, iLowLimit, dictStart) : 0;
//                        matchLength -= back;
//                        if (matchLength > longest)
//                        {
//                            longest = matchLength;
//                            *matchpos = @base + matchIndex + back;   /* virtual pos, relative to ip, to retrieve offset */
//                            *startpos = ip + back;
//                        }
//                    }
//                }

//                if (chainSwap != 0 && matchLength == longest)
//                {    /* better match => select a better chain */
//                    Debug.Assert(lookBackLength == 0);   /* search forward only */
//                    if (matchIndex + longest <= ipIndex)
//                    {
//                        var distanceToNextMatch = 1u;
//                        int pos;
//                        for (pos = 0; pos <= longest - MINMATCH; pos++)
//                        {
//                            var candidateDist = chainTable[matchIndex + pos];
//                            if (candidateDist > distanceToNextMatch)
//                            {
//                                distanceToNextMatch = candidateDist;
//                                matchChainPos = pos;
//                            }
//                        }
//                        if (distanceToNextMatch > 1)
//                        {
//                            if (distanceToNextMatch > matchIndex) break;   /* avoid overflow */
//                            matchIndex -= distanceToNextMatch;
//                            continue;
//                        }
//                    }
//                }

//                {
//                    var distNextMatch = chainTable[matchIndex];
//                    if (patternAnalysis && distNextMatch == 1 && matchChainPos == 0)
//                    {
//                        var matchCandidateIdx = matchIndex - 1;
//                        /* may be a repeated pattern */
//                        if (repeat == RepeatState.Untested)
//                        {
//                            if (((pattern & 0xFFFF) == (pattern >> 16))
//                                & ((pattern & 0xFF) == (pattern >> 24)))
//                            {
//                                repeat = RepeatState.Confirmed;
//                                srcPatternLength = LZ4HC_countPattern<TArch, TEndian>(ip + sizeof(uint), iHighLimit, pattern) + sizeof(uint);
//                            }
//                            else
//                            {
//                                repeat = RepeatState.Not;
//                            }
//                        }
//                        if (repeat == RepeatState.Confirmed
//                            && matchCandidateIdx >= dictLimit)
//                        {   /* same segment only */
//                            var matchPtr = @base + matchCandidateIdx;
//                            if (LZ4_read32(matchPtr) == pattern)
//                            {  /* good candidate */
//                                var forwardPatternLength = LZ4HC_countPattern<TArch, TEndian>(matchPtr + sizeof(uint), iHighLimit, pattern) + sizeof(uint);
//                                var lowestMatchPtr = (lowPrefixPtr + MAX_DISTANCE >= ip) ? lowPrefixPtr : ip - MAX_DISTANCE;
//                                var backLength = LZ4HC_reverseCountPattern(matchPtr, lowestMatchPtr, pattern);
//                                var currentSegmentLength = backLength + forwardPatternLength;

//                                if ((currentSegmentLength >= srcPatternLength)   /* current pattern segment large enough to contain full srcPatternLength */
//                                    && (forwardPatternLength <= srcPatternLength))
//                                { /* haven't reached this position yet */
//                                    matchIndex = matchCandidateIdx + (uint)forwardPatternLength - (uint)srcPatternLength;  /* best position, full pattern, might be followed by more match */
//                                }
//                                else
//                                {
//                                    matchIndex = matchCandidateIdx - (uint)backLength;   /* farthest position in current segment, will find a match of length currentSegmentLength + maybe some back */
//                                    if (lookBackLength == 0)
//                                    {  /* no back possible */
//                                        var maxMl = Math.Min(currentSegmentLength, srcPatternLength);
//                                        if (longest < maxMl)
//                                        {
//                                            Debug.Assert(maxMl < 2u * Bits.GIGA_BYTE);
//                                            longest = (int)maxMl;
//                                            *matchpos = @base + matchIndex;   /* virtual pos, relative to ip, to retrieve offset */
//                                            *startpos = ip;
//                                        }
//                                        {
//                                            var distToNextPattern = chainTable[matchIndex];
//                                            if (distToNextPattern > matchIndex) break;  /* avoid overflow */
//                                            matchIndex -= distToNextPattern;
//                                        }
//                                    }
//                                }
//                                continue;
//                            }
//                        }
//                    }
//                }   /* PA optimization */

//                /* follow current chain */
//                matchIndex -= chainTable[matchIndex + matchChainPos];

//            }  /* while ((matchIndex>=lowestMatchIndex) && (nbAttempts)) */

//            if (typeof(TDictCtx) == typeof(UsingDictCtx) && nbAttempts != 0 && ipIndex - lowestMatchIndex < MAX_DISTANCE)
//            {
//                var dictEndOffset = dictCtx->end - dictCtx->@base;
//                Debug.Assert(dictEndOffset <= 1 * Bits.GIGA_BYTE);
//                dictMatchIndex = dictCtx->hashTable[LZ4HC_hashPtr(ip)];
//                matchIndex = dictMatchIndex + lowestMatchIndex - (uint)dictEndOffset;
//                while (ipIndex - matchIndex <= MAX_DISTANCE && nbAttempts-- != 0) // Todo: Check if(nbAttempts-- != 0) vs C++ if(nbAttempts--)
//                {
//                    var matchPtr = dictCtx->@base + dictMatchIndex;

//                    if (LZ4_read32(matchPtr) == pattern)
//                    {
//                        var vLimit = ip + (dictEndOffset - dictMatchIndex);
//                        if (vLimit > iHighLimit) vLimit = iHighLimit;
//                        var mlt  = LZ4_count<TArch, TEndian>(ip + MINMATCH, matchPtr + MINMATCH, vLimit) + MINMATCH;
//                        var back = lookBackLength != 0 ? LZ4HC_countBack(ip, matchPtr, iLowLimit, dictCtx->@base + dictCtx->dictLimit) : 0;
//                        mlt -= back;
//                        if (mlt > longest)
//                        {
//                            longest = mlt;
//                            *matchpos = @base + matchIndex + back;
//                            *startpos = ip + back;
//                        }
//                    }

//                    {
//                        var nextOffset = dictCtx->chainTable[dictMatchIndex];
//                        dictMatchIndex -= nextOffset;
//                        matchIndex -= nextOffset;
//                    }
//                }
//            }

//            return longest;
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private static int LZ4HC_countBack(byte* ip, byte* match, byte* iMin, byte* mMin)
//        {
//            var back = 0;
//            var min = (int)Math.Max(iMin - ip, mMin - match);
//            Debug.Assert(min <= 0);
//            Debug.Assert(ip >= iMin); Debug.Assert((uint)(ip - iMin) < (1U << 31));
//            Debug.Assert(match >= mMin); Debug.Assert((uint)(match - mMin) < (1U << 31));
//            while (back > min && ip[back - 1] == match[back - 1])
//                back--;
//            return back;
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private static int LZ4_count<TArch, TEndian>(byte* pIn, byte* pMatch, byte* pInLimit)
//            where TArch : struct, IArch
//            where TEndian : IEndian
//        {
//            var pStart = pIn;
//            while (pIn < pInLimit - (STEPSIZE - 1))
//            {
//                var diff = LZ4_xor<TArch>(pMatch, pIn);
//                if (diff.Not())
//                {
//                    pIn += STEPSIZE;
//                    pMatch += STEPSIZE;
//                    continue;
//                }
//                pIn += LZ4_NbCommonBytes<TArch, TEndian>(diff);
//                return (int)(pIn - pStart);
//            }

//            if(typeof(TArch) == typeof(X64))
//            {
//                if (pIn < pInLimit - 3 && LZ4_read32(pMatch) == LZ4_read32(pIn)) { pIn += sizeof(uint); pMatch += sizeof(uint); }
//            }

//            if (pIn < pInLimit - 1 && LZ4_read16(pMatch) == LZ4_read16(pIn)) { pIn += sizeof(ushort); pMatch += sizeof(ushort); }
//            if (pIn < pInLimit && *pMatch == *pIn) pIn++;

//            return (int)(pIn - pStart);
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private static void LZ4HC_Insert(LZ4HC_CCtx* hc4, byte* ip)
//        {
//            var chainTable = hc4->chainTable;
//            var hashTable  = hc4->hashTable;
//            var @base = hc4->@base;
//            var target = (uint)(ip - @base);
//            var idx = hc4->nextToUpdate;

//            while (idx < target)
//            {
//                var h = LZ4HC_hashPtr(@base + idx);
//                var delta = idx - hashTable[h];
//                if (delta > MAX_DISTANCE) delta = MAX_DISTANCE;
//                chainTable[(ushort)idx] = (ushort)delta;
//                hashTable[h] = idx;
//                idx++;
//            }

//            hc4->nextToUpdate = target;
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private static bool LZ4HC_encodeSequence<TLimitedOutput>(byte** ip, byte** op, byte** anchor, 
//            int matchLength, byte* match, byte* oend)
//        {
//            var token = (*op)++;

//            /* Encode Literal length */
//            var length = (int)(*ip - *anchor);
//            // Todo: Check here Jit constant propagation otherwize replace by (typeof(TLimitedOutput) == typeof(LimitedDestSize) || typeof(TLimitedOutput) == typeof(LimitedOutput))
//            if (typeof(TLimitedOutput) != typeof(NoLimit) && ((*op + (length >> 8) + length + (2 + 1 + LASTLITERALS)) > oend)) return true;   /* Check output limit */
//            if (length >= RUN_MASK)
//            {
//                var len = length - RUN_MASK;
//                *token = (RUN_MASK << ML_BITS);
//                for (; len >= 255; len -= 255) *(*op)++ = 255;
//                **op++ = (byte)len;
//            }
//            else
//            {
//                *token = (byte)(length << ML_BITS);
//            }

//            /* Copy Literals */
//            LZ4_wildCopy(*op, *anchor, (*op) + length);
//            *op += length;

//            /* Encode Offset */
//            LZ4_write16(*op, (ushort)(*ip - match));
//            *op += 2;

//            /* Encode MatchLength */
//            Debug.Assert(matchLength >= MINMATCH);
//            length = matchLength - MINMATCH;

//            // Todo: Check here Jit constant propagation otherwize replace by (typeof(TLimitedOutput) == typeof(LimitedDestSize) || typeof(TLimitedOutput) == typeof(LimitedOutput))
//            if (typeof(TLimitedOutput) != typeof(NoLimit) && (*op + (length >> 8) + (1 + LASTLITERALS) > oend)) return true;   /* Check output limit */
//            if (length >= ML_MASK)
//            {
//                *token += ML_MASK;
//                length -= ML_MASK;
//                for (; length >= 510; length -= 510) { *(*op)++ = 255; *(*op)++ = 255; }
//                if (length >= 255) { length -= 255; *(*op)++ = 255; }
//                *(*op)++ = (byte)length;
//            }
//            else
//            {
//                *token += (byte)(length);
//            }

//            /* Prepare next loop */
//            *ip += matchLength;
//            *anchor = *ip;

//            return false;
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private static void LZ4_wildCopy(byte* dest, byte* src, byte* destEnd)
//        {
//            do
//            {
//                ((ulong*)dest)[0] = ((ulong*)src)[0];
//                if (dest + 1 * sizeof(ulong) >= destEnd)
//                    goto Return;

//                ((ulong*)dest)[1] = ((ulong*)src)[1];
//                if (dest + 2 * sizeof(ulong) >= destEnd)
//                    goto Return;

//                ((ulong*)dest)[2] = ((ulong*)src)[2];
//                if (dest + 3 * sizeof(ulong) >= destEnd)
//                    goto Return;

//                ((ulong*)dest)[3] = ((ulong*)src)[3];

//                dest += 4 * sizeof(ulong);
//                src += 4 * sizeof(ulong);
//            }
//            while (dest < destEnd);

//            Return:
//            // ReSharper disable once RedundantJumpStatement
//            return;
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private static uint LZ4HC_hashPtr(byte* ptr)
//        {
//            return (LZ4_read32(ptr) * 2654435761U) >> ((MINMATCH - 8) - LZ4HC_HASH_LOG);
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private static uint LZ4_read32(byte* p)
//        {
//            return *(uint*)p;
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private static void LZ4_write32(byte* p, uint value)
//        {
//            *(uint*)p = value;
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private static uint LZ4_read16(byte* p)
//        {
//            return *(ushort*)p;
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private static void LZ4_write16(byte* p, ushort value)
//        {
//            *(ushort*)p = value;
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private static TArch LZ4_xor<TArch>(byte* x, byte* y)
//            where TArch : struct, IArch
//        {
//            if (typeof(TArch) == typeof(X32))
//                return (TArch)(object)(*(uint*)x ^ *(uint*)y);
//            if (typeof(TArch) == typeof(X64))
//                return (TArch)(object)(*(ulong*)x ^ *(ulong*)y);

//            throw new NotSupportedException(typeof(TArch).ToString());
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private static int LZ4_NbCommonBytes<TArch, TEndian>(TArch val)
//            where TArch : struct, IArch
//            where TEndian : IEndian
//        {
//            // Todo : In this method still missing som improvements...
//            // Todo : Take into account if we running into _MSC_VER, _WIN64, __clang__, __GNUC__, ...
//            return val.NbBytes<TEndian>();
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private static int LZ4HC_countPattern<TArch, TEndian>(byte* ip, byte* iEnd, uint pattern32)
//            where TArch : struct, IArch
//            where TEndian : IEndian
//        {
//            var iStart = ip;

//            if (typeof(TArch) == typeof(X32))
//            {
//                var pattern = pattern32;
//                while (ip < iEnd - (sizeof(X32) - 1))
//                {
//                    var diff = LZ4_xor<TArch>(ip, (byte*)&pattern);
//                    if (diff.Not()) { ip += sizeof(X32); continue; }
//                    ip += LZ4_NbCommonBytes<TArch, TEndian>(diff);
//                    return (int)(ip - iStart);
//                }

//                if (typeof(TEndian) == typeof(LittleEndian))
//                {
//                    var patternByte = pattern;
//                    while (ip < iEnd && *ip == (byte)patternByte)
//                    {
//                        ip++; patternByte >>= 8;
//                    }
//                }
//                else if(typeof(TEndian) == typeof(BigEndian))
//                {  /* big endian */
//                    var bitOffset = sizeof(X32) * 8 - 8;
//                    while (ip < iEnd)
//                    {
//                        var @byte = (byte)(pattern >> bitOffset);
//                        if (*ip != @byte) break;
//                        ip++; bitOffset -= 8;
//                    }
//                } else throw new NotSupportedException(typeof(TEndian).ToString());

//                return (int)(ip - iStart);
//            }

//            if (typeof(TArch) == typeof(X64))
//            {
//                var pattern = pattern32 + ((ulong) pattern32 << 32);

//                while (ip < iEnd - (sizeof(X64) - 1))
//                {
//                    var diff = LZ4_xor<TArch>(ip, (byte*)&pattern);
//                    if (diff.Not()) { ip += sizeof(X64); continue; }
//                    ip += LZ4_NbCommonBytes<TArch, TEndian>(diff);
//                    return (int)(ip - iStart);
//                }

//                if (typeof(TEndian) == typeof(LittleEndian))
//                {
//                    var patternByte = pattern;
//                    while (ip < iEnd && *ip == (byte)patternByte)
//                    {
//                        ip++; patternByte >>= 8;
//                    }
//                }
//                else if (typeof(TEndian) == typeof(BigEndian))
//                {  /* big endian */
//                    var bitOffset = sizeof(X64) * 8 - 8;
//                    while (ip < iEnd)
//                    {
//                        var @byte = (byte)(pattern >> bitOffset);
//                        if (*ip != @byte) break;
//                        ip++; bitOffset -= 8;
//                    }
//                }
//                else throw new NotSupportedException(typeof(TEndian).ToString());

//                return (int)(ip - iStart);
//            }

//            throw new NotSupportedException(typeof(TArch).ToString());
//        }

//        /// <summary>
//        /// pattern must be a sample of repetitive pattern of length 1, 2 or 4 (but not 3!)
//        /// read using natural platform endianess
//        /// </summary>
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private static int LZ4HC_reverseCountPattern(byte* ip, byte* iLow, uint pattern)
//        {
//            var iStart = ip;

//            while (ip >= iLow + 4)
//            {
//                if (LZ4_read32(ip - 4) != pattern) break;
//                ip -= 4;
//            }

//            /* works for any endianess */
//            var bytePtr = (byte*) &pattern + 3;
//            while (ip > iLow)
//            {
//                if (*(ip - 1) != *bytePtr) break;
//                ip--; bytePtr--;
//            }

//            return (int) (iStart - ip);
//        }

//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//        private static bool LZ4_IsLittleEndian()
//        {
//            uint v = 1;
//            return *(byte*) &v != 0;
//        }

//        #endregion
//    }
//}