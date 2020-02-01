using System;

#if Debug
using System;
using System.Text;
#endif

namespace Minotaur.Codecs
{
    public static unsafe class Codec
    {
        public const int MAX_INT32_LENGTH = 5;
        public const int MAX_UINT32_LENGTH = 5;
        public const int MAX_INT64_LENGTH = 9;
        public const int MAX_UINT64_LENGTH = 9;
        public const int MAX_DOUBLE_LENGTH = 8;

        #region UInt64

        public static int BytesCount(ulong value)
        {
            if (value < 32UL) return 1;
            if (value < 8192UL) return 2;
            if (value < 2097152UL) return 3;
            if (value < 536870912UL) return 4;
            if (value < 137438953472UL) return 5;
            if (value < 35184372088832UL) return 6;
            return value < 9007199254740992UL ? 7 : MAX_UINT64_LENGTH;
        }

        public static int EncodeUInt64(ulong value, ref byte* pTo)
        {
            if (value < 32UL) // 5 bits (1 byte >> 3)
            {
                *pTo++ = (byte)((0 << 5) | value);
#if Debug
                countU64[0]++;
#endif
                return 1;
            }
            if (value < 8192UL) // 13 bits (2 bytes >> 3)
            {
                *pTo++ = (byte)((1 << 5) | ((value & 0x1f00) >> 8));
                *pTo++ = (byte)(value & 0xff);
#if Debug
                countU64[1]++;
#endif
                return 2;
            }
            if (value < 2097152UL) // 21 bits (3 bytes >> 3)
            {
                *pTo++ = (byte)(2 << 5 | ((value & 0x1f0000) >> 16));
                *pTo++ = (byte)((value & 0xff00) >> 8);
                *pTo++ = (byte)(value & 0xff);
#if Debug
                countU64[2]++;
#endif
                return 3;
            }
            if (value < 536870912UL) // 29 bits (4 bytes >> 3)
            {
                *pTo++ = (byte)(3 << 5 | ((value & 0x1f000000) >> 24));
                *pTo++ = (byte)((value & 0xff0000) >> 16);
                *pTo++ = (byte)((value & 0xff00) >> 8);
                *pTo++ = (byte)(value & 0xff);
#if Debug
                countU64[3]++;
#endif
                return 4;
            }
            if (value < 137438953472UL) // 37 bits (5 bytes >> 3)
            {
                *pTo++ = (byte)(4 << 5 | ((value & 0x1f00000000) >> 32));
                *pTo++ = (byte)((value & 0xff000000) >> 24);
                *pTo++ = (byte)((value & 0xff0000) >> 16);
                *pTo++ = (byte)((value & 0xff00) >> 8);
                *pTo++ = (byte)(value & 0xff);
#if Debug
                countU64[4]++;
#endif
                return 5;
            }
            if (value < 35184372088832UL) // 45 bits (6 bytes >> 3)
            {
                *pTo++ = (byte)(5 << 5 | ((value & 0x1f0000000000) >> 40));
                *pTo++ = (byte)((value & 0xff00000000) >> 32);
                *pTo++ = (byte)((value & 0xff000000) >> 24);
                *pTo++ = (byte)((value & 0xff0000) >> 16);
                *pTo++ = (byte)((value & 0xff00) >> 8);
                *pTo++ = (byte)(value & 0xff);
#if Debug
                countU64[5]++;
#endif
                return 6;
            }
            if (value < 9007199254740992UL) // 53 bits (7 bytes >> 3)
            {
                *pTo++ = (byte)(6 << 5 | ((value & 0x1f000000000000) >> 48));
                *pTo++ = (byte)((value & 0xff0000000000) >> 40);
                *pTo++ = (byte)((value & 0xff00000000) >> 32);
                *pTo++ = (byte)((value & 0xff000000) >> 24);
                *pTo++ = (byte)((value & 0xff0000) >> 16);
                *pTo++ = (byte)((value & 0xff00) >> 8);
                *pTo++ = (byte)(value & 0xff);
#if Debug
                countU64[6]++;
#endif
                return 7;
            }

            // 64 bits (8 bytes full)
            *pTo++ = 7 << 5;
            *pTo++ = (byte)((value & 0xff00000000000000) >> 56);
            *pTo++ = (byte)((value & 0xff000000000000) >> 48);
            *pTo++ = (byte)((value & 0xff0000000000) >> 40);
            *pTo++ = (byte)((value & 0xff00000000) >> 32);
            *pTo++ = (byte)((value & 0xff000000) >> 24);
            *pTo++ = (byte)((value & 0xff0000) >> 16);
            *pTo++ = (byte)((value & 0xff00) >> 8);
            *pTo++ = (byte)(value & 0xff);
#if Debug
            countU64[8]++;
#endif
            return MAX_UINT64_LENGTH;
        }

