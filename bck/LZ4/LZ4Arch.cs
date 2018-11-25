using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using DotNetCross.Memory;
using Minotaur.Core;
using Minotaur.Core.Platform;

namespace Minotaur.Codecs.LZ4
{
    public static unsafe partial class Lz4
    {
        #region Compress

        public static int LZ4_compress_fast(byte* source, byte* dest, int inputSize, int maxOutputSize, int acceleration)
        {
            LZ4_stream_t ctx;
            return LZ4_compress_fast_extState(&ctx, source, dest, inputSize, maxOutputSize, acceleration);
        }

        private static int LZ4_compress_fast_extState(LZ4_stream_t* state, byte* source, byte* dest, int inputSize, int maxOutputSize, int acceleration)
        {
            LZ4_resetStream(state);
            if (acceleration < 1) acceleration = ACCELERATION_DEFAULT;

            var consumedInput = 0;
            if (maxOutputSize >= LZ4_compressBound(inputSize))
            {
                if (inputSize < LZ4_64Klimit)
                    return LZ4_compress_generic<NotLimited, ByU16, NoDict, NoDictIssue>(
                        state, source, dest, inputSize, ref consumedInput, 0, acceleration);
                if(sizeof(byte*) == sizeof(uint))
                    return LZ4_compress_generic<NotLimited, ByPtr, NoDict, NoDictIssue>(
                        state, source, dest, inputSize, ref consumedInput, 0, acceleration);

                return LZ4_compress_generic<NotLimited, ByU32, NoDict, NoDictIssue>(
                    state, source, dest, inputSize, ref consumedInput, 0, acceleration);
            }
            
            if (inputSize < LZ4_64Klimit)
                return LZ4_compress_generic<LimitedOutput, ByU16, NoDict, NoDictIssue>(
                    state, source, dest, inputSize, ref consumedInput, maxOutputSize, acceleration);
            if (sizeof(byte*) == sizeof(uint))
                return LZ4_compress_generic<LimitedOutput, ByPtr, NoDict, NoDictIssue>(
                    state, source, dest, inputSize, ref consumedInput, maxOutputSize, acceleration);

            return LZ4_compress_generic<LimitedOutput, ByU32, NoDict, NoDictIssue>(
                state, source, dest, inputSize, ref consumedInput, maxOutputSize, acceleration);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int LZ4_compress_generic<TLimitedOutput, TTableType, TDict, TDictIssue>(
            LZ4_stream_t* cctx,
            byte* source,
            byte* dest,
            int inputSize,
            ref int inputConsumed, /*only written when TLimitedOutput is FillOutput*/
            int maxOutputSize,
            int acceleration)
            where TLimitedOutput : struct, ILimitedOutput
            where TTableType : struct, ITableType
            where TDict : struct, IDict
            where TDictIssue : struct, IDictIssue
        {
            if (is64Bits)
            {
                if (isLittleEndian)
                    return LZ4_compress_generic<TLimitedOutput, TTableType, TDict, TDictIssue, LittleEndian, X64>(
                        cctx, source, dest, inputSize, ref inputConsumed, maxOutputSize, acceleration);

                return LZ4_compress_generic<TLimitedOutput, TTableType, TDict, TDictIssue, BigEndian, X64>(
                    cctx, source, dest, inputSize, ref inputConsumed, maxOutputSize, acceleration);
            }

            if (isLittleEndian)
                return LZ4_compress_generic<TLimitedOutput, TTableType, TDict, TDictIssue, LittleEndian, X32>(
                    cctx, source, dest, inputSize, ref inputConsumed, maxOutputSize, acceleration);

            return LZ4_compress_generic<TLimitedOutput, TTableType, TDict, TDictIssue, BigEndian, X32>(
                cctx, source, dest, inputSize, ref inputConsumed, maxOutputSize, acceleration);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int LZ4_compress_generic<TLimitedOutput, TTableType, TDict, TDictIssue, TEndian, TArch>(
            LZ4_stream_t* cctx,
            byte* source,
            byte* dest,
            int inputSize,
            ref int inputConsumed, /*only written when TLimitedOutput is FillOutput*/
            int maxOutputSize,
            int acceleration)
            where TLimitedOutput : struct, ILimitedOutput
            where TTableType : struct, ITableType
            where TDict : struct, IDict
            where TDictIssue : struct, IDictIssue
            where TEndian : struct, IEndian
            where TArch : unmanaged, IArch
        {
            var ip = source;

            var startIndex = cctx->currentOffset;
            var @base = source - startIndex;

            var dictCtx = cctx->dictCtx;
            byte* dictionary;
            uint dictSize, dictDelta;
            if (typeof(TDict) == typeof(UsingDictCtx))
            {
                dictionary = dictCtx->dictionary;
                dictSize = dictCtx->dictSize;
                dictDelta = startIndex - cctx->dictCtx->currentOffset; /* make indexes in dictCtx comparable with index in current context */
            }
            else
            {
                dictionary = cctx->dictionary;
                dictSize = cctx->dictSize;
                dictDelta = 0;
            }

            var prefixIdxLimit = startIndex - dictSize;   /* used when dictDirective == dictSmall */
            var dictEnd = dictionary + dictSize;
            var anchor = source;
            var iend = ip + inputSize;
            var mflimitPlusOne = iend - MFLIMIT + 1;
            var matchlimit = iend - LASTLITERALS;

            /* the dictCtx currentOffset is indexed on the start of the dictionary,
             * while a dictionary in the current context precedes the currentOffset */
            var dictBase = typeof(TDict) == typeof(UsingDictCtx) ?
                dictionary + dictSize - dictCtx->currentOffset :
                dictionary + dictSize - startIndex;

            var op = dest;
            var olimit = op + maxOutputSize;

            var offset = 0u;

            Debug.WriteLine($"LZ4_compress_generic: srcSize={inputSize}, tableType={typeof(TTableType).Name}");
            /* Init conditions */
            if (typeof(TLimitedOutput) == typeof(FillOutput) && maxOutputSize < 1) return 0; /* Impossible to store anything */
            if (inputSize > LZ4_MAX_INPUT_SIZE) return 0;   /* Unsupported inputSize, too large (or negative) */
            if (typeof(TTableType) == typeof(ByU16) && inputSize >= LZ4_64Klimit) return 0;  /* Size too large (not within 64K limit) */
            if (typeof(TTableType) == typeof(ByPtr) && typeof(TDict) != typeof(NoDict)) throw new NotSupportedException($"Table type {typeof(ByPtr).Name} only supports a {typeof(NoDict).Name} directive");
            if (acceleration < 1) throw new ArgumentException("Acceleration has to be greater than 0", nameof(acceleration));


            var lowLimit = source - (typeof(TDict) == typeof(WithPrefix64K) ? dictSize : 0);

            /* Update context state */
            if (typeof(TDict) == typeof(UsingDictCtx))
            {
                /* Subsequent linked blocks can't use the dictionary. */
                /* Instead, they use the block we just compressed. */
                cctx->dictCtx = null;
                cctx->dictSize = (uint)inputSize;
            }
            else
            {
                cctx->dictSize += (uint)inputSize;
            }
            cctx->currentOffset += (uint)inputSize;
            cctx->tableType = ToU16<TTableType>();

            if (inputSize < LZ4_minLength) goto _last_literals;        /* Input too small, no compression (all literals) */

            /* First Byte */
            LZ4_putPosition<TTableType, TEndian, TArch>(ip, cctx->hashTable, @base);
            ip++;
            var forwardH = LZ4_hashPosition<TTableType, TEndian, TArch>(ip);

            /* Main Loop */
            for (; ; )
            {
                byte* match;
                byte* token;

                /* Find a match */
                if (typeof(TTableType) == typeof(ByPtr))
                {
                    var forwardIp = ip;
                    var step = 1;
                    var searchMatchNb = acceleration << LZ4_skipTrigger;
                    do
                    {
                        var h = forwardH;
                        ip = forwardIp;
                        forwardIp += step;
                        step = (searchMatchNb++ >> LZ4_skipTrigger);

                        if (forwardIp > mflimitPlusOne) goto _last_literals;
                        if (!(ip < mflimitPlusOne))
                            throw new IndexOutOfRangeException("ip < mflimitPlusOne");

                        match = LZ4_getPositionOnHash<TTableType>(h, cctx->hashTable, @base);
                        forwardH = LZ4_hashPosition<TTableType, TEndian, TArch>(forwardIp);
                        LZ4_putPositionOnHash<TTableType>(ip, h, cctx->hashTable, @base);

                    }
                    while (match + MAX_DISTANCE < ip || Mem.ReadU32(match) != Mem.ReadU32(ip));
                }
                else
                {   /* byU32, byU16 */

                    var forwardIp = ip;
                    var step = 1;
                    var searchMatchNb = acceleration << LZ4_skipTrigger;
                    do
                    {
                        var h = forwardH;
                        var current = (uint)(forwardIp - @base);
                        var matchIndex = LZ4_getIndexOnHash<TTableType>(h, cctx->hashTable);
                        assert(matchIndex <= current);
                        assert((ulong)(forwardIp - @base) < _2GB - 1);
                        ip = forwardIp;
                        forwardIp += step;
                        step = searchMatchNb++ >> LZ4_skipTrigger;

                        if (forwardIp > mflimitPlusOne) goto _last_literals;
                        assert(ip < mflimitPlusOne);

                        if (typeof(TDict) == typeof(UsingDictCtx))
                        {
                            if (matchIndex < startIndex)
                            {
                                /* there was no match, try the dictionary */
                                assert(typeof(TTableType) == typeof(ByU32));
                                matchIndex = LZ4_getIndexOnHash<ByU32>(h, dictCtx->hashTable);
                                match = dictBase + matchIndex;
                                matchIndex += dictDelta;   /* make dictCtx index comparable with current context */
                                lowLimit = dictionary;
                            }
                            else
                            {
                                match = @base + matchIndex;
                                lowLimit = source;
                            }
                        }
                        else if (typeof(TDict) == typeof(UsingExtDict))
                        {
                            if (matchIndex < startIndex)
                            {
                                Debug.WriteLine($"extDict candidate: matchIndex={matchIndex}  <  startIndex={startIndex}");
                                assert(startIndex - matchIndex >= MINMATCH);
                                match = dictBase + matchIndex;
                                lowLimit = dictionary;
                            }
                            else
                            {
                                match = @base + matchIndex;
                                lowLimit = source;
                            }
                        }
                        else    /* single continuous memory segment */
                            match = @base + matchIndex;

                        forwardH = LZ4_hashPosition<TTableType, TEndian, TArch>(forwardIp);
                        LZ4_putIndexOnHash<TTableType>(current, h, cctx->hashTable);

                        if (typeof(TDictIssue) == typeof(DictSmall) && matchIndex < prefixIdxLimit) continue;    /* match outside of valid area */
                        assert(matchIndex < current);
                        if (typeof(TTableType) != typeof(ByU16) && matchIndex + MAX_DISTANCE < current) continue;  /* too far */
                        if (typeof(TTableType) == typeof(ByU16)) assert((current - matchIndex) <= MAX_DISTANCE);     /* too_far presumed impossible with byU16 */

                        if (Mem.ReadU32(match) == Mem.ReadU32(ip))
                        {
                            if (typeof(TDict) == typeof(UsingExtDict) || typeof(TDict) == typeof(UsingDictCtx)) offset = current - matchIndex;
                            break;   /* match found */
                        }

                    } while (true);
                }
                
                /* Catch up */
                while ((ip > anchor) & (match > lowLimit) && ip[-1] == match[-1]) { ip--; match--; }

                /* Encode Literals */
                {
                    var litLength = (uint)(ip - anchor);
                    token = op++;
                    if (typeof(TLimitedOutput) == typeof(LimitedOutput) &&  /* Check output buffer overflow */
                        op + litLength + (2 + 1 + LASTLITERALS) + litLength / 255 > olimit)
                        return 0;

                    if (typeof(TLimitedOutput) == typeof(FillOutput) &&
                        op + (litLength + 240) / 255 /* litlen */ + litLength /* literals */ + 2 /* offset */ + 1 /* token */ + MFLIMIT - MINMATCH /* min last literals so last match is <= end - MFLIMIT */ > olimit)
                    {
                        op--;
                        goto _last_literals;
                    }
                    if (litLength >= RUN_MASK)
                    {
                        var len = litLength - RUN_MASK;
                        *token = (RUN_MASK << ML_BITS);
                        for (; len >= 255; len -= 255) *op++ = 255;
                        *op++ = (byte)len;
                    }
                    else *token = (byte)(litLength << ML_BITS);

                    /* Copy Literals */
                    Mem.WildCopy(op, anchor, op + litLength);
                    op += litLength;
                    Debug.WriteLine($"seq.start:{(int)(anchor - source)}, literals={litLength}, match.start:{(int)(ip - source)}");
                }

        _next_match:
                /* at this stage, the following variables must be correctly set :
                 * - ip : at start of LZ operation
                 * - match : at start of previous pattern occurence; can be within current prefix, or within extDict
                 * - offset : if maybe_ext_memSegment==1 (constant)
                 * - lowLimit : must be == dictionary to mean "match is within extDict"; must be == source otherwise
                 * - token and *token : position to write 4-bits for match length; higher 4-bits for literal length supposed already written
                 */

                if (typeof(TLimitedOutput) == typeof(FillOutput) &&
                    op + 2 /* offset */ + 1 /* token */ + MFLIMIT - MINMATCH /* min last literals so last match is <= end - MFLIMIT */ > olimit)
                {
                    /* the match was too close to the end, rewind and go to last literals */
                    op = token;
                    goto _last_literals;
                }

                /* Encode Offset */
                if (typeof(TDict) == typeof(UsingExtDict) || typeof(TDict) == typeof(UsingDictCtx))
                {   /* static test */
                    Debug.WriteLine($"             with offset={offset}  (ext if > {(int)(ip - source)}");
                    assert(offset <= MAX_DISTANCE && offset > 0);

                    LZ4_writeLE16<TEndian>(op, (ushort)offset);
                    op += 2;
                }
                else
                {
                    Debug.WriteLine($"             with offset={(uint)(ip - match)}  (same segment)");
                    assert(ip - match <= MAX_DISTANCE);

                    LZ4_writeLE16<TEndian>(op, (ushort)(ip - match));
                    op += 2;
                }

                /* Encode MatchLength */
                {
                    uint matchCode;

                    if ((typeof(TDict) == typeof(UsingExtDict) || typeof(TDict) == typeof(UsingDictCtx))
                        && lowLimit == dictionary /* match within extDict */ )
                    {
                        var limit = ip + (dictEnd - match);
                        assert(dictEnd > match);
                        if (limit > matchlimit) limit = matchlimit;
                        matchCode = LZ4_count<TEndian, TArch>(ip + MINMATCH, match + MINMATCH, limit);
                        ip += MINMATCH + matchCode;
                        if (ip == limit)
                        {
                            var more = LZ4_count<TEndian, TArch>(limit, source, matchlimit);
                            matchCode += more;
                            ip += more;
                        }
                        Debug.WriteLine($"             with matchLength={matchCode + MINMATCH} starting in extDict");
                    }
                    else
                    {
                        matchCode = LZ4_count<TEndian, TArch>(ip + MINMATCH, match + MINMATCH, matchlimit);
                        ip += MINMATCH + matchCode;
                        Debug.WriteLine($"             with matchLength={matchCode + MINMATCH}");
                    }

                    if (typeof(TLimitedOutput) != typeof(NotLimited) &&    /* Check output buffer overflow */
                        op + (1 + LASTLITERALS) + (matchCode >> 8) > olimit)
                    {
                        if (typeof(TLimitedOutput) == typeof(LimitedOutput))
                            return 0;

                        if (typeof(TLimitedOutput) == typeof(FillOutput))
                        {
                            /* Match description too long : reduce it */
                            var newMatchCode = 15 /* in token */ - 1 /* to avoid needing a zero byte */ + ((uint)(olimit - op) - 2 - 1 - LASTLITERALS) * 255;
                            ip -= matchCode - newMatchCode;
                            matchCode = newMatchCode;
                        }
                    }

                    if (matchCode >= ML_MASK)
                    {
                        *token += ML_MASK;
                        matchCode -= ML_MASK;
                        Mem.Write(op, 0xFFFFFFFF);
                        while (matchCode >= 4 * 255)
                        {
                            op += 4;
                            Mem.Write(op, 0xFFFFFFFF);
                            matchCode -= 4 * 255;
                        }
                        op += matchCode / 255;
                        *op++ = (byte)(matchCode % 255);
                    }
                    else
                        *token += (byte)matchCode;
                }

                anchor = ip;

                /* Test end of chunk */
                if (ip >= mflimitPlusOne) break;

                /* Fill table */
                LZ4_putPosition<TTableType, TEndian, TArch>(ip - 2, cctx->hashTable, @base);

                /* Test next position */
                if (typeof(TTableType) == typeof(ByPtr))
                {

                    match = LZ4_getPosition<TTableType, TEndian, TArch>(ip, cctx->hashTable, @base);
                    LZ4_putPosition<TTableType, TEndian, TArch>(ip, cctx->hashTable, @base);

                    if (match + MAX_DISTANCE >= ip && Mem.ReadU32(match) == Mem.ReadU32(ip))
                    {
                        token = op++;
                        *token = 0;
                        goto _next_match;
                    }
                }
                else
                {   /* byU32, byU16 */

                    var h = LZ4_hashPosition<TTableType, TEndian, TArch>(ip);
                    var current = (uint)(ip - @base);
                    var matchIndex = LZ4_getIndexOnHash<TTableType>(h, cctx->hashTable);
                    assert(matchIndex < current);

                    if (typeof(TDict) == typeof(UsingDictCtx))
                    {
                        if (matchIndex < startIndex)
                        {
                            /* there was no match, try the dictionary */
                            matchIndex = LZ4_getIndexOnHash<ByU32>(h, dictCtx->hashTable);
                            match = dictBase + matchIndex;
                            lowLimit = dictionary;   /* required for match length counter */
                            matchIndex += dictDelta;
                        }
                        else
                        {
                            match = @base + matchIndex;
                            lowLimit = source;  /* required for match length counter */
                        }
                    }
                    else if (typeof(TDict) == typeof(UsingExtDict))
                    {
                        if (matchIndex < startIndex)
                        {
                            match = dictBase + matchIndex;
                            lowLimit = dictionary;   /* required for match length counter */
                        }
                        else
                        {
                            match = @base + matchIndex;
                            lowLimit = source;   /* required for match length counter */
                        }
                    }
                    else
                    {   /* single memory segment */
                        match = @base + matchIndex;
                    }

                    LZ4_putIndexOnHash<TTableType>(current, h, cctx->hashTable);
                    assert(matchIndex < current);

                    if ((typeof(TDictIssue) != typeof(DictSmall) || matchIndex >= prefixIdxLimit)
                      && (typeof(TTableType) == typeof(ByU16) || matchIndex + MAX_DISTANCE >= current)
                      && Mem.ReadU32(match) == Mem.ReadU32(ip))
                    {
                        token = op++;
                        *token = 0;
                        if (typeof(TDict) == typeof(UsingExtDict) || typeof(TDict) == typeof(UsingDictCtx))
                            offset = current - matchIndex;

                        Debug.WriteLine($"seq.start:{(int)(anchor - source)}, literals={0}, match.start:{(int)(ip - source)}");
                        goto _next_match;
                    }
                }

                /* Prepare next loop */
                forwardH = LZ4_hashPosition<TTableType, TEndian, TArch>(++ip);

            }

        _last_literals:
            /* Encode Last Literals */
            {
                var lastRun = (uint)(iend - anchor);
                if (typeof(TLimitedOutput) != typeof(NotLimited) &&  /* Check output buffer overflow */
                    op + lastRun + 1 + (lastRun + 255 - RUN_MASK) / 255 > olimit)
                {
                    if (typeof(TLimitedOutput) == typeof(FillOutput))
                    {
                        /* adapt lastRun to fill 'dst' */
                        lastRun = (uint)(olimit - op) - 1;
                        lastRun -= (lastRun + 240) / 255;
                    }
                    if (typeof(TLimitedOutput) == typeof(LimitedOutput))
                        return 0;
                }

                if (lastRun >= RUN_MASK)
                {
                    var accumulator = lastRun - RUN_MASK;
                    *op++ = RUN_MASK << ML_BITS;
                    for (; accumulator >= 255; accumulator -= 255) *op++ = 255;
                    *op++ = (byte)accumulator;
                }
                else *op++ = (byte)(lastRun << ML_BITS);

                Unsafe.CopyBlock(op, anchor, lastRun);
                ip = anchor + lastRun;
                op += lastRun;
            }

            if (typeof(TLimitedOutput) == typeof(FillOutput))
                inputConsumed = (int)(ip - source);

            Debug.WriteLine($"LZ4_compress_generic: compressed {inputSize} bytes into {(int)(op - dest)} bytes");
            return (int)(op - dest);
        }

        #endregion

        #region Decompress

        public static int LZ4_decompress_fast(ref byte* source, ref byte* dest, int originalSize) =>
            LZ4_decompress_generic<EndOnOutputSize, Full, WithPrefix64K>(
                ref source, ref dest, 0, originalSize, 0, dest - _64KB, null, _64KB);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int LZ4_decompress_generic<TEndCondition, TEarlyEnd, TDict>(
            ref byte* src,
            ref byte* dst,
            int srcSize,
            int outputSize, /* If TEndCondition==EndOnInputSize, this value is `dstCapacity` */
            int targetOutputSize, /* only used if TEarlyEnd==Partial*/
            byte* lowPrefix, /* always <= dst, == dst when no prefix */
            byte* dictStart, /* only if TDict==UsingExtDict */
            uint dictSize) /* note : = 0 if TDict==NoDict */
            where TEndCondition : struct, IEndCondition
            where TEarlyEnd : struct, IEarlyEnd
            where TDict : struct, IDict
        {
            if (isLittleEndian)
                return LZ4_decompress_generic<TEndCondition, TEarlyEnd, TDict, LittleEndian>(
                    ref src, ref dst, srcSize, outputSize, targetOutputSize, lowPrefix, dictStart, dictSize);

            return LZ4_decompress_generic<TEndCondition, TEarlyEnd, TDict, BigEndian>(
                ref src, ref dst, srcSize, outputSize, targetOutputSize, lowPrefix, dictStart, dictSize);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int LZ4_decompress_generic<TEndCondition, TEarlyEnd, TDict, TEndian>(
            ref byte* src,
            ref byte* dst,
            int srcSize,
            int outputSize, /* If TEndCondition==EndOnInputSize, this value is `dstCapacity` */
            int targetOutputSize, /* only used if TEarlyEnd==Partial*/
            byte* lowPrefix, /* always <= dst, == dst when no prefix */
            byte* dictStart, /* only if TDict==UsingExtDict */
            uint dictSize) /* note : = 0 if TDict==NoDict */
            where TEndCondition : struct, IEndCondition
            where TEarlyEnd : struct, IEarlyEnd
            where TDict : struct, IDict
            where TEndian : struct, IEndian
        {
            var ip = src;
            var iend = ip + srcSize;

            var op = dst;
            var oend = op + outputSize;
            var oexit = op + targetOutputSize;

            var dictEnd = dictStart + dictSize;

            /* Set up the "end" pointers for the shortcut. */
            var shortiend = iend - (typeof(TEndCondition) == typeof(EndOnInputSize) ? 14 : 8) /*maxLL*/ - 2 /*offset*/;
            var shortoend = oend - (typeof(TEndCondition) == typeof(EndOnInputSize) ? 14 : 8) /*maxLL*/ - 18 /*maxML*/;

            Debug.WriteLine($"LZ4_decompress_generic (srcSize:{srcSize})");

            /* Special cases */
            if (typeof(TEarlyEnd) == typeof(Partial) && oexit > oend - MFLIMIT) oexit = oend - MFLIMIT;                      /* targetOutputSize too high => just decode everything */
            if (typeof(TEndCondition) == typeof(EndOnInputSize) && outputSize == 0) return srcSize == 1 && *ip == 0 ? 0 : -1;  /* Empty output buffer */
            if (typeof(TEndCondition) == typeof(EndOnOutputSize) && outputSize == 0) return *ip == 0 ? 1 : -1;
            if (typeof(TEndCondition) == typeof(EndOnInputSize) && srcSize == 0) return -1;

            /* Main Loop : decode sequences */
            byte* cpy;
            while (true)
            {
                byte* match;
                ushort offset;

                var token = *ip++;
                var length = token >> ML_BITS; /* literal length */

                assert(typeof(TEndCondition) != typeof(EndOnInputSize) || ip <= iend); /* ip < iend before the increment */

                /* A two-stage shortcut for the most common case:
                 * 1) If the literal length is 0..14, and there is enough space,
                 * enter the shortcut and copy 16 bytes on behalf of the literals
                 * (in the fast mode, only 8 bytes can be safely copied this way).
                 * 2) Further if the match length is 4..18, copy 18 bytes in a similar
                 * manner; but we ensure that there's enough space in the output for
                 * those 18 bytes earlier, upon entering the shortcut (in other words,
                 * there is a combined check for both stages).
                 */
                if ((typeof(TEndCondition) == typeof(EndOnInputSize) ? length != RUN_MASK : length <= 8)
                  /* strictly "less than" on input, to re-enter the loop with at least one byte */
                  && (typeof(TEndCondition) != typeof(EndOnInputSize) || ip < shortiend) & (op <= shortoend))
                {
                    /* Copy the literals */
                    if (typeof(TEndCondition) == typeof(EndOnInputSize))
                        Mem.Copy16(op, ip);
                    else Mem.Copy8(op, ip);

                    op += length;
                    ip += length;

                    /* The second stage: prepare for match copying, decode full info.
                     * If it doesn't work out, the info won't be wasted. */
                    length = token & ML_MASK; /* match length */
                    offset = LZ4_readLE16<TEndian>(ip);
                    ip += 2;
                    match = op - offset;

                    /* Do not deal with overlapping matches. */
                    if (length != ML_MASK
                      && offset >= 8
                      && (typeof(TDict) == typeof(WithPrefix64K) || match >= lowPrefix))
                    {
                        /* Copy the match. */
                        Unsafe.CopyBlock(op, match, 18);
                        op += length + MINMATCH;
                        /* Both stages worked, load the next token. */
                        continue;
                    }

                    /* The second stage didn't work out, but the info is ready.
                     * Propel it right to the point of match copying. */
                    goto _copy_match;
                }

                /* decode literal length */
                if (length == RUN_MASK)
                {
                    byte s;
                    if (typeof(TEndCondition) == typeof(EndOnInputSize))
                    {
                        if (ip >= iend - RUN_MASK) goto _output_error; /* overflow detection */

                        do
                        {
                            s = *ip++;
                            length += s;
                        } while (ip < iend - RUN_MASK && s == 255);

                        if (op + length < op || ip + length < ip) goto _output_error;   /* overflow detection */
                    }
                    else
                    {
                        do
                        {
                            s = *ip++;
                            length += s;
                        } while (s == 255);
                    }
                }

                /* copy literals */
                cpy = op + length;
                if (typeof(TEndCondition) == typeof(EndOnInputSize) && (cpy > (typeof(TEarlyEnd) == typeof(Partial) ? oexit : oend - MFLIMIT) || ip + length > iend - (2 + 1 + LASTLITERALS))
                    || typeof(TEndCondition) == typeof(EndOnOutputSize) && cpy > oend - WILDCOPYLENGTH)
                {
                    if (typeof(TEarlyEnd) == typeof(Partial))
                    {
                        if (cpy > oend) goto _output_error; /* Error : write attempt beyond end of output buffer */
                        if (typeof(TEndCondition) == typeof(EndOnInputSize) && ip + length > iend)
                            goto _output_error;   /* Error : read attempt beyond end of input buffer */
                    }
                    else
                    {
                        if (typeof(TEndCondition) != typeof(EndOnInputSize) && cpy != oend)
                            goto _output_error; /* Error : block decoding must stop exactly there */
                        if (typeof(TEndCondition) == typeof(EndOnInputSize) && (ip + length != iend || cpy > oend))
                            goto _output_error;   /* Error : input must be consumed */
                    }
                    Unsafe.CopyBlock(op, ip, (uint)length);
                    ip += length;
                    op += length;
                    break;     /* Necessarily EOF, due to parsing restrictions */
                }

                Mem.WildCopy(op, ip, cpy);
                ip += length;
                op = cpy;

                /* get offset */
                offset = LZ4_readLE16<TEndian>(ip);
                ip += 2;
                match = op - offset;

                /* get matchlength */
                length = token & ML_MASK;

                _copy_match:
                if (typeof(TEndCondition) == typeof(EndOnInputSize) && dictSize < _64KB && !(match + dictSize < lowPrefix))
                    goto _output_error;   /* Error : offset outside buffers */

                Mem.Write(op, (uint)offset);   /* costs ~1%; silence an msan warning when offset==0 */

                if (length == ML_MASK)
                {
                    byte s;
                    do
                    {
                        s = *ip++;

                        if (typeof(TEndCondition) == typeof(EndOnInputSize) && ip > iend - LASTLITERALS)
                            goto _output_error;

                        length += s;
                    } while (s == 255);

                    if (typeof(TEndCondition) == typeof(EndOnInputSize) && op + length < op)
                        goto _output_error;   /* overflow detection */
                }

                length += MINMATCH;

                /* check external dictionary */
                if (typeof(TDict) == typeof(UsingExtDict) && match < lowPrefix)
                {
                    if (op + length > oend - LASTLITERALS)
                        goto _output_error; /* doesn't respect parsing restriction */

                    if (length <= lowPrefix - match)
                    {
                        /* match can be copied as a single segment from external dictionary */
                        Platform.Move(op, dictEnd - (lowPrefix - match), length);
                        op += length;
                    }
                    else
                    {
                        /* match encompass external dictionary and current block */
                        var copySize = (uint)(lowPrefix - match);
                        var restSize = (uint)length - copySize;
                        Unsafe.CopyBlock(op, dictEnd - copySize, copySize);
                        op += copySize;
                        if (restSize > (uint)(op - lowPrefix))
                        {  /* overlap copy */
                            var endOfMatch = op + restSize;
                            var copyFrom = lowPrefix;
                            while (op < endOfMatch)
                                *op++ = *copyFrom++;
                        }
                        else
                        {
                            Unsafe.CopyBlock(op, lowPrefix, restSize);
                            op += restSize;
                        }
                    }
                    continue;
                }

                /* copy match within block */
                cpy = op + length;
                if (offset < 8)
                {
                    op[0] = match[0];
                    op[1] = match[1];
                    op[2] = match[2];
                    op[3] = match[3];
                    match += inc32table[offset];
                    Unsafe.CopyBlock(op + 4, match, 4);
                    match -= dec64table[offset];
                }
                else
                {
                    Unsafe.CopyBlock(op, match, 8);
                    match += 8;
                }

                op += 8;

                if (cpy > oend - 12)
                {
                    var oCopyLimit = oend - (WILDCOPYLENGTH - 1);
                    if (cpy > oend - LASTLITERALS)
                        goto _output_error;    /* Error : last LASTLITERALS bytes must be literals (uncompressed) */

                    if (op < oCopyLimit)
                    {
                        Mem.WildCopy(op, match, oCopyLimit);
                        match += oCopyLimit - op;
                        op = oCopyLimit;
                    }

                    while (op < cpy) *op++ = *match++;
                }
                else
                {
                    Mem.Copy8(op, match);
                    if (length > 16)
                        Mem.WildCopy(op + 8, match + 8, cpy);
                }

                op = cpy;   /* correction */
            }


            var wrote = (int)(op - dst);
            var read = (int)(ip - src);

            dst += wrote;
            src += read;

            /* end of decoding */
            if (typeof(TEndCondition) == typeof(EndOnInputSize))
                return wrote;     /* Nb of output bytes decoded */

            return read;   /* Nb of input bytes read */

            /* Overflow error detected */
            _output_error:
            return (int)(-(ip - src)) - 1;
        }

        #endregion
    }
}
