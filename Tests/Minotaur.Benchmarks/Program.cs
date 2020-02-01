using System;
using System.Collections.Generic;
using System.Linq;
using System.Management;
using System.Reflection;
using System.Threading;
using BenchmarkDotNet.Running;
using Minotaur.Benchmarks.Codecs;
using Minotaur.Benchmarks.HighPerf;

namespace Minotaur.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<ObjectPoolBenchmark>();
            //BenchmarkRunner.Run<ColumnStreamBenchmark>();
            //BenchmarkRunner.Run<Int32CodecEncodeDecodeBenchmark>();
            //BenchmarkRunner.Run<Int32CodecDecodeOnlyBenchmark>();
            //foreach (var cacheMemory in GetCacheInfo())
            //    Console.WriteLine(cacheMemory);
            //Console.ReadLine();
        }

        public static List<CacheMemory> GetCacheInfo()
        {
            var properties = typeof(CacheMemory).GetProperties();
            return new ManagementClass("Win32_CacheMemory")
                .GetInstances()
                .OfType<ManagementObject>()
                .Select(p =>
                {
                    var cm = new CacheMemory();
                    foreach (var property in properties)
                        property.SetValue(cm, p.Properties[property.Name].Value);
                    return cm;
                })
                .ToList();
        }
    }

    public enum CacheLevel : ushort
    {
        Level1 = 3,
        Level2 = 4,
        Level3 = 5
    }

    public class CacheMemory
    {
        public static readonly PropertyInfo[] Properties = typeof(CacheMemory).GetProperties();

        public string Name { get; set; }
        public ulong BlockSize { get; set; }
        public uint CacheSpeed { get; set; }
        public ushort CacheType { get; set; }
        public CacheLevel Level { get; set; }
        public uint LineSize { get; set; }
        public ushort Location { get; set; }
        public uint MaxCacheSize { get; set; }

        public override string ToString()
        {
            return $"{nameof(Name)}: {Name}, {nameof(BlockSize)}: {BlockSize}, {nameof(CacheSpeed)}: {CacheSpeed}, {nameof(CacheType)}: {CacheType}, {nameof(Level)}: {Level}, {nameof(LineSize)}: {LineSize}, {nameof(Location)}: {Location}, {nameof(MaxCacheSize)}: {MaxCacheSize}";
        }
    }
}