        public static ulong DecodeUInt64(ref byte* pFrom)
        {
            var length = (*pFrom & 0xE0) >> 5;

            if (length == 0)
                return (ulong)(*pFrom++ & 0x1F);

            if (length == 1)
                return (ulong)(((*pFrom++ & 0x1F) << 8) | *pFrom++);

            if (length == 2)
                return (ulong)(((*pFrom++ & 0x1F) << 16) | *pFrom++ << 8 | *pFrom++);

            if (length == 3)
                return (ulong)(((*pFrom++ & 0x1F) << 24) | *pFrom++ << 16 | *pFrom++ << 8 | *pFrom++);

            if (length == 4)
            {
                var i1 = *pFrom++ & 0x1F;
                var i2 = *pFrom++ << 24 | *pFrom++ << 16 | *pFrom++ << 8 | *pFrom++;
                return ((ulong)i1 << 32) | (uint)i2;
            }

            if (length == 5)
            {
                var i1 = (*pFrom++ & 0x1F) << 8 | *pFrom++;
                var i2 = *pFrom++ << 24 | *pFrom++ << 16 | *pFrom++ << 8 | *pFrom++;
                return ((ulong)i1 << 32) | (uint)i2;
            }

            if (length == 6)
            {
                var i1 = (*pFrom++ & 0x1F) << 16 | *pFrom++ << 8 | *pFrom++;
                var i2 = *pFrom++ << 24 | *pFrom++ << 16 | *pFrom++ << 8 | *pFrom++;
                return ((ulong)i1 << 32) | (uint)i2;
            }
            else
            {
                pFrom++;
                var i1 = *pFrom++ << 24 | *pFrom++ << 16 | *pFrom++ << 8 | *pFrom++;
                var i2 = *pFrom++ << 24 | *pFrom++ << 16 | *pFrom++ << 8 | *pFrom++;
                return ((ulong)i1 << 32) | (uint)i2;
            }
        }

        #endregion

        #region Int64

        public static int BytesCount(long value)
        {
            if (value == long.MinValue) return 9;
            var v = (ulong)(value < 0 ? -value : value);
            if (v < 16UL) return 1;
            if (v < 4096UL) return 2;
            if (v < 1048576UL) return 3;
            if (v < 268435456UL) return 4;
            if (v < 68719476736UL) return 5;
            if (v < 17592186044416UL) return 6;
            return v < 4503599627370496UL ? 7 : MAX_INT64_LENGTH;
        }

