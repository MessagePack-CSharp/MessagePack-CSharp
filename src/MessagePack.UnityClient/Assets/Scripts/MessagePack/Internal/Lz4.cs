#pragma warning disable SA1300 // Element should begin with upper-case letter
#pragma warning disable SA1507 // Code should not contain multiple blank lines in a row
#pragma warning disable SA1505 // Opening braces should not be followed by blank line
#pragma warning disable SA1508 // Closing braces should not be preceded by blank line
#pragma warning disable SA1520 // Use braces consistently
#pragma warning disable SA1025 // Code should not contain multiple whitespace in a row
#pragma warning disable SA1107 // Code should not contain multiple statements on one line
#pragma warning disable SA1307 // Accessible fields should begin with upper-case letter
#pragma warning disable SA1513 // Closing brace should be followed by blank line
#pragma warning disable SA1400 // Access modifier should be declared
#pragma warning disable SA1310 // Field names should not contain underscore
#pragma warning disable SA1119 // Statement should not use unnecessary parenthesis
#pragma warning disable SA1407 // Arithmetic expressions should declare precedence
#pragma warning disable SA1405 // Debug.Assert should provide message text
#pragma warning disable SA1503 // Braces should not be omitted
#pragma warning disable IDE1006

using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

using static MessagePack.Internal.dict_directive;
using static MessagePack.Internal.dictIssue_directive;
using static MessagePack.Internal.limitedOutput_directive;
using static MessagePack.Internal.tableType_t;
using BYTE = System.Byte;
using U16 = System.UInt16;
using U32 = System.UInt32;
using U64 = System.UInt64;

namespace MessagePack.Internal
{
    internal enum dict_directive
    {
        noDict = 0,
        withPrefix64k,
        usingExtDict,
        usingDictCtx,
    }

    internal enum dictIssue_directive
    {
        noDictIssue = 0,
        dictSmall,
    }

    internal enum limitedOutput_directive
    {
        notLimited = 0,
        limitedOutput = 1,
        fillOutput = 2,
    }

    internal enum tableType_t
    {
        clearedTable = 0,
        byPtr,
        byU32,
        byU16,
    }

    public static unsafe class Lz4
    {
        const int LZ4_MEMORY_USAGE = 14;
        const int LZ4_MAX_INPUT_SIZE = 0x7E000000;
        const int LZ4_HASHLOG = LZ4_MEMORY_USAGE - 2;
        const int LZ4_HASHTABLESIZE = 1 << LZ4_MEMORY_USAGE;
        const int LZ4_HASH_SIZE_U32 = 1 << LZ4_HASHLOG;
        const int LZ4_DISTANCE_MAX = 65535;

        const int ACCELERATION_DEFAULT = 1;

        const int KB = (1 << 10);
        const int MB = (1 << 20);
        const uint GB = (1U << 30);

        const int MINMATCH = 4;
        const int MFLIMIT = 12;
        const int LASTLITERALS = 5;
        const U32 LZ4_skipTrigger = 6;

        const int LZ4_64Klimit = ((64 * KB) + (MFLIMIT - 1));
        const int LZ4_minLength = (MFLIMIT + 1);
        const int LZ4_DISTANCE_ABSOLUTE_MAX = 65535;

        const int ML_BITS = 4;
        const uint ML_MASK = ((1U << ML_BITS) - 1);
        const int RUN_BITS = (8 - ML_BITS);
        const uint RUN_MASK = ((1U << RUN_BITS) - 1);

        static readonly int STEPSIZE = IntPtr.Size;


        static readonly uint[] DeBruijnBytePos = {
            0, 0, 0, 0, 0, 1, 1, 2,
            0, 3, 1, 3, 1, 4, 2, 7,
            0, 2, 3, 6, 1, 5, 3, 5,
            1, 3, 4, 4, 2, 5, 6, 7,
            7, 0, 1, 2, 3, 3, 4, 6,
            2, 6, 5, 5, 3, 4, 5, 6,
            7, 1, 2, 4, 6, 4, 4, 5,
            7, 2, 6, 5, 7, 6, 7, 7
        };

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint LZ4_NbCommonBytes(ulong val) => DeBruijnBytePos[unchecked((ulong)((long)val & -(long)val) * 0x0218A392CDABBD3Ful >> 58)];

