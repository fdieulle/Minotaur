using System;
using System.Collections.Generic;
using Minotaur.Native;

namespace Minotaur.Tests
{
    public static class Factory
    {
        private static readonly Random random = new Random(42);

        public static byte[] CreateRandomBytes(int count)
        {
            var data = new byte[count];
            random.NextBytes(data);
            return data;
        }

        public static int[] CreateRandomInt32(int count)
        {
            var data = new int[count];
            for(var i=0; i< data.Length; i++)
                data[i] = random.Next(int.MinValue, int.MaxValue);
            return data;
        }

        public static double[] CreateRandomDouble(int count)
        {
            var data = new double[count];
            for (var i = 0; i < data.Length; i++)
                data[i] = random.NextDouble();
            return data;
        }

        public static long[] CreateTimelineTicks(int count, double intervalMs = 233, DateTime? start = null)
        {
            var ticks = new long[count];
            var now = start ?? DateTime.Now;
            for (var i = 0; i < count; i++)
            {
                ticks[i] = now.Ticks;
                now = now.AddMilliseconds(intervalMs);
            }
            return ticks;
        }

        public static DateTime[] CreateRandomDateTime(DateTime start, DateTime end, double intervalMs = 233)
        {
            var list = new List<DateTime>();

            while (start < end)
            {
                list.Add(start);
                start = start.AddMilliseconds(random.NextDouble() * 2 * intervalMs);
            }

            list.Add(end);
            return list.ToArray();
        }

        public static Int32Entry[] CreateInt32Chunk(int count, DateTime? start = null, int valueQo = 1000, double percentOfFill = 1.0, int ticksIntervalMs = 100)
        {
            var data = new Int32Entry[(int)(count * percentOfFill)];

            var ticks = (start ?? DateTime.Now).Ticks;
            var ticksStep = TimeSpan.FromMilliseconds(ticksIntervalMs).Ticks;
            data[0].ticks = ticks;
            data[0].value = (int)(random.NextDouble() * 100 / valueQo) * valueQo;

            for (int i = 1, j = 1; i < count && j < data.Length; i++)
            {
                ticks += ticksStep;

                if (!(random.NextDouble() + percentOfFill >= 1.0)) continue;

                data[j].ticks = ticks;
                data[j].value = (int)(random.NextDouble() * 100 / valueQo) * valueQo;
                j++;
            }
            return data;
        }

        public static Int64Entry[] CreateInt64Chunk(int count, DateTime? start = null, int valueQo = 1000, double percentOfFill = 1.0, int ticksIntervalMs = 100)
        {
            var data = new Int64Entry[(int)(count * percentOfFill)];

            var ticks = (start ?? DateTime.Now).Ticks;
            var ticksStep = TimeSpan.FromMilliseconds(ticksIntervalMs).Ticks;
            data[0].ticks = ticks;
            data[0].value = (int)(random.NextDouble() * 100 / valueQo) * valueQo;

            for (int i = 1, j = 1; i < count && j < data.Length; i++)
            {
                ticks += ticksStep;

                if (!(random.NextDouble() + percentOfFill >= 1.0)) continue;

                data[j].ticks = ticks;
                data[j].value = (long)(random.NextDouble() * 100 / valueQo) * valueQo;
                j++;
            }
            return data;
        }

        public static DoubleEntry[] CreateDoubleChunk(int count, DateTime? start = null, double percentOfFill = 1.0, int ticksIntervalMs = 100)
        {
            var data = new DoubleEntry[(int)(count * percentOfFill)];

            var ticks = (start ?? DateTime.Now).Ticks;
            var ticksStep = TimeSpan.FromMilliseconds(ticksIntervalMs).Ticks;
            data[0].ticks = ticks;
            data[0].value = random.NextDouble() * 100.0;

            for (int i = 1, j = 1; i < count && j < data.Length; i++)
            {
                ticks += ticksStep;

                if (!(random.NextDouble() + percentOfFill >= 1.0)) continue;

                data[j].ticks = ticks;
                data[j].value = random.NextDouble() * 100.0;
                j++;
            }
            return data;
        }

        public static FloatEntry[] CreateFloatChunk(int count, DateTime? start = null, double percentOfFill = 1.0, int ticksIntervalMs = 100)
        {
            var data = new FloatEntry[(int)(count * percentOfFill)];

            var ticks = (start ?? DateTime.Now).Ticks;
            var ticksStep = TimeSpan.FromMilliseconds(ticksIntervalMs).Ticks;
            data[0].ticks = ticks;
            data[0].value = (float)(random.NextDouble() * 100.0);

            for (int i = 1, j = 1; i < count && j < data.Length; i++)
            {
                ticks += ticksStep;

                if (!(random.NextDouble() + percentOfFill >= 1.0)) continue;

                data[j].ticks = ticks;
                data[j].value = (float)(random.NextDouble() * 100.0);
                j++;
            }
            return data;
        }

        public static StringEntry[] CreateStringChunk(int count, DateTime? start = null, double percentOfFill = 1.0, int ticksIntervalMs = 100)
        {
            var data = new StringEntry[(int)(count * percentOfFill)];

            var ticks = (start ?? DateTime.Now).Ticks;
            var ticksStep = TimeSpan.FromMilliseconds(ticksIntervalMs).Ticks;
            data[0].ticks = ticks;
            data[0].SetValue(Guid.NewGuid().ToString("D"));

            for (int i = 1, j = 1; i < count && j < data.Length; i++)
            {
                ticks += ticksStep;

                if (!(random.NextDouble() + percentOfFill >= 1.0)) continue;

                data[j].ticks = ticks;
                data[j].SetValue(Guid.NewGuid().ToString("D"));
                j++;
            }
            return data;
        }
    }
}