        public static int EncodeInt64(long value, ref byte* pTo)
        {
            var sign = 0;
            var uvalue = (ulong)value;
            if (value < 0)
            {
                sign = 1;
                if (value != long.MinValue)
                    uvalue = (ulong)-value;
                else
                {
                    *pTo++ = (byte)((sign << 7) | (7 << 4));
                    *pTo++ = (byte)((uvalue & 0xff00000000000000) >> 56);
                    *pTo++ = (byte)((uvalue & 0xff000000000000) >> 48);
                    *pTo++ = (byte)((uvalue & 0xff0000000000) >> 40);
                    *pTo++ = (byte)((uvalue & 0xff00000000) >> 32);
                    *pTo++ = (byte)((uvalue & 0xff000000) >> 24);
                    *pTo++ = (byte)((uvalue & 0xff0000) >> 16);
                    *pTo++ = (byte)((uvalue & 0xff00) >> 8);
                    *pTo++ = (byte)(uvalue & 0xff);
#if Debug
                    count64[8]++;
#endif
                    return MAX_INT64_LENGTH;
                }
            }

            if (uvalue < 16UL) // 4 bits (1 byte >> 4)
            {
                *pTo++ = (byte)((sign << 7) | (byte)(uvalue & 0xf));
#if Debug
                count64[0]++;
#endif
                return 1;
            }
            if (uvalue < 4096UL) // 12 bits (2 bytes >> 4)
            {
                *pTo++ = (byte)((sign << 7) | (1 << 4) | (byte)((uvalue & 0xf00) >> 8));
                *pTo++ = (byte)(uvalue & 0xff);
#if Debug
                count64[1]++;
#endif
                return 2;
            }
            if (uvalue < 1048576UL) // 20 bits (3 bytes >> 4)
            {
                *pTo++ = (byte)((sign << 7) | (2 << 4) | (byte)((uvalue & 0xf0000) >> 16));
                *pTo++ = (byte)((uvalue & 0xff00) >> 8);
                *pTo++ = (byte)(uvalue & 0xff);
#if Debug
                count64[2]++;
#endif
                return 3;
            }
            if (uvalue < 268435456UL) // 28 bits (4 bytes >> 4)
            {
                *pTo++ = (byte)((sign << 7) | (3 << 4) | (byte)((uvalue & 0xf000000) >> 24));
                *pTo++ = (byte)((uvalue & 0xff0000) >> 16);
                *pTo++ = (byte)((uvalue & 0xff00) >> 8);
                *pTo++ = (byte)(uvalue & 0xff);
#if Debug
                count64[3]++;
#endif
                return 4;
            }
            if (uvalue < 68719476736UL) // 36 bits (5 bytes >> 4)
            {
                *pTo++ = (byte)((sign << 7) | (4 << 4) | (byte)((uvalue & 0xf00000000) >> 32));
                *pTo++ = (byte)((uvalue & 0xff000000) >> 24);
                *pTo++ = (byte)((uvalue & 0xff0000) >> 16);
                *pTo++ = (byte)((uvalue & 0xff00) >> 8);
                *pTo++ = (byte)(uvalue & 0xff);
#if Debug
                count64[4]++;
#endif
                return 5;
            }
            if (uvalue < 17592186044416UL) // 44 bits (6 bytes >> 4)
            {
                *pTo++ = (byte)((sign << 7) | (5 << 4) | (byte)((uvalue & 0xf0000000000) >> 40));
                *pTo++ = (byte)((uvalue & 0xff00000000) >> 32);
                *pTo++ = (byte)((uvalue & 0xff000000) >> 24);
                *pTo++ = (byte)((uvalue & 0xff0000) >> 16);
                *pTo++ = (byte)((uvalue & 0xff00) >> 8);
                *pTo++ = (byte)(uvalue & 0xff);
#if Debug
                count64[5]++;
#endif
                return 6;
            }
            if (uvalue < 4503599627370496UL) // 52 bits (7 bytes >> 4)
            {
                *pTo++ = (byte)((sign << 7) | (6 << 4) | (byte)((uvalue & 0xf000000000000) >> 48));
                *pTo++ = (byte)((uvalue & 0xff0000000000) >> 40);
                *pTo++ = (byte)((uvalue & 0xff00000000) >> 32);
                *pTo++ = (byte)((uvalue & 0xff000000) >> 24);
                *pTo++ = (byte)((uvalue & 0xff0000) >> 16);
                *pTo++ = (byte)((uvalue & 0xff00) >> 8);
                *pTo++ = (byte)(uvalue & 0xff);
#if Debug
                count64[6]++;
#endif
                return 7;
            }

            *pTo++ = (byte)((sign << 7) | (7 << 4));
            *pTo++ = (byte)((uvalue & 0xff00000000000000) >> 56);
            *pTo++ = (byte)((uvalue & 0xff000000000000) >> 48);
            *pTo++ = (byte)((uvalue & 0xff0000000000) >> 40);
            *pTo++ = (byte)((uvalue & 0xff00000000) >> 32);
            *pTo++ = (byte)((uvalue & 0xff000000) >> 24);
            *pTo++ = (byte)((uvalue & 0xff0000) >> 16);
            *pTo++ = (byte)((uvalue & 0xff00) >> 8);
            *pTo++ = (byte)(uvalue & 0xff);
#if Debug
            count64[8]++;
#endif
            return MAX_INT64_LENGTH;
        }

        public static long DecodeInt64(ref byte* pFrom)
        {
            var sign = (*pFrom & 0x80) == 0x80 ? -1 : 1;
            var length = (*pFrom & 0x70) >> 4;

            switch (length)
            {
                case 0:
                    return sign * (*pFrom++ & 0xf);
                case 1:
                    return sign * (((*pFrom++ & 0xf) << 8) | *pFrom++);
                case 2:
                    return sign * (((*pFrom++ & 0xf) << 16) | (*pFrom++ << 8) | *pFrom++);
                case 3:
                    return sign * (((*pFrom++ & 0xf) << 24) | (*pFrom++ << 16) | (*pFrom++ << 8) | *pFrom++);
                case 4:
                    {
                        var i1 = *pFrom++ & 0xf;
                        var i2 = (*pFrom++ << 24) | (*pFrom++ << 16) | (*pFrom++ << 8) | *pFrom++;
                        return sign * (long)(((ulong)i1 << 32) | (uint)i2);
                    }
                case 5:
                    {
                        var i1 = ((*pFrom++ & 0xf) << 8) | *pFrom++;
                        var i2 = (*pFrom++ << 24) | (*pFrom++ << 16) | (*pFrom++ << 8) | *pFrom++;
                        return sign * (long)(((ulong)i1 << 32) | (uint)i2);
                    }
                case 6:
                    {
                        var i1 = ((*pFrom++ & 0xf) << 16) | (*pFrom++ << 8) | *pFrom++;
                        var i2 = (*pFrom++ << 24) | (*pFrom++ << 16) | (*pFrom++ << 8) | *pFrom++;
                        return sign * (long)(((ulong)i1 << 32) | (uint)i2);
                    }
                default:
                    {
                        pFrom++;
                        var i1 = (*pFrom++ << 24) | (*pFrom++ << 16) | (*pFrom++ << 8) | *pFrom++;
                        var i2 = (*pFrom++ << 24) | (*pFrom++ << 16) | (*pFrom++ << 8) | *pFrom++;
                        return sign * (long)(((ulong)i1 << 32) | (uint)i2);
                    }
            }
        }

