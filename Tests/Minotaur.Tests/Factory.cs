﻿using System;
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
    }
}