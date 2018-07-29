using BenchmarkDotNet.Running;

namespace Minotaur.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<ColumnStreamBenchmark>();
        }
    }
}