        /// <summary>
        ///
        ///    Bits  | Description
        /// ---------|------------------------
        ///     1    | Flag value
        /// ---------|------------------------
        ///     2    | Sign value
        /// ---------|------------------------
        /// [ 3;  5] | Length of encoded value
        /// ---------|------------------------
        /// [ 6; 72] | Encoded value. The right bound depends of the length
        /// 
        /// </summary>
        /// <param name="flag"></param>
        /// <param name="value"></param>
        /// <param name="pTo"></param>
        /// <returns></returns>
        public static int EncodeInt64(bool flag, long value, ref byte* pTo)
        {
            var f = flag ? 1 : 0;
            var sign = 0;
            var uvalue = (ulong)value;
            if (value < 0)
            {
                sign = 1;
                if (value != long.MinValue)
                    uvalue = (ulong)-value;
                else
                {
                    *pTo++ = (byte)((f << 7) | (sign << 6) | (7 << 3));
                    *(long*) pTo = value;
                    pTo += sizeof(long);
#if Debug
                    count32[4]++;
#endif
                    return MAX_INT64_LENGTH;
                }
            }

            if (uvalue < 8UL) // 3 bits to encode
            {
                *pTo++ = (byte)((f << 7) | (sign << 6) | (byte)(uvalue & 0x7));
#if Debug
                count64[0]++;
#endif
                return 1;
            }
            if (uvalue < 2048UL) // 11 bits to encode
            {
                *pTo++ = (byte)((f << 7) | (sign << 6) | (1 << 3) | (byte)((uvalue & 0x700) >> 8));
                *pTo++ = (byte)(uvalue & 0xff);
#if Debug
                count64[1]++;
#endif
                return 2;
            }
            if (uvalue < 524287UL) // 19 bits to encode
            {
                *pTo++ = (byte)((f << 7) | (sign << 6) | (2 << 3) | (byte)((uvalue & 0x70000) >> 16));
                *pTo++ = (byte)((uvalue & 0xff00) >> 8);
                *pTo++ = (byte)(uvalue & 0xff);
#if Debug
                count64[2]++;
#endif
                return 3;
            }
            if (uvalue < 134217728UL) // 27 bits to encode
            {
                *pTo++ = (byte)((f << 7) | (sign << 6) | (3 << 3) | (byte)((uvalue & 0x7000000) >> 24));
                *pTo++ = (byte)((uvalue & 0xff0000) >> 16);
                *pTo++ = (byte)((uvalue & 0xff00) >> 8);
                *pTo++ = (byte)(uvalue & 0xff);
#if Debug
                count64[3]++;
#endif
                return 4;
            }
            if (uvalue < 34359738368UL) // 31 bits to encode
            {
                *pTo++ = (byte)((f << 7) | (sign << 6) | (4 << 3) | (byte)((uvalue & 0x700000000) >> 32));
                *pTo++ = (byte)((uvalue & 0xff000000) >> 24);
                *pTo++ = (byte)((uvalue & 0xff0000) >> 16);
                *pTo++ = (byte)((uvalue & 0xff00) >> 8);
                *pTo++ = (byte)(uvalue & 0xff);
#if Debug
                count64[4]++;
#endif
                return 5;
            }
            if (uvalue < 8796093022207UL) // 39 bits to encode
            {
                *pTo++ = (byte)((f << 7) | (sign << 6) | (5 << 3) | (byte)((uvalue & 0x70000000000) >> 40));
                *pTo++ = (byte)((uvalue & 0xff00000000) >> 32);
                *pTo++ = (byte)((uvalue & 0xff000000) >> 24);
                *pTo++ = (byte)((uvalue & 0xff0000) >> 16);
                *pTo++ = (byte)((uvalue & 0xff00) >> 8);
                *pTo++ = (byte)(uvalue & 0xff);
#if Debug
                count64[5]++;
#endif
                return 6;
            }
            if (uvalue < 2251799813685247UL) // 47 bits to encode
            {
                *pTo++ = (byte)((f << 7) | (sign << 6) | (6 << 3) | (byte)((uvalue & 0x7000000000000) >> 48));
                *pTo++ = (byte)((uvalue & 0xff0000000000) >> 40);
                *pTo++ = (byte)((uvalue & 0xff00000000) >> 32);
                *pTo++ = (byte)((uvalue & 0xff000000) >> 24);
                *pTo++ = (byte)((uvalue & 0xff0000) >> 16);
                *pTo++ = (byte)((uvalue & 0xff00) >> 8);
                *pTo++ = (byte)(uvalue & 0xff);
#if Debug
                count64[6]++;
#endif
                return 7;
            }
            
            // 64 bits full 9 bytes size
            *pTo++ = (byte)((f << 7) | (sign << 6) | (7 << 3));
            *(long*) pTo = value;
            pTo += sizeof(long);
#if Debug
            count64[9]++;
#endif
            return MAX_INT64_LENGTH;
        }

