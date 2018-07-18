using System;
using System.Text;
using Minotaur.Tests.Tools;
using Xunit;
using Xunit.Abstractions;

namespace Minotaur.Benchmarks
{
    public class MemoryCopyBench
    {
        private readonly ITestOutputHelper _output;

        public MemoryCopyBench(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void Bench()
        {
            const int count = 20;

            var maxSize = (int) Math.Pow(2, count);
            var src = new byte[maxSize];
            var dst = new byte[maxSize];
            var sb = new StringBuilder(8192);

            Action<byte[], byte[], int> cm1 = CopyMem1;
            Action<byte[], byte[], int> cm2 = CopyMem2;

            for (var i = 0; i < count; i++)
            {
                var size = (int)Math.Pow(2, i + 1);

                sb.AppendLine();
                sb.AppendLine($"== For Size: {size}");
                sb.AppendLine($"#1 {cm1.Measure(src, dst, size)}");
                sb.AppendLine($"#2 {cm2.Measure(src, dst, size)}");
            }

           _output.WriteLine(sb.ToString());
        }

        private static unsafe void CopyMem1(byte[] src, byte[] dst, int len)
        {
            fixed (byte* pSrc = src)
            fixed (byte* pDst = dst)
                CopyMemory1(pSrc, pDst, len);
        }
        private static unsafe void CopyMemory1(byte* src, byte* dst, int length)
        {
            Buffer.MemoryCopy(src, dst, length, length);
        }

        private static unsafe void CopyMem2(byte[] src, byte[] dst, int len)
        {
            fixed (byte* pSrc = src)
            fixed (byte* pDst = dst)
                CopyMemory2(pSrc, pDst, len);
        }
        private const int OPTIMAL_MEMCPY_SIZE = 8192;
        private static unsafe void CopyMemory2(byte* src, byte* dst, int length)
        {
            // Todo: Benchmark with  Buffer.MemoryCopy();
            // Todo: Benchmark with Buffer.Memove() from : https://github.com/dotnet/coreclr/blob/ea9bee5ac2f96a1ea6b202dc4094b8d418d9209c/src/mscorlib/src/System/Buffer.cs
            var nbSteps = length / OPTIMAL_MEMCPY_SIZE;
            for (var i = 0; i < nbSteps; i++)
            {
                Buffer.MemoryCopy(src, dst, OPTIMAL_MEMCPY_SIZE, OPTIMAL_MEMCPY_SIZE);
                src += OPTIMAL_MEMCPY_SIZE;
                dst += OPTIMAL_MEMCPY_SIZE;
            }

            var remainingSize = length - nbSteps * OPTIMAL_MEMCPY_SIZE;
            if (remainingSize > 0)
                Buffer.MemoryCopy(src, dst, OPTIMAL_MEMCPY_SIZE, OPTIMAL_MEMCPY_SIZE);
        }
    }
}