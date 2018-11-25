using System;
using System.Diagnostics;
using Minotaur.Codecs;
using Minotaur.Codecs.LZ4;
using Minotaur.Core;
using Minotaur.Native;
using NUnit.Framework;

namespace Minotaur.Tests.Codecs
{
    [TestFixture]
    public unsafe class Lz4Tests
    {
        [Test]
        public void NominaleTest()
        {
            const int size = 16 * Mem.KB;
            var count = size / sizeof(DoubleEntry);

            var chunk = Factory.CreateDoubleChunk(count);
            var buffer = new byte[size];
            var result = new DoubleEntry[count];

            fixed (byte* o = buffer)
            {
                int compressedSize;
                fixed (DoubleEntry* i = chunk)
                {
                    var pi = (byte*)i;
                    var po = o;
                    compressedSize = Lz4.LZ4_compress_fast(pi, po, size, (int)Lz4.MaximumOutputLength(size), 1);
                }

                Console.WriteLine("Compression ratio: x {0}", size / (double)compressedSize);

                fixed (DoubleEntry* r = result)
                {
                    var ip = o;
                    var op = (byte*)r;
                    Lz4.LZ4_decompress_fast(ref ip, ref op, compressedSize);
                }
            }

            result.Check(chunk);
        }

        [Test]
        public void RavenNominaleTest()
        {
            const int size = 16 * Mem.KB;
            var count = size / sizeof(DoubleEntry);

            var chunk = Factory.CreateDoubleChunk(count);
            var buffer = new byte[size];
            var result = new DoubleEntry[count];

            fixed (byte* o = buffer)
            {
                int compressedSize;
                fixed (DoubleEntry* i = chunk)
                {
                    var pi = (byte*) i;
                    var po = o;
                    compressedSize = Lz4Raven.Encode64(ref pi, ref po, size, (int)Lz4Raven.MaximumOutputLength(size));
                }
                
                Console.WriteLine("Compression ratio: x {0}", size / (double)compressedSize);

                fixed (DoubleEntry* r = result)
                    Lz4Raven.Decode64(o, compressedSize, (byte*)r, size, true);
            }
            
            result.Check(chunk);
        }

        [Test]
        public void RavenUncompressWithLowerBlocksTest()
        {
            const int size = 16 * Mem.KB;
            const int writeBlockSize = 4 * Mem.KB;
            var count = size / sizeof(DoubleEntry);


            var chunk = Factory.CreateDoubleChunk(count);
            var buffer = new byte[size];
            var result = new DoubleEntry[count];

            fixed (byte* o = buffer)
            {
                int compressedSize;
                fixed (DoubleEntry* i = chunk)
                {
                    var pi = (byte*)i;
                    var po = o;
                    compressedSize = Lz4Raven.Encode64(ref pi, ref po, size, (int)Lz4Raven.MaximumOutputLength(size));
                }

                Console.WriteLine("Compression ratio: x {0}", size / (double)compressedSize);

                fixed (DoubleEntry* r = result)
                {
                    var iPtr = o;
                    var oPtr = (byte*)r;
                    var inputLength = compressedSize;

                    var wrote = 0;
                    while (wrote < size)
                        wrote += Lz4Raven.Decode64(ref iPtr, inputLength, ref oPtr, size - wrote, writeBlockSize);
                }
            }

            result.Check(chunk);
        }

        [Test]
        public void RavenUncompressWithHigherBlocksTest()
        {
            const int size = 4 * Mem.KB;
            const int writeBlockSize = 16 * Mem.KB;
            var count = size / sizeof(DoubleEntry);


            var chunk = Factory.CreateDoubleChunk(count);
            var buffer = new byte[size];
            var result = new DoubleEntry[count];

            fixed (byte* o = buffer)
            {
                int compressedSize;
                fixed (DoubleEntry* i = chunk)
                {
                    var pi = (byte*)i;
                    var po = o;
                    compressedSize = Lz4Raven.Encode64(ref pi, ref po, size, (int)Lz4Raven.MaximumOutputLength(size));
                }

                Console.WriteLine("Compression ratio: x {0}", size / (double)compressedSize);

                fixed (DoubleEntry* r = result)
                {
                    var iPtr = o;
                    var oPtr = (byte*)r;
                    var inputLength = compressedSize;

                    var wrote = 0;
                    while (wrote < size)
                        wrote += Lz4Raven.Decode64(ref iPtr, inputLength, ref oPtr, size - wrote, writeBlockSize);
                }
            }

            result.Check(chunk);
        }