        public static long DecodeInt64(ref byte* pFrom, out bool flag)
        {
            flag = (*pFrom & 0x80) == 0x80;
            var sign = (*pFrom & 0x40) == 0x40 ? -1 : 1;
            var length = (*pFrom & 0x38) >> 3;

            switch (length)
            {
                case 0:
                    return sign * (*pFrom++ & 0x7);
                case 1:
                    return sign * (((*pFrom++ & 0x7) << 8) | *pFrom++);
                case 2:
                    return sign * (((*pFrom++ & 0x7) << 16) | *pFrom++ << 8 | *pFrom++);
                case 3:
                    return sign * (((*pFrom++ & 0x7) << 24) | *pFrom++ << 16 | *pFrom++ << 8 | *pFrom++);
                case 4:
                {
                    var i1 = *pFrom++ & 0x7;
                    var i2 = (*pFrom++ << 24) | (*pFrom++ << 16) | (*pFrom++ << 8) | *pFrom++;
                    return sign * (long)(((ulong)i1 << 32) | (uint)i2);
                }
                case 5:
                {
                    var i1 = ((*pFrom++ & 0x7) << 8) | *pFrom++;
                    var i2 = (*pFrom++ << 24) | (*pFrom++ << 16) | (*pFrom++ << 8) | *pFrom++;
                    return sign * (long)(((ulong)i1 << 32) | (uint)i2);
                }
                case 6:
                {
                    var i1 = ((*pFrom++ & 0x7) << 16) | (*pFrom++ << 8) | *pFrom++;
                    var i2 = (*pFrom++ << 24) | (*pFrom++ << 16) | (*pFrom++ << 8) | *pFrom++;
                    return sign * (long)(((ulong)i1 << 32) | (uint)i2);
                }
                default:
                    pFrom++;
                    var value = *(long*) pFrom;
                    pFrom += sizeof(long);
                    return value;
            }
        }

        #endregion

        #region UInt32

        public static int BytesCount(uint value)
        {
            if (value < 64U) return 1;
            if (value < 16384U) return 2;
            return value < 4194304U ? 3 : MAX_UINT32_LENGTH;
        }

        /// <summary>
        /// --------|-----------------
        /// 2 bits  | Encoding length
        ///         | 00 : 1 Byte
        ///         | 01 : 2 Byte
        ///         | 10 : 3 Byte
        ///         | 11 : 5 Byte
        /// --------|-----------------
        /// 32 bits | Data
        /// --------------------------
        ///
        /// | Length | Mask Bits | Hex |
        /// |--------|-----------|-----|  
        /// |   1    | 1100 0000 | C0  |
        /// |--------|-----------|-----|  
        /// |   2    | 1100 0000 | C0  |
        /// |--------|-----------|-----|  
        /// |   3    | 1100 0000 | C0  |
        /// |--------|-----------|-----|  
        /// |   4    | 1100 0000 | C0  |
        /// 
        /// Max value
        /// | Length |    Bits   | Hex | Dec           |
        /// |--------|-----------|-----|---------------|
        /// |   1    | 0011 1111 | 3F  | 63            |
        /// |--------|-----------|-----|---------------|
        /// |   2    | 0011 1111 | 3F  | 16 383        |
        /// |        | 1111 1111 | FF  |               |
        /// |--------|-----------|-----|---------------|
        /// |   3    | 0011 1111 | 3F  | 4 194 303     |
        /// |        | 1111 1111 | FF  |               |
        /// |        | 1111 1111 | FF  |               |
        /// |--------|-----------|-----|---------------|
        /// |   4    | 0000 0000 | 00  | 4 294 967 295 |
        /// |        | 1111 1111 | FF  |               |
        /// |        | 1111 1111 | FF  |               |
        /// |        | 1111 1111 | FF  |               |
        /// |        | 1111 1111 | FF  |               |
        /// |--------|-----------|-----|---------------|
        /// </summary>
        public static int EncodeUInt32(uint value, ref byte* pTo)
        {
            if (value < 64U) // 6 bits (1 byte >> 2)
            {
                *pTo++ = (byte)(value & 0x3f);
#if Debug
                countU32[0]++;
#endif
                return 1;
            }
            if (value < 16384U) // 14 bits (2 bytes >> 2)
            {
                *pTo++ = (byte)((1 << 6) | ((value & 0x3f00) >> 8));
                *pTo++ = (byte)(value & 0xff);
#if Debug
                countU32[1]++;
#endif
                return 2;
            }
            if (value < 4194304U) // 22 bits (3 bytes >> 2)
            {
                *pTo++ = (byte)(2 << 6 | ((value & 0x3f0000) >> 16));
                *pTo++ = (byte)((value & 0xff00) >> 8);
                *pTo++ = (byte)(value & 0xff);
#if Debug
                countU32[2]++;
#endif
                return 3;
            }

            // 32 bits full 4 bytes size
            *pTo++ = 3 << 6;
            *pTo++ = (byte)((value & 0xff000000) >> 24);
            *pTo++ = (byte)((value & 0xff0000) >> 16);
            *pTo++ = (byte)((value & 0xff00) >> 8);
            *pTo++ = (byte)(value & 0xff);
#if Debug
            countU32[4]++;
#endif
            return MAX_UINT32_LENGTH;
        }