        /// <summary>
        /// LZ4_compress_default: Compresses 'srcSize' bytes from buffer 'src' into already allocated 'dst' buffer of size 'dstCapacity'.
        /// </summary>
        /// <param name="src">source buffer.</param>
        /// <param name="dst">destination buffer.</param>
        /// <param name="srcSize">max supported value is LZ4_MAX_INPUT_SIZE.</param>
        /// <param name="dstCapacity">size of buffer 'dst' (which must be already allocated).</param>
        /// <returns>the number of bytes written into buffer 'dst' (necessarily &lt;= dstCapacity) or 0 if compression fails.</returns>
        public static int LZ4_compress_default(byte* src, byte* dst, int srcSize, int dstCapacity)
        {
            return LZ4_compress_fast(src, dst, srcSize, dstCapacity, 1);
        }

        /// <summary>
        /// LZ4_decompress_safe: 
        /// </summary>
        /// <param name="src">source buffer.</param>
        /// <param name="dst">destination buffer.</param>
        /// <param name="compressedSize">is the exact complete size of the compressed block.</param>
        /// <param name="dstCapacity">is the size of destination buffer (which must be already allocated), presumed an upper bound of decompressed size.</param>
        /// <returns>
        /// the number of bytes decompressed into destination buffer (necessarily &lt;= dstCapacity)
        /// If destination buffer is not large enough, decoding will stop and output an error code(negative value).
        /// If the source stream is detected malformed, the function will stop decoding and return a negative result.
        /// </returns>
        public static int LZ4_decompress_safe(byte* src, byte* dst, int compressedSize, int dstCapacity)
        {
            throw new NotImplementedException();
        }

        [Conditional("DEBUG")]
        static void assert(bool condition) => Debug.Assert(condition);

