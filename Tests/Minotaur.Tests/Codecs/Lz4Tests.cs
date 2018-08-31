using System;
using System.Diagnostics;
using Minotaur.Codecs;
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
            const int size = 16 * Bits.KILO_BYTE;
            var count = size / sizeof(DoubleEntry);

            var chunk = Factory.CreateDoubleChunk(count);
            var buffer = new byte[size];
            var result = new DoubleEntry[count];

            fixed (byte* o = buffer)
            {
                int compressedSize;
                fixed (DoubleEntry* i = chunk)
                    compressedSize = Lz4.Encode64((byte*) i, o, size, (int)Lz4.MaximumOutputLength(size));
                
                Console.WriteLine("Compression ratio: x {0}", size / (double)compressedSize);

                fixed (DoubleEntry* r = result)
                    Lz4.Decode64(o, compressedSize, (byte*)r, size, true);
            }
            
            result.Check(chunk);
        }

        [Test]
        public void UncompressWithLowerBlocksTest()
        {
            const int size = 16 * Bits.KILO_BYTE;
            const int readBlockSize = 4 * Bits.KILO_BYTE;
            var count = size / sizeof(DoubleEntry);


            var chunk = Factory.CreateDoubleChunk(count);
            var buffer = new byte[size];
            var result = new DoubleEntry[count];

            fixed (byte* o = buffer)
            {
                int compressedSize;
                fixed (DoubleEntry* i = chunk)
                    compressedSize = Lz4.Encode64((byte*)i, o, size, (int)Lz4.MaximumOutputLength(size));

                Console.WriteLine("Compression ratio: x {0}", size / (double)compressedSize);

                fixed (DoubleEntry* r = result)
                {
                    var iPtr = o;
                    var oPtr = (byte*)r;
                    var inputLength = compressedSize;

                    var wrote = 0;
                    while (wrote < size)
                        wrote += Lz4.Decode64(ref iPtr, inputLength, ref oPtr, size - wrote, readBlockSize);
                }
            }

            result.Check(chunk);
        }

        [Test]
        public void UncompressWithHigherBlocksTest()
        {
            const int size = 4 * Bits.KILO_BYTE;
            const int readBlockSize = 16 * Bits.KILO_BYTE;
            var count = size / sizeof(DoubleEntry);


            var chunk = Factory.CreateDoubleChunk(count);
            var buffer = new byte[size];
            var result = new DoubleEntry[count];

            fixed (byte* o = buffer)
            {
                int compressedSize;
                fixed (DoubleEntry* i = chunk)
                    compressedSize = Lz4.Encode64((byte*)i, o, size, (int)Lz4.MaximumOutputLength(size));

                Console.WriteLine("Compression ratio: x {0}", size / (double)compressedSize);

                fixed (DoubleEntry* r = result)
                {
                    var iPtr = o;
                    var oPtr = (byte*)r;
                    var inputLength = compressedSize;

                    var wrote = 0;
                    while (wrote < size)
                        wrote += Lz4.Decode64(ref iPtr, inputLength, ref oPtr, size - wrote, readBlockSize);
                }
            }

            result.Check(chunk);
        }

        [Test]
        public void ComparePerf()
        {
            const int size = 4 * Bits.KILO_BYTE;
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
                    compressedSize = Lz4.Encode64((byte*)i, o, size, (int)Lz4.MaximumOutputLength(size));
                Console.WriteLine("Compression ratio: x {0}", size / (double)compressedSize);

                fixed (DoubleEntry* r = result)
                {
                    Lz4.Decode64(o, compressedSize, (byte*) r, size, true);

                    // Jitter purpose
                    for (var i = 0; i < 10; i++)
                    {
                        var iPtr = o;
                        var oPtr = (byte*)r;
                        var writeBlockSize = readSizes[0] * Bits.KILO_BYTE;
                        var wrote = 0;
                        while (wrote < size)
                            wrote += Lz4.Decode64(ref iPtr, compressedSize, ref oPtr, size - wrote, writeBlockSize);
                    }

                    var sw = Stopwatch.StartNew();
                    for (var i = 0; i < iterations; i++)
                        Lz4.Decode64(o, compressedSize, (byte*)r, size, true);
                    sw.Stop();
                    Console.WriteLine("Match: {0} µs", sw.Elapsed.TotalMilliseconds / iterations * 1e3);
                    result.Check(chunk);

                    for (var j = 0; j < readSizes.Length; j++)
                    {
                        var writeBlockSize = readSizes[j] * Bits.KILO_BYTE;

                        sw = Stopwatch.StartNew();
                        for (var i = 0; i < iterations; i++)
                        {
                            var iPtr = o;
                            var oPtr = (byte*)r;

                            var wrote = 0;
                            while (wrote < size)
                                wrote += Lz4.Decode64(ref iPtr, compressedSize, ref oPtr, size - wrote, writeBlockSize);
                        }
                        sw.Stop();
                        Console.WriteLine("Size {0} KB: {1} µs", readSizes[j], sw.Elapsed.TotalMilliseconds / iterations * 1e3);
                        result.Check(chunk);
                    }
                }
            }
        }
    }
}