        public static uint DecodeUInt32(ref byte* pFrom)
        {
            var length = (*pFrom & 0xC0) >> 6;

            if (length == 0)
                return (uint)(*pFrom++ & 0x3F);

            if (length == 1)
                return (uint)(((*pFrom++ & 0x3F) << 8) | *pFrom++);

            if (length == 2)
                return (uint)(((*pFrom++ & 0x3F) << 16) | *pFrom++ << 8 | *pFrom++);

            pFrom++;
            return (uint)(*pFrom++ << 24 | *pFrom++ << 16 | *pFrom++ << 8 | *pFrom++);
        }

        #endregion

        #region Int32

        public static int BytesCount(int value)
        {
            if (value == int.MinValue) return 5;
            var v = (uint)(value < 0 ? -value : value);
            if (v < 32) return 1;
            if (v < 8192) return 2;
            return v < 2097152 ? 3 : MAX_INT32_LENGTH;
        }

        public static int EncodeInt32(int value, ref byte* pTo)
        {
            var sign = 0;
            if (value < 0)
            {
                sign = 1;
                if (value != int.MinValue)
                    value = -value;
                else
                {
                    *pTo++ = (byte)((sign << 7) | (3 << 5));
                    *pTo++ = (byte)((value & 0xff000000) >> 24);
                    *pTo++ = (byte)((value & 0xff0000) >> 16);
                    *pTo++ = (byte)((value & 0xff00) >> 8);
                    *pTo++ = (byte)(value & 0xff);
#if Debug
                    count32[4]++;
#endif
                    return MAX_INT32_LENGTH;
                }
            }

            if (value < 32) // 5 bits (1 byte >> 3)
            {
                *pTo++ = (byte)((sign << 7) | value & 0x1f);
#if Debug
                count32[2]++;
#endif
                return 1;
            }

            if (value < 8192) // 13 bits (2 bytes >> 3)
            {
                *pTo++ = (byte)((sign << 7) | (1 << 5) | ((value & 0x1f00) >> 8));
                *pTo++ = (byte)(value & 0xff);
#if Debug
                count32[1]++;
#endif
                return 2;
            }

            if (value < 2097152) // 21 bits (3 bytes >> 3)
            {
                *pTo++ = (byte)((sign << 7) | (2 << 5) | ((value & 0x1f0000) >> 16));
                *pTo++ = (byte)((value & 0xff00) >> 8);
                *pTo++ = (byte)(value & 0xff);
#if Debug
                count32[2]++;
#endif
                return 3;
            }

            // 32 bits full 4 bytes size
            *pTo++ = (byte)((sign << 7) | (3 << 5));
            *pTo++ = (byte)((value & 0xff000000) >> 24);
            *pTo++ = (byte)((value & 0xff0000) >> 16);
            *pTo++ = (byte)((value & 0xff00) >> 8);
            *pTo++ = (byte)(value & 0xff);
#if Debug
            count32[4]++;
#endif
            return MAX_INT32_LENGTH;
        }