        [Conditional("DEBUG")]
        static void DEBUGLOG(int _, string format, params object[] args)
        {
            Console.WriteLine(string.Format(format, args));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void memcpy(void* destination, void* source, long size) => Buffer.MemoryCopy(source, destination, size, size);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static U32 LZ4_hash4(U32 sequence, tableType_t tableType)
        {
            if (tableType == byU16)
                return ((sequence * 2654435761U) >> ((MINMATCH * 8) - (LZ4_HASHLOG + 1)));
            else
                return ((sequence * 2654435761U) >> ((MINMATCH * 8) - LZ4_HASHLOG));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static U32 LZ4_hash5(U64 sequence, tableType_t tableType)
        {
            U32 hashLog = (U32)((tableType == byU16) ? LZ4_HASHLOG + 1 : LZ4_HASHLOG);
            if (BitConverter.IsLittleEndian)
            {
                const U64 prime5bytes = 889523592379UL;
                return (U32)(((sequence << 24) * prime5bytes) >> (64 - (int)hashLog));
            }
            else
            {
                const U64 prime8bytes = 11400714785074694791UL;
                return (U32)(((sequence >> 24) * prime8bytes) >> (64 - (int)hashLog));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ushort LZ4_read16(void* p) => *(ushort*)p;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint LZ4_read32(void* p) => *(uint*)p;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ulong LZ4_read64(void* p) => *(ulong*)p;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static ulong LZ4_read_ARCH(void* p) => (IntPtr.Size == 8) ? LZ4_read64(p) : LZ4_read32(p);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void LZ4_write16(void* memPtr, U16 value) => *(U16*)memPtr = value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void LZ4_write32(void* memPtr, U32 value) => *(U32*)memPtr = value;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static U32 LZ4_hashPosition(void* p, tableType_t tableType)
        {
            if ((IntPtr.Size == 8) && (tableType != byU16)) return LZ4_hash5(LZ4_read_ARCH(p), tableType);
            return LZ4_hash4(LZ4_read32(p), tableType);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void LZ4_putPosition(BYTE* p, void* tableBase, tableType_t tableType, BYTE* srcBase)
        {
            U32 h = LZ4_hashPosition(p, tableType);
            LZ4_putPositionOnHash(p, h, tableBase, tableType, srcBase);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static BYTE* LZ4_getPosition(BYTE* p, void* tableBase, tableType_t tableType, BYTE* srcBase)
        {
            U32 h = LZ4_hashPosition(p, tableType);
            return LZ4_getPositionOnHash(h, tableBase, tableType, srcBase);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void LZ4_putPositionOnHash(BYTE* p, U32 h, void* tableBase, tableType_t tableType, BYTE* srcBase)
        {
            switch (tableType)
            {
                case clearedTable: { /* illegal! */ assert(false); return; }
                case byPtr: { BYTE** hashTable = (BYTE**)tableBase; hashTable[h] = p; return; }
                case byU32: { U32* hashTable = (U32*)tableBase; hashTable[h] = (U32)(p - srcBase); return; }
                case byU16: { U16* hashTable = (U16*)tableBase; hashTable[h] = (U16)(p - srcBase); return; }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static BYTE* LZ4_getPositionOnHash(U32 h, void* tableBase, tableType_t tableType, BYTE* srcBase)
        {
            if (tableType == byPtr) { BYTE** hashTable = (BYTE**)tableBase; return hashTable[h]; }
            if (tableType == byU32) { U32* hashTable = (U32*)tableBase; return hashTable[h] + srcBase; }
            { U16* hashTable = (U16*)tableBase; return hashTable[h] + srcBase; }   /* default, to ensure a return */
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void LZ4_putIndexOnHash(U32 idx, U32 h, void* tableBase, tableType_t tableType)
        {
            switch (tableType)
            {
                default: /* fallthrough */
                case clearedTable: /* fallthrough */
                case byPtr: { /* illegal! */ assert(false); return; }
                case byU32: { U32* hashTable = (U32*)tableBase; hashTable[h] = idx; return; }
                case byU16: { U16* hashTable = (U16*)tableBase; assert(idx < 65536); hashTable[h] = (U16)idx; return; }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static U32 LZ4_getIndexOnHash(U32 h, void* tableBase, tableType_t tableType)
        {
            // LZ4_STATIC_ASSERT(LZ4_MEMORY_USAGE > 2);
            if (tableType == byU32)
            {
                U32* hashTable = (U32*)tableBase;
                assert(h < (1U << (LZ4_MEMORY_USAGE - 2)));
                return hashTable[h];
            }
            if (tableType == byU16)
            {
                U16* hashTable = (U16*)tableBase;
                assert(h < (1U << (LZ4_MEMORY_USAGE - 1)));
                return hashTable[h];
            }
            assert(false); return 0;  /* forbidden case */
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void LZ4_clearHash(U32 h, void* tableBase, tableType_t tableType)
        {
            switch (tableType)
            {
                default: /* fallthrough */
                case clearedTable: { /* illegal! */ assert(false); return; }
                case byPtr: { BYTE** hashTable = (BYTE**)tableBase; hashTable[h] = null; return; }
                case byU32: { U32* hashTable = (U32*)tableBase; hashTable[h] = 0; return; }
                case byU16: { U16* hashTable = (U16*)tableBase; hashTable[h] = 0; return; }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static void LZ4_wildCopy8(void* dstPtr, void* srcPtr, void* dstEnd)
        {
            BYTE* d = (BYTE*)dstPtr;
            BYTE* s = (BYTE*)srcPtr;
            BYTE* e = (BYTE*)dstEnd;

            do { memcpy(d, s, 8); d += 8; s += 8; } while (d < e);
        }

        static void LZ4_writeLE16(void* memPtr, U16 value)
        {
            if (BitConverter.IsLittleEndian)
            {
                LZ4_write16(memPtr, value);
            }
            else
            {
                BYTE* p = (BYTE*)memPtr;
                p[0] = (BYTE)value;
                p[1] = (BYTE)(value >> 8);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        static uint LZ4_count(BYTE* pIn, BYTE* pMatch, BYTE* pInLimit)
        {
            BYTE* pStart = pIn;

            if ((pIn < pInLimit - (STEPSIZE - 1)))
            {
                var diff = LZ4_read_ARCH(pMatch) ^ LZ4_read_ARCH(pIn);
                if (diff == 0)
                {
                    pIn += STEPSIZE; pMatch += STEPSIZE;
                }
                else
                {
                    return LZ4_NbCommonBytes(diff);
                }
            }

            while ((pIn < pInLimit - (STEPSIZE - 1)))
            {
                var diff = LZ4_read_ARCH(pMatch) ^ LZ4_read_ARCH(pIn);
                if (diff == 0) { pIn += STEPSIZE; pMatch += STEPSIZE; continue; }
                pIn += LZ4_NbCommonBytes(diff);
                return (uint)(pIn - pStart);
            }

            if ((STEPSIZE == 8) && (pIn < (pInLimit - 3)) && (LZ4_read32(pMatch) == LZ4_read32(pIn))) { pIn += 4; pMatch += 4; }
            if ((pIn < (pInLimit - 1)) && (LZ4_read16(pMatch) == LZ4_read16(pIn))) { pIn += 2; pMatch += 2; }
            if ((pIn < pInLimit) && (*pMatch == *pIn)) pIn++;
            return (uint)(pIn - pStart);
        }

        static int LZ4_compress_fast(byte* source, byte* dest, int inputSize, int maxOutputSize, int acceleration)
        {
            int result;
            //LZ4_stream_t ctx;
            //LZ4_stream_t* ctxPtr = &ctx;
            result = LZ4_compress_fast_extState(null, source, dest, inputSize, maxOutputSize, acceleration);
            return result;
        }

        static int LZ4_compress_fast_extState(void* state, byte* source, byte* dest, int inputSize, int maxOutputSize, int acceleration)
        {
            LZ4_stream_t_internal context = default;
            LZ4_stream_t_internal* ctx = &context;

            if (acceleration < 1) acceleration = ACCELERATION_DEFAULT;

            if (maxOutputSize >= LZ4_compressBound(inputSize))
            {
                if (inputSize < LZ4_64Klimit)
                {
                    return LZ4_compress_generic(ctx, source, dest, inputSize, null, 0, notLimited, byU16, noDict, noDictIssue, acceleration);
                }
                else
                {
                    tableType_t tableType = ((sizeof(void*) == 4) && ((uint)source > LZ4_DISTANCE_MAX)) ? byPtr : byU32;
                    return LZ4_compress_generic(ctx, source, dest, inputSize, null, 0, notLimited, tableType, noDict, noDictIssue, acceleration);
                }
            }
            else
            {
                if (inputSize < LZ4_64Klimit)
                {
                    return LZ4_compress_generic(ctx, source, dest, inputSize, null, maxOutputSize, limitedOutput, byU16, noDict, noDictIssue, acceleration);
                }
                else
                {
                    tableType_t tableType = ((sizeof(void*) == 4) && ((uint)source > LZ4_DISTANCE_MAX)) ? byPtr : byU32;
                    return LZ4_compress_generic(ctx, source, dest, inputSize, null, maxOutputSize, limitedOutput, tableType, noDict, noDictIssue, acceleration);
                }
            }
        }

        static int LZ4_compress_generic(
            LZ4_stream_t_internal* cctx,
            byte* source,
            byte* dest,
            int inputSize,
            int* inputConsumed, /* only written when outputDirective == fillOutput */
            int maxOutputSize,
            limitedOutput_directive outputDirective,
            tableType_t tableType,
            dict_directive dictDirective,
            dictIssue_directive dictIssue,
            int acceleration)
        {
            int result;
            BYTE* ip = (BYTE*)source;

            U32 startIndex = cctx->currentOffset;
            BYTE* @base = (BYTE*)source - startIndex;
            BYTE* lowLimit;

            LZ4_stream_t_internal* dictCtx = (LZ4_stream_t_internal*)cctx->dictCtx;
            BYTE* dictionary =
                dictDirective == usingDictCtx ? dictCtx->dictionary : cctx->dictionary;
            U32 dictSize =
                dictDirective == usingDictCtx ? dictCtx->dictSize : cctx->dictSize;
            U32 dictDelta = (dictDirective == usingDictCtx) ? startIndex - dictCtx->currentOffset : 0;   /* make indexes in dictCtx comparable with index in current context */

            bool maybe_extMem = (dictDirective == usingExtDict) || (dictDirective == usingDictCtx);
            U32 prefixIdxLimit = startIndex - dictSize;   /* used when dictDirective == dictSmall */
            BYTE* dictEnd = dictionary + dictSize;
            BYTE* anchor = (BYTE*)source;
            BYTE* iend = ip + inputSize;
            BYTE* mflimitPlusOne = iend - MFLIMIT + 1;
            BYTE* matchlimit = iend - LASTLITERALS;

            /* the dictCtx currentOffset is indexed on the start of the dictionary,
             * while a dictionary in the current context precedes the currentOffset */
            BYTE* dictBase = (dictDirective == usingDictCtx) ?
                                    dictionary + dictSize - dictCtx->currentOffset :
                                    dictionary + dictSize - startIndex;

            BYTE* op = (BYTE*)dest;
            BYTE* olimit = op + maxOutputSize;

            U32 offset = 0;
            U32 forwardH;

            DEBUGLOG(5, "LZ4_compress_generic: srcSize={0}, tableType={1}", inputSize, tableType);

            /* If init conditions are not met, we don't have to mark stream
             * as having dirty context, since no action was taken yet */
            if (outputDirective == fillOutput && maxOutputSize < 1) { return 0; } /* Impossible to store anything */
            if ((U32)inputSize > (U32)LZ4_MAX_INPUT_SIZE) { return 0; }           /* Unsupported inputSize, too large (or negative) */
            if ((tableType == byU16) && (inputSize >= LZ4_64Klimit)) { return 0; }  /* Size too large (not within 64K limit) */
            if (tableType == byPtr) assert(dictDirective == noDict);      /* only supported use case with byPtr */
            assert(acceleration >= 1);

            lowLimit = (BYTE*)source - (dictDirective == withPrefix64k ? dictSize : 0);

            /* Update context state */
            if (dictDirective == usingDictCtx)
            {
                /* Subsequent linked blocks can't use the dictionary. */
                /* Instead, they use the block we just compressed. */
                cctx->dictCtx = null;
                cctx->dictSize = (U32)inputSize;
            }
            else
            {
                cctx->dictSize += (U32)inputSize;
            }
            cctx->currentOffset += (U32)inputSize;
            cctx->tableType = (U16)tableType;

            if (inputSize < LZ4_minLength) goto _last_literals;        /* Input too small, no compression (all literals) */

            /* First Byte */
            LZ4_putPosition(ip, cctx->hashTable, tableType, @base);
            ip++; forwardH = LZ4_hashPosition(ip, tableType);

            /* Main Loop */
            for (; ; )
            {
                BYTE* match;
                BYTE* token;
                BYTE* filledIp;

                /* Find a match */
                if (tableType == byPtr)
                {
                    BYTE* forwardIp = ip;
                    int step = 1;
                    int searchMatchNb = acceleration << (int)LZ4_skipTrigger;
                    do
                    {
                        U32 h = forwardH;
                        ip = forwardIp;
                        forwardIp += step;
                        step = (searchMatchNb++ >> (int)LZ4_skipTrigger);

                        if ((forwardIp > mflimitPlusOne)) goto _last_literals;
                        assert(ip < mflimitPlusOne);

                        match = LZ4_getPositionOnHash(h, cctx->hashTable, tableType, @base);
                        forwardH = LZ4_hashPosition(forwardIp, tableType);
                        LZ4_putPositionOnHash(ip, h, cctx->hashTable, tableType, @base);

                    } while ((match + LZ4_DISTANCE_MAX < ip)
                           || (LZ4_read32(match) != LZ4_read32(ip)));

                }
                else
                {   /* byU32, byU16 */

                    BYTE* forwardIp = ip;
                    int step = 1;
                    int searchMatchNb = acceleration << (int)LZ4_skipTrigger;
                    do
                    {
                        U32 h = forwardH;
                        U32 current = (U32)(forwardIp - @base);
                        U32 matchIndex = LZ4_getIndexOnHash(h, cctx->hashTable, tableType);
                        assert(matchIndex <= current);
                        assert(forwardIp - @base < (2 * GB - 1));
                        ip = forwardIp;
                        forwardIp += step;
                        step = (searchMatchNb++ >> (int)LZ4_skipTrigger);

                        if ((forwardIp > mflimitPlusOne)) goto _last_literals;
                        assert(ip < mflimitPlusOne);

                        if (dictDirective == usingDictCtx)
                        {
                            if (matchIndex < startIndex)
                            {
                                /* there was no match, try the dictionary */
                                assert(tableType == byU32);
                                matchIndex = LZ4_getIndexOnHash(h, dictCtx->hashTable, byU32);
                                match = dictBase + matchIndex;
                                matchIndex += dictDelta;   /* make dictCtx index comparable with current context */
                                lowLimit = dictionary;
                            }
                            else
                            {
                                match = @base + matchIndex;
                                lowLimit = (BYTE*)source;
                            }
                        }
                        else if (dictDirective == usingExtDict)
                        {
                            if (matchIndex < startIndex)
                            {
                                DEBUGLOG(7, "extDict candidate: matchIndex={0}  <  startIndex={1}", matchIndex, startIndex);
                                assert(startIndex - matchIndex >= MINMATCH);
                                match = dictBase + matchIndex;
                                lowLimit = dictionary;
                            }
                            else
                            {
                                match = @base + matchIndex;
                                lowLimit = (BYTE*)source;
                            }
                        }
                        else
                        {   /* single continuous memory segment */
                            match = @base + matchIndex;
                        }
                        forwardH = LZ4_hashPosition(forwardIp, tableType);
                        LZ4_putIndexOnHash(current, h, cctx->hashTable, tableType);

                        DEBUGLOG(7, "candidate at pos={0}  (offset={1}", matchIndex, current - matchIndex);
                        if ((dictIssue == dictSmall) && (matchIndex < prefixIdxLimit)) { continue; }    /* match outside of valid area */
                        assert(matchIndex < current);
                        if (((tableType != byU16) || (LZ4_DISTANCE_MAX < LZ4_DISTANCE_ABSOLUTE_MAX))
                          && (matchIndex + LZ4_DISTANCE_MAX < current))
                        {
                            continue;
                        } /* too far */
                        assert((current - matchIndex) <= LZ4_DISTANCE_MAX);  /* match now expected within distance */

                        if (LZ4_read32(match) == LZ4_read32(ip))
                        {
                            if (maybe_extMem) offset = current - matchIndex;
                            break;   /* match found */
                        }

                    } while (true);
                }

                /* Catch up */
                filledIp = ip;
                while (((ip > anchor) & (match > lowLimit)) && ((ip[-1] == match[-1]))) { ip--; match--; }

                /* Encode Literals */
                {
                    var litLength = (uint)(ip - anchor);
                    token = op++;
                    if ((outputDirective == limitedOutput) &&  /* Check output buffer overflow */
                        ((op + litLength + (2 + 1 + LASTLITERALS) + (litLength / 255) > olimit)))
                    {
                        return 0;   /* cannot compress within `dst` budget. Stored indexes in hash table are nonetheless fine */
                    }
                    if ((outputDirective == fillOutput) &&
                        ((op + (litLength + 240) / 255 /* litlen */ + litLength /* literals */ + 2 /* offset */ + 1 /* token */ + MFLIMIT - MINMATCH /* min last literals so last match is <= end - MFLIMIT */ > olimit)))
                    {
                        op--;
                        goto _last_literals;
                    }
                    if (litLength >= RUN_MASK)
                    {
                        int len = (int)(litLength - RUN_MASK);
                        *token = ((int)RUN_MASK << ML_BITS);
                        for (; len >= 255; len -= 255) *op++ = 255;
                        *op++ = (BYTE)len;
                    }
                    else *token = (BYTE)(litLength << ML_BITS);

                    /* Copy Literals */
                    LZ4_wildCopy8(op, anchor, op + litLength);
                    op += litLength;
                    DEBUGLOG(6, "seq.start:{0}, literals={1}, match.start:{2}", (int)(anchor - (BYTE*)source), litLength, (int)(ip - (BYTE*)source));
                }

_next_match:
/* at this stage, the following variables must be correctly set :
 * - ip : at start of LZ operation
 * - match : at start of previous pattern occurence; can be within current prefix, or within extDict
 * - offset : if maybe_ext_memSegment==1 (constant)
 * - lowLimit : must be == dictionary to mean "match is within extDict"; must be == source otherwise
 * - token and *token : position to write 4-bits for match length; higher 4-bits for literal length supposed already written
 */

                if ((outputDirective == fillOutput) &&
                    (op + 2 /* offset */ + 1 /* token */ + MFLIMIT - MINMATCH /* min last literals so last match is <= end - MFLIMIT */ > olimit))
                {
                    /* the match was too close to the end, rewind and go to last literals */
                    op = token;
                    goto _last_literals;
                }

                /* Encode Offset */
                if (maybe_extMem)
                {   /* static test */
                    DEBUGLOG(6, "             with offset={0}  (ext if > {1})", offset, (int)(ip - (BYTE*)source));
                    assert(offset <= LZ4_DISTANCE_MAX && offset > 0);
                    LZ4_writeLE16(op, (U16)offset); op += 2;
                }
                else
                {
                    DEBUGLOG(6, "             with offset={0}  (same segment)", (U32)(ip - match));
                    assert(ip - match <= LZ4_DISTANCE_MAX);
                    LZ4_writeLE16(op, (U16)(ip - match)); op += 2;
                }

                /* Encode MatchLength */
                {
                    uint matchCode;

                    if ((dictDirective == usingExtDict || dictDirective == usingDictCtx)
                      && (lowLimit == dictionary) /* match within extDict */ )
                    {
                        BYTE* limit = ip + (dictEnd - match);
                        assert(dictEnd > match);
                        if (limit > matchlimit) limit = matchlimit;
                        matchCode = LZ4_count(ip + MINMATCH, match + MINMATCH, limit);
                        ip += matchCode + MINMATCH;
                        if (ip == limit)
                        {
                            var more = LZ4_count(limit, (BYTE*)source, matchlimit);
                            matchCode += more;
                            ip += more;
                        }
                        DEBUGLOG(6, "             with matchLength={0} starting in extDict", matchCode + MINMATCH);
                    }
                    else
                    {
                        matchCode = LZ4_count(ip + MINMATCH, match + MINMATCH, matchlimit);
                        ip += matchCode + MINMATCH;
                        DEBUGLOG(6, "             with matchLength={0}", matchCode + MINMATCH);
                    }

                    if ((outputDirective == limitedOutput) &&    /* Check output buffer overflow */
                        ((op + (1 + LASTLITERALS) + (matchCode + 240) / 255 > olimit)))
                    {
                        if (outputDirective == fillOutput)
                        {
                            /* Match description too long : reduce it */
                            U32 newMatchCode = 15 /* in token */ - 1 /* to avoid needing a zero byte */ + ((U32)(olimit - op) - 1 - LASTLITERALS) * 255;
                            ip -= matchCode - newMatchCode;
                            assert(newMatchCode < matchCode);
                            matchCode = newMatchCode;
                            if ((ip <= filledIp))
                            {
                                /* We have already filled up to filledIp so if ip ends up less than filledIp
                                 * we have positions in the hash table beyond the current position. This is
                                 * a problem if we reuse the hash table. So we have to remove these positions
                                 * from the hash table.
                                 */
                                BYTE* ptr;
                                DEBUGLOG(5, "Clearing {0} positions", (U32)(filledIp - ip));
                                for (ptr = ip; ptr <= filledIp; ++ptr)
                                {
                                    U32 h = LZ4_hashPosition(ptr, tableType);
                                    LZ4_clearHash(h, cctx->hashTable, tableType);
                                }
                            }
                        }
                        else
                        {
                            assert(outputDirective == limitedOutput);
                            return 0;   /* cannot compress within `dst` budget. Stored indexes in hash table are nonetheless fine */
                        }
                    }
                    if (matchCode >= ML_MASK)
                    {
                        *token += (int)ML_MASK;
                        matchCode -= ML_MASK;
                        LZ4_write32(op, 0xFFFFFFFF);
                        while (matchCode >= 4 * 255)
                        {
                            op += 4;
                            LZ4_write32(op, 0xFFFFFFFF);
                            matchCode -= 4 * 255;
                        }
                        op += matchCode / 255;
                        *op++ = (BYTE)(matchCode % 255);
                    }
                    else
                        *token += (BYTE)(matchCode);
                }
                /* Ensure we have enough space for the last literals. */
                assert(!(outputDirective == fillOutput && op + 1 + LASTLITERALS > olimit));

                anchor = ip;

                /* Test end of chunk */
                if (ip >= mflimitPlusOne) break;

                /* Fill table */
                LZ4_putPosition(ip - 2, cctx->hashTable, tableType, @base);

                /* Test next position */
                if (tableType == byPtr)
                {
                    match = LZ4_getPosition(ip, cctx->hashTable, tableType, @base);
                    LZ4_putPosition(ip, cctx->hashTable, tableType, @base);
                    if ((match + LZ4_DISTANCE_MAX >= ip)
                      && (LZ4_read32(match) == LZ4_read32(ip)))
                    { token = op++; *token = 0; goto _next_match; }
                }
                else
                {   /* byU32, byU16 */

                    U32 h = LZ4_hashPosition(ip, tableType);
                    U32 current = (U32)(ip - @base);
                    U32 matchIndex = LZ4_getIndexOnHash(h, cctx->hashTable, tableType);
                    assert(matchIndex < current);
                    if (dictDirective == usingDictCtx)
                    {
                        if (matchIndex < startIndex)
                        {
                            /* there was no match, try the dictionary */
                            matchIndex = LZ4_getIndexOnHash(h, dictCtx->hashTable, byU32);
                            match = dictBase + matchIndex;
                            lowLimit = dictionary;   /* required for match length counter */
                            matchIndex += dictDelta;
                        }
                        else
                        {
                            match = @base + matchIndex;
                            lowLimit = (BYTE*)source;  /* required for match length counter */
                        }
                    }
                    else if (dictDirective == usingExtDict)
                    {
                        if (matchIndex < startIndex)
                        {
                            match = dictBase + matchIndex;
                            lowLimit = dictionary;   /* required for match length counter */
                        }
                        else
                        {
                            match = @base + matchIndex;
                            lowLimit = (BYTE*)source;   /* required for match length counter */
                        }
                    }
                    else
                    {   /* single memory segment */
                        match = @base + matchIndex;
                    }
                    LZ4_putIndexOnHash(current, h, cctx->hashTable, tableType);
                    assert(matchIndex < current);
                    if (((dictIssue == dictSmall) ? (matchIndex >= prefixIdxLimit) : true)
                      && (((tableType == byU16) && (LZ4_DISTANCE_MAX == LZ4_DISTANCE_ABSOLUTE_MAX)) ? true : (matchIndex + LZ4_DISTANCE_MAX >= current))
                      && (LZ4_read32(match) == LZ4_read32(ip)))
                    {
                        token = op++;
                        *token = 0;
                        if (maybe_extMem) offset = current - matchIndex;
                        DEBUGLOG(6, "seq.start:{0}, literals={1}, match.start:{2}", (int)(anchor - (BYTE*)source), 0, (int)(ip - (BYTE*)source));
                        goto _next_match;
                    }
                }

                /* Prepare next loop */
                forwardH = LZ4_hashPosition(++ip, tableType);
            }

_last_literals:
/* Encode Last Literals */
            {
                var lastRun = (iend - anchor);
                if (((int)outputDirective != 0) &&  /* Check output buffer overflow */
                    (op + lastRun + 1 + ((lastRun + 255 - RUN_MASK) / 255) > olimit))
                {
                    if (outputDirective == fillOutput)
                    {
                        /* adapt lastRun to fill 'dst' */
                        assert(olimit >= op);
                        lastRun = (olimit - op) - 1;
                        lastRun -= (lastRun + 240) / 255;
                    }
                    else
                    {
                        assert(outputDirective == limitedOutput);
                        return 0;   /* cannot compress within `dst` budget. Stored indexes in hash table are nonetheless fine */
                    }
                }
                if (lastRun >= RUN_MASK)
                {
                    var accumulator = lastRun - RUN_MASK;
                    *op++ = (int)RUN_MASK << ML_BITS;
                    for (; accumulator >= 255; accumulator -= 255) *op++ = 255;
                    *op++ = (BYTE)accumulator;
                }
                else
                {
                    *op++ = (BYTE)(lastRun << ML_BITS);
                }
                memcpy(op, anchor, lastRun);
                ip = anchor + lastRun;
                op += lastRun;
            }

            if (outputDirective == fillOutput)
            {
                *inputConsumed = (int)(((byte*)ip) - source);
            }
            DEBUGLOG(5, "LZ4_compress_generic: compressed {0} bytes into {1} bytes", inputSize, (int)(((byte*)op) - dest));
            result = (int)(((byte*)op) - dest);
            assert(result > 0);
            return result;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int LZ4_compressBound(int isize)
        {
            return isize > LZ4_MAX_INPUT_SIZE ? 0 : isize + (isize / 255) + 16;
        }

        private unsafe struct LZ4_stream_t_internal
        {
            public fixed int hashTable[LZ4_HASH_SIZE_U32];
            public uint currentOffset;
            public ushort dirty;
            public ushort tableType;
            public byte* dictionary;
            public LZ4_stream_t_internal* dictCtx;
            public uint dictSize;
        }
    }
}
