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
    public unsafe class XorSwapBenchmark
    {
        private byte* x;
        private byte* y;

        [GlobalSetup]
        public void Setup()
        {
            x = (byte*)Marshal.AllocHGlobal(sizeof(DoubleEntry));
            y = (byte*)Marshal.AllocHGlobal(sizeof(DoubleEntry));
        }

        [GlobalCleanup]
        public void Cleanup()
        {
            Marshal.FreeHGlobal((IntPtr)x);
            Marshal.FreeHGlobal((IntPtr)y);
        }

        [Benchmark(Baseline = true)]
        public void SwapWithTmp()
        {
            var tmp = x;
            x = y;
            y = tmp;
        }

        [Benchmark]
        public void XorSwap()
        {
            x = (byte*)((ulong)x ^ (ulong)y);
            y = (byte*)((ulong)y ^ (ulong)x);
            x = (byte*)((ulong)x ^ (ulong)y);
        }
    }
}