        public static int DecodeInt32(ref byte* pFrom)
        {
            var sign = (*pFrom & 0x80) == 0x80 ? -1 : 1;
            var length = (*pFrom & 0x60) >> 5;

            if (length == 0)
                return sign * (*pFrom++ & 0x1F);

            if (length == 1)
                return sign * (((*pFrom++ & 0x1F) << 8) | *pFrom++);

            if (length == 2)
                return sign * (((*pFrom++ & 0x1F) << 16) | *pFrom++ << 8 | *pFrom++);

            pFrom++;
            var value = *pFrom++ << 24 | *pFrom++ << 16 | *pFrom++ << 8 | *pFrom++;
            return value == int.MinValue ? int.MinValue : sign * value;
        }

        public static int BytesCount(bool flag, int value)
        {
            if (value == int.MinValue) return 5;
            var v = (uint)(value < 0 ? -value : value);
            if (v < 16) return 1;
            if (v < 4096) return 2;
            return v < 1048576 ? 3 : MAX_INT32_LENGTH;
        }

        public static int EncodeInt32(bool flag, int value, ref byte* pTo)
        {
            var f = flag ? 1 : 0;
            var sign = 0;
            if (value < 0)
            {
                sign = 1;
                if (value != int.MinValue)
                    value = -value;
                else
                {
                    *pTo++ = (byte)((f << 7) | (sign << 6) | (3 << 4));
                    *pTo++ = (byte)((value & 0xff000000) >> 24);
                    *pTo++ = (byte)((value & 0xff0000) >> 16);
                    *pTo++ = (byte)((value & 0xff00) >> 8);
                    *pTo++ = (byte)(value & 0xff);
#if Debug
                    count32[4]++;
#endif
                    return MAX_INT32_LENGTH;
                }
            }

            if (value < 16) // 4 bits (1 byte >> 4)
            {
                *pTo++ = (byte)((f << 7) | (sign << 6) | value & 0xf);
#if Debug
                count32[0]++;
#endif
                return 0;
            }
            if (value < 4096) // 12 bits (2 bytes >> 4)
            {
                *pTo++ = (byte)((f << 7) | (sign << 6) | (1 << 4) | ((value & 0xf00) >> 8));
                *pTo++ = (byte)(value & 0xff);
#if Debug
                count32[1]++;
#endif
                return 1;
            }
            if (value < 1048576) // 20 bits (3 bytes >> 4)
            {
                *pTo++ = (byte)((f << 7) | (sign << 6) | (2 << 4) | ((value & 0xf0000) >> 16));
                *pTo++ = (byte)((value & 0xff00) >> 8);
                *pTo++ = (byte)(value & 0xff);
#if Debug
                count32[2]++;
#endif
                return 2;
            }

            // 32 bits full 4 bytes size
            *pTo++ = (byte)((f << 7) | (sign << 6) | (3 << 4));
            *pTo++ = (byte)((value & 0xff000000) >> 24);
            *pTo++ = (byte)((value & 0xff0000) >> 16);
            *pTo++ = (byte)((value & 0xff00) >> 8);
            *pTo++ = (byte)(value & 0xff);
#if Debug
            count32[4]++;
#endif
            return MAX_INT32_LENGTH;
        }

        public static int DecodeInt32(ref byte* pFrom, out bool flag)
        {
            flag = (*pFrom & 0x80) == 0x80;
            var sign = (*pFrom & 0x40) == 0x40 ? -1 : 1;
            var length = (*pFrom & 0x30) >> 4;

            switch (length)
            {
                case 0:
                    return sign * (*pFrom++ & 0xf);
                case 1:
                    return sign * (((*pFrom++ & 0xF) << 8) | *pFrom++);
                case 2:
                    return sign * (((*pFrom++ & 0xF) << 16) | *pFrom++ << 8 | *pFrom++);
            }

            pFrom++;
            var value = *pFrom++ << 24 | *pFrom++ << 16 | *pFrom++ << 8 | *pFrom++;
            return value == int.MinValue ? int.MinValue : sign * value;
        }

        #endregion

        #region Double

        public static int EncodeDouble(double value, ref byte* pTo)
        {
            *(double*)pTo = value;
            pTo += MAX_DOUBLE_LENGTH;
            return MAX_DOUBLE_LENGTH;
        }

        public static double DecodeDouble(ref byte* pFrom)
        {
            var value = *(double*)pFrom;
            pFrom += MAX_DOUBLE_LENGTH;
            return value;
        }

