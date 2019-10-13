using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using Minotaur.Core;
using Minotaur.Cursors;
using Minotaur.Native;
using Minotaur.Recorders;
using Minotaur.Streams;

namespace Minotaur.Tests
{
    public static class Extensions
    {
        #region Chunk Helpers

        #region Set

        public static DoubleEntry[] Set(this DoubleEntry[] chunk, int i, long ticks, double value)
        {
            chunk[i].ticks = ticks;
            chunk[i].value = value;
            return chunk;
        }

        public static Int32Entry[] Set(this Int32Entry[] chunk, int i, long ticks, int value)
        {
            chunk[i].ticks = ticks;
            chunk[i].value = value;
            return chunk;
        }

        public static void Set(this FloatEntry[] chunk, int i, long ticks, float value)
        {
            chunk[i].ticks = ticks;
            chunk[i].value = value;
        }

        public static void Set(this Int64Entry[] array, int i, long ticks, long value)
        {
            array[i].ticks = ticks;
            array[i].value = value;
        }

        public static void Set(this StringEntry[] array, int i, long ticks, string value)
        {
            array[i].ticks = ticks;
            array[i].SetValue(value);
        }

        #endregion

        #region Add

        public static List<DoubleEntry> Add(this List<DoubleEntry> chunk, long ticks, double value)
        {
            chunk.Add(new DoubleEntry { ticks = ticks, value = value });
            return chunk;
        }

        public static List<DoubleEntry> Add(this List<DoubleEntry> chunk, string time, double value)
        {
            return chunk.Add(time.ToDateTime().Ticks, value);
        }

        public static List<Int32Entry> Add(this List<Int32Entry> chunk, long ticks, double value)
        {
            chunk.Add(new Int32Entry { ticks = ticks, value = (int)value });
            return chunk;
        }

        public static List<Int32Entry> Add(this List<Int32Entry> chunk, string time, double value)
        {
            return chunk.Add(time.ToDateTime().Ticks, value);
        }

        public static List<DateTime> Add(this List<DateTime> chunk, string timestamp)
        {
            chunk.Add(timestamp.ToDateTime());
            return chunk;
        }

        public static DoubleEntry[] Add(this DoubleEntry[] chunk, long ticks, double value)
        {
            return new List<DoubleEntry>(chunk) { new DoubleEntry { ticks = ticks, value = value } }.ToArray();
        }

        public static Int32Entry[] Add(this Int32Entry[] chunk, long ticks, double value)
        {
            return new List<Int32Entry>(chunk) { new Int32Entry { ticks = ticks, value = (int)value } }.ToArray();
        }

        public static DateTime[] Add(this DateTime[] chunk, string timestamp)
        {
            return new List<DateTime>(chunk) { timestamp.ToDateTime() }.ToArray();
        }

        #endregion

        #region Write

        public static unsafe int Write(this IColumnStream stream, DoubleEntry[] chunk)
        {
            fixed (DoubleEntry* p = chunk)
                return stream.Write((byte*)p, chunk.Length * sizeof(DoubleEntry));
        }

        public static unsafe int Write(this IColumnStream stream, Int32Entry[] chunk)
        {
            fixed (Int32Entry* p = chunk)
                return stream.Write((byte*)p, chunk.Length * sizeof(Int32Entry));
        }

        public static unsafe int Write(this IColumnStream stream, DateTime[] chunk)
        {
            var ticks = chunk.Select(p => p.Ticks).ToArray();
            fixed (long* p = ticks)
                return stream.Write((byte*)p, chunk.Length * sizeof(long));
        }

        #endregion

        #region Converters

        public static FloatEntry ToFloat(this DoubleEntry entry)
        {
            return new FloatEntry { ticks = entry.ticks, value = (float)entry.value };
        }

        public static FloatEntry[] ToFloat(this DoubleEntry[] chunck)
        {
            return chunck.Select(ToFloat).ToArray();
        }

        public static Int32Entry ToInt32(this DoubleEntry entry)
        {
            return new Int32Entry { ticks = entry.ticks, value = (int)entry.value };
        }

        public static Int32Entry[] ToInt32(this DoubleEntry[] chunck)
        {
            return chunck.Select(ToInt32).ToArray();
        }

        public static Int64Entry ToInt64(this DoubleEntry entry)
        {
            return new Int64Entry { ticks = entry.ticks, value = (long)entry.value };
        }

        public static Int64Entry[] ToInt64(this DoubleEntry[] chunck)
        {
            return chunck.Select(ToInt64).ToArray();
        }

        private static readonly string[] dateTimeFormats = {
            "yyyy-MM-dd HH:mm:ss.fff",
            "yyyy-MM-dd HH:mm:ss",
            "yyyy-MM-dd HH:mm",
            "yyyy-MM-dd",
            "dd/MM/yyyy HH:mm:ss.fff",
            "dd/MM/yyyy HH:mm:ss",
            "dd/MM/yyyy HH:mm",
            "dd/MM/yyyy",
            "HH:mm:ss.fff",
            "HH:mm:ss",
            "HH:mm",
        };
        
        [DebuggerStepThrough]
        public static DateTime ToDateTime(this string value)
        {
            if (string.IsNullOrEmpty(value)) return DateTime.MinValue;
            if (value.ToLower() == "min") return DateTime.MinValue;
            if (value.ToLower() == "max") return DateTime.MaxValue;

            DateTime.TryParseExact(value, dateTimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out var result);
            return result;
        }

        #endregion

        #endregion

        public static T GetNext<T>(this IColumnCursor<T> cursor, int index)
            where T : struct
        {
            cursor.MoveNext(index);
            return cursor.Value;
        }

        public static int Floor(this int x, int qo) => x / qo * qo;

        public static unsafe void SetAll(this UnsafeBuffer buffer, byte value) 
            => SetAll(buffer.Ptr, buffer.Length, value);

        public static unsafe void SetAllUntil(this UnsafeBuffer buffer, int length, byte value)
            => SetAll(buffer.Ptr, length, value);

        public static unsafe void SetAll(this IntPtr ptr, int length, byte value) 
            => SetAll((byte*)ptr, length, value);

        private static unsafe void SetAll(byte* p, int length, byte value)
        {
            for (var i = 0; i < length; i++)
                *(p + i) = value;
        }

        public static IEnumerable<DateTime> SplitDaysTo(this DateTime start, DateTime end)
        {
            while (start.Date <= end.Date)
            {
                yield return start.Date;
                start = start.AddDays(1);
            }
        }

        [DebuggerStepThrough]
        public static IRowRecorder<ITimeSeriesRecorder> AddRow(this ITimeSeriesRecorder recorder, string timestamp)
            => recorder.AddRow(timestamp.ToDateTime());

        public static unsafe void CopyBlock(byte* src, byte* dst, int length, int take = int.MaxValue, int skip = 0)
        {
            take = Math.Min(length, take);

            var end = src + length;
            while (src < end)
            {
                Unsafe.CopyBlock(dst, src, (uint)take);
                src += take + skip;
                dst += take + skip;
            }
        }
    }
}