        [Test]
        public void RavenUncompressComparePerf()
        {
            const int size = 4 * Mem.KB;
            var readSizes = new [] { 1, 2, 4, 8, 16};
            const int iterations = 10000;
            var count = size / sizeof(DoubleEntry);

            var chunk = Factory.CreateDoubleChunk(count);
            var buffer = new byte[size];
            var result = new DoubleEntry[count];

            fixed (byte* o = buffer)
            {
                int compressedSize;
                fixed (DoubleEntry* i = chunk)
                {
                    var pi = (byte*)i;
                    var po = o;
                    compressedSize = Lz4Raven.Encode64(ref pi, ref po, size, (int)Lz4Raven.MaximumOutputLength(size));
                }
                Console.WriteLine("Compression ratio: x {0}", size / (double)compressedSize);

                fixed (DoubleEntry* r = result)
                {
                    Lz4Raven.Decode64(o, compressedSize, (byte*) r, size, true);

                    // Jitter purpose
                    for (var i = 0; i < 10; i++)
                    {
                        var iPtr = o;
                        var oPtr = (byte*)r;
                        var writeBlockSize = readSizes[0] * Mem.KB;
                        var wrote = 0;
                        while (wrote < size)
                            wrote += Lz4Raven.Decode64(ref iPtr, compressedSize, ref oPtr, size - wrote, writeBlockSize);
                    }

                    var sw = Stopwatch.StartNew();
                    for (var i = 0; i < iterations; i++)
                        Lz4Raven.Decode64(o, compressedSize, (byte*)r, size, true);
                    sw.Stop();
                    Console.WriteLine("Match: {0} µs", sw.Elapsed.TotalMilliseconds / iterations * 1e3);
                    result.Check(chunk);

                    for (var j = 0; j < readSizes.Length; j++)
                    {
                        var writeBlockSize = readSizes[j] * Mem.KB;

                        sw = Stopwatch.StartNew();
                        for (var i = 0; i < iterations; i++)
                        {
                            var iPtr = o;
                            var oPtr = (byte*)r;

                            var wrote = 0;
                            while (wrote < size)
                                wrote += Lz4Raven.Decode64(ref iPtr, compressedSize, ref oPtr, size - wrote, writeBlockSize);
                        }
                        sw.Stop();
                        Console.WriteLine("Size {0} KB: {1} µs", readSizes[j], sw.Elapsed.TotalMilliseconds / iterations * 1e3);
                        result.Check(chunk);
                    }
                }
            }
        }

        [Test]
        public void RavenCompressWithLowerBlockTest()
        {
            const int size = 16 * Mem.KB;
            const int writeBlockSize = 1 * Mem.KB;
            var count = size / sizeof(DoubleEntry);


            var chunk = Factory.CreateDoubleChunk(count);
            var buffer = new byte[size];
            var result = new DoubleEntry[count];

            fixed (byte* o = buffer)
            {
                var compressedSize = 0;
                fixed (DoubleEntry* i = chunk)
                {
                    var pi = (byte*)i;
                    var pEnd = pi + size;
                    var po = o;

                    while (pi < pEnd)
                    {
                        var wrote = Lz4Raven.Encode64(ref pi, ref po, (int)(pEnd - pi), size, writeBlockSize, 1);
                        //Assert.LessOrEqual(wrote, writeBlockSize);
                        compressedSize += wrote;
                    }
                }

                Console.WriteLine("Compression ratio: x {0}", size / (double)compressedSize);

                fixed (DoubleEntry* r = result)
                {
                    var iPtr = o;
                    var oPtr = (byte*)r;
                    var inputLength = compressedSize;

                    var wrote = 0;
                    while (wrote < size)
                        wrote += Lz4Raven.Decode64(ref iPtr, inputLength, ref oPtr, size - wrote, writeBlockSize);
                }
            }

            result.Check(chunk);
        }
    }
}