        #endregion

#if Debug
        private static readonly int[] countU64 = new int[9];
        public static void ResetCountersU64()
        {
            ResetCounters(countU64);
        }
        public static void WriteCountersU64()
        {
            WriteCounters("Unsigned 64Bits", countU64);
        }
        private static readonly int[] count64 = new int[9];
        public static void ResetCounters64()
        {
            ResetCounters(count64);
        }
        public static void WriteCounters64()
        {
            WriteCounters("Signed 64Bits", count64);
        }
        private static readonly int[] countU32 = new int[5];
        public static void ResetCountersU32()
        {
            ResetCounters(countU32);
        }
        public static void WriteCountersU32()
        {
            WriteCounters("Unsigned 32Bits", countU32);
        }
        private static readonly int[] count32 = new int[5];
        public static void ResetCounters32()
        {
            ResetCounters(count32);
        }
        public static void WriteCounters32()
        {
            WriteCounters("Signed 32Bits", count32);
        }
        public static void ResetAllCounters()
        {
            ResetCountersU32();
            ResetCounters32();
            ResetCountersU64();
            ResetCounters64();
        }
        public static void WriteAllCounters()
        {
            WriteCounters32();
            Console.WriteLine("=============");
            WriteCountersU32();
            Console.WriteLine("=============");
            WriteCounters64();
            Console.WriteLine("=============");
            WriteCountersU64();
        }
        private static void ResetCounters(int[] counters)
        {
            for (var i = 0; i < counters.Length; i++) counters[i] = 0;
        }
        private static void WriteCounters(string text, int[] counters)
        {
            var sb = new StringBuilder(text);
            for (var i = 0; i < counters.Length; i++)
                sb.AppendFormat("\r\n\tNb {0} byte{1} {2}", i + 1, i == 0 ? string.Empty : "s", counters[i]);
            Console.WriteLine(sb.ToString());
        }
#endif
    }

    public unsafe interface IValueTypeCodec<T> where T : unmanaged
    {
        int GetMaxEncodedSize();

        int Encode(T value, ref byte* dst);

        T Decode(ref byte* src);

        T Subtract(T x, T y);

        T Min(T x, T y);

        T Max(T x, T y);
    }

    public unsafe struct Int32Codec : IValueTypeCodec<int>
    {
        private const int MAX_ENCODED_SIZE = 5;

        #region Implementation of IValueTypeCodec<int>

        public int GetMaxEncodedSize() => MAX_ENCODED_SIZE;

        public int Encode(int value, ref byte* dst)
        {
            var sign = 0;
            if (value < 0)
            {
                sign = 1;
                if (value != int.MinValue)
                    value = -value;
                else
                {
                    *dst++ = (byte)((sign << 7) | (3 << 5));
                    *dst++ = (byte)((value & 0xff000000) >> 24);
                    *dst++ = (byte)((value & 0xff0000) >> 16);
                    *dst++ = (byte)((value & 0xff00) >> 8);
                    *dst++ = (byte)(value & 0xff);

                    return MAX_ENCODED_SIZE;
                }
            }

            if (value < 32) // 5 bits (1 byte >> 3)
            {
                *dst++ = (byte)((sign << 7) | value & 0x1f);

                return 1;
            }

            if (value < 8192) // 13 bits (2 bytes >> 3)
            {
                *dst++ = (byte)((sign << 7) | (1 << 5) | ((value & 0x1f00) >> 8));
                *dst++ = (byte)(value & 0xff);

                return 2;
            }

            if (value < 2097152) // 21 bits (3 bytes >> 3)
            {
                *dst++ = (byte)((sign << 7) | (2 << 5) | ((value & 0x1f0000) >> 16));
                *dst++ = (byte)((value & 0xff00) >> 8);
                *dst++ = (byte)(value & 0xff);

                return 3;
            }

            // 32 bits full 4 bytes size
            *dst++ = (byte)((sign << 7) | (3 << 5));
            *dst++ = (byte)((value & 0xff000000) >> 24);
            *dst++ = (byte)((value & 0xff0000) >> 16);
            *dst++ = (byte)((value & 0xff00) >> 8);
            *dst++ = (byte)(value & 0xff);

            return MAX_ENCODED_SIZE;
        }

        public int Decode(ref byte* src)
        {
            var sign = (*src & 0x80) == 0x80 ? -1 : 1;
            var length = (*src & 0x60) >> 5;

            if (length == 0)
                return sign * (*src++ & 0x1F);

            if (length == 1)
                return sign * (((*src++ & 0x1F) << 8) | *src++);

            if (length == 2)
                return sign * (((*src++ & 0x1F) << 16) | *src++ << 8 | *src++);

            src++;
            var value = *src++ << 24 | *src++ << 16 | *src++ << 8 | *src++;
            return value == int.MinValue ? int.MinValue : sign * value;
        }

        public int Subtract(int x, int y) => x - y;

        public int Min(int x, int y) => Math.Min(x, y);

        public int Max(int x, int y) => Math.Max(x, y);

        #endregion
    }
}
