using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using Minotaur.Native;

namespace Minotaur.Benchmarks
{
    [AllStatisticsColumn]
    [DisassemblyDiagnoser(printSource: true)]
    public unsafe class MinMaxBranchless
    {
        public IEnumerable<object[]> DateTimes()
        {
            yield return new object[] { DateTime.Now.Ticks, DateTime.Now.AddDays(1).Ticks };
            yield return new object[] { DateTime.Now.AddDays(1).Ticks, DateTime.Now.Ticks };
        }

        [Benchmark]
        [ArgumentsSource(nameof(DateTimes))]
        public long Min(long x, long y)
        {
            return x > y ? y : x;
        }

        //[Benchmark]
        //[ArgumentsSource(nameof(DateTimes))]
        //public DateTime Min2(DateTime x, DateTime y)
        //{
        //    return x.Ticks > y.Ticks ? y : x;
        //}

        [Benchmark]
        [ArgumentsSource(nameof(DateTimes))]
        public long Min3(long x, long y)
        {
            var cmp = x < y;
            return y ^ ((x ^ y) & ~*((byte*)&cmp));
        }

        //public DateTime Min4(DateTime x, DateTime y)
        //{
        //    return y + ((x.Ticks - y.Ticks) & ((x.Ticks - y.Ticks) >> (sizeof(int) * CHAR_BIT - 1)));
        //}
    }
}
