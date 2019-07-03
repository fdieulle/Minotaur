using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Minotaur.Codecs;
using Minotaur.Core;
using Minotaur.Native;
using Minotaur.Pocs.Codecs;
using Minotaur.Pocs.Codecs.Int32;
using Minotaur.Tests.Tools;
using NUnit.Framework;

namespace Minotaur.Tests.Codecs
{
    [TestFixture]
    public unsafe class CodecTests
    {
        [Test]
        public void TestEncodeDecodeUInt64()
        {
            const int factor = 5;
            var values = new ulong[64 * factor];
            for (var i = 1; i < 64; i++)
            {
                var value = (ulong)Math.Pow(2, i);
                values[i * 2] = value - 2;
                values[i * 2 + 1] = value - 1;
                values[i * 2 + 2] = value;
                values[i * 2 + 3] = value + 1;
                values[i * 2 + 3] = value + 2;
            }

            var buffer = stackalloc byte[9];
            for (var i = 0; i < values.Length; i++)
            {
                var p = buffer;
                Codec.EncodeUInt64(values[i], ref p);

                p = buffer;
                var value = Codec.DecodeUInt64(ref p);
                Assert.AreEqual(values[i], value, "At idx: " + i);
            }
        }

        [Test]
        public void TestEncodeDecodeInt64()
        {
            const int factor = 10;
            var values = new long[64 * factor];
            for (var i = 1; i < 64; i++)
            {
                var value = (long)Math.Pow(2, i);

                values[i * factor] = value - 2;
                values[i * factor + 1] = value - 1;
                values[i * factor + 2] = value;
                values[i * factor + 3] = value + 1;
                values[i * factor + 4] = value + 2;

                values[i * factor + 5] = -values[i * factor];
                values[i * factor + 6] = -values[i * factor + 1];
                values[i * factor + 7] = -values[i * factor + 2];
                values[i * factor + 8] = -values[i * factor + 3];
                values[i * factor + 9] = -values[i * factor + 4];
            }

            var buffer = stackalloc byte[9];
            for (var i = 0; i < values.Length; i++)
            {
                var p = buffer;
                Codec.EncodeInt64(values[i], ref p);

                p = buffer;
                var value = Codec.DecodeInt64(ref p);
                Assert.AreEqual(values[i], value, "At idx: " + i);
            }
        }

        [Test]
        public void TestEncodeDecodeUInt32()
        {
            var values = new uint[32 * 2];
            for (var i = 1; i < 32; i++)
            {
                var value = (uint)Math.Pow(2, i);
                values[i * 2] = value - 1;
                values[i * 2 + 1] = value;
            }

            var buffer = stackalloc byte[5];
            for (var i = 0; i < values.Length; i++)
            {
                var p = buffer;
                Codec.EncodeUInt32(values[i], ref p);

                p = buffer;
                var value = Codec.DecodeUInt32(ref p);
                Assert.AreEqual(values[i], value, "At idx: " + i);
            }
        }

        [Test]
        public void TestEncodeDecodeInt32()
        {
            var values = new int[32 * 4];
            for (var i = 1; i < 32; i++)
            {
                var value = (uint)Math.Pow(2, i);
                values[i * 4] = (int)value - 1;
                values[i * 4 + 1] = (int)value;
                values[i * 4 + 2] = -values[i * 4];
                values[i * 4 + 3] = -values[i * 4 + 1];
            }

            var buffer = stackalloc byte[5];
            for (var i = 0; i < values.Length; i++)
            {
                var p = buffer;
                Codec.EncodeInt32(values[i], ref p);

                p = buffer;
                var value = Codec.DecodeInt32(ref p);
                Assert.AreEqual(values[i], value, "At idx: " + i);
            }
        }

        [Test]
        public void TestEncodeDecodeFlaggedInt32()
        {
            var values = new int[32 * 4];
            for (var i = 1; i < 32; i++)
            {
                var value = (uint)Math.Pow(2, i);
                values[i * 4] = (int)value - 1;
                values[i * 4 + 1] = (int)value;
                values[i * 4 + 2] = -values[i * 4];
                values[i * 4 + 3] = -values[i * 4 + 1];
            }

            var buffer = stackalloc byte[5];
            for (var i = 0; i < values.Length; i++)
            {
                var p = buffer;
                Codec.EncodeInt32(true, values[i], ref p);

                p = buffer;
                var value = Codec.DecodeInt32(ref p, out var flag);
                Assert.AreEqual(values[i], value, "At idx: " + i);
                Assert.IsTrue(flag);
            }

            for (var i = 0; i < values.Length; i++)
            {
                var p = buffer;
                Codec.EncodeInt32(false, values[i], ref p);

                p = buffer;
                var value = Codec.DecodeInt32(ref p, out var flag);
                Assert.AreEqual(values[i], value, "At idx: " + i);
                Assert.IsFalse(flag);
            }
        }

        [Test]
        public void TestEncodeDecodeFlaggedInt64()
        {
            var values = new long[64 * 4];
            for (var i = 1; i < 64; i++)
            {
                var value = (ulong)Math.Pow(2, i);
                values[i * 4] = (long)value - 1;
                values[i * 4 + 1] = (long)value;
                values[i * 4 + 2] = -values[i * 4];
                values[i * 4 + 3] = -values[i * 4 + 1];
            }

            var buffer = stackalloc byte[5];
            for (var i = 0; i < values.Length; i++)
            {
                var p = buffer;
                Codec.EncodeInt64(true, values[i], ref p);

                p = buffer;
                var value = Codec.DecodeInt64(ref p, out var flag);
                Assert.AreEqual(values[i], value, "At idx: " + i);
                Assert.IsTrue(flag);
            }

            for (var i = 0; i < values.Length; i++)
            {
                var p = buffer;
                Codec.EncodeInt64(false, values[i], ref p);

                p = buffer;
                var value = Codec.DecodeInt64(ref p, out var flag);
                Assert.AreEqual(values[i], value, "At idx: " + i);
                Assert.IsFalse(flag);
            }
        }

        [TestCase(10)]
        [TestCase(100)]
        [TestCase(1000)]
        [TestCase(10000)]
        public void EncodeDecodeMinDelta64BlockNoSkip(int count) 
            => TestEncodeDecodeMinDelta64(count, c => Factory.CreateTimelineTicks(c), 0);

        [TestCase(10)]
        [TestCase(100)]
        [TestCase(1000)]
        [TestCase(10000)]
        public void EncodeDecodeMinDelta64BlockForInt32Chunk(int count)
            => TestEncodeDecodeMinDelta64(count, c => Factory.CreateInt32Chunk(c), sizeof(int));

        [TestCase(10)]
        [TestCase(100)]
        [TestCase(1000)]
        [TestCase(10000)]
        public void EncodeDecodeMinDelta64BlockForInt64Chunk(int count)
            => TestEncodeDecodeMinDelta64(count, c => Factory.CreateInt64Chunk(c), sizeof(long));

        private void TestEncodeDecodeMinDelta64<T>(int count, Func<int, T[]> factory, int skip)
            where T : unmanaged
        {
            var chunk = factory(count);
            var result = new T[count];

            var size = CodecExt.GetMaxEncodedSizeForMinDeltaU64(chunk.Length);
            var buffer = new UnsafeBuffer(size);

            fixed (T* p = chunk)
            {
                var dst = buffer.Ptr;
                CodecExt.EncodeMinDeltaU64((byte*)p, count * sizeof(T), skip, ref dst);
            }

            fixed (T* p = result)
            {
                var src = buffer.Ptr;
                CodecExt.DecodeMinDeltaU64(ref src, skip, (byte*)p);
            }

            chunk.IsEqualTo(result);
        }

        [TestCase(10)]
        [TestCase(100)]
        [TestCase(1000)]
        [TestCase(10000)]
        public void EncodeDecodeMinDelta32BlockNoSkip(int count)
            => TestEncodeDecodeMinDelta32(count, Factory.CreateRandomInt32, 0, 0);

        [TestCase(5)]
        [TestCase(100)]
        [TestCase(1000)]
        [TestCase(10000)]
        public void EncodeDecodeMinDelta32BlockForInt32Chunk(int count)
            => TestEncodeDecodeMinDelta32(count, c => Factory.CreateInt32Chunk(c), sizeof(long), sizeof(long));

        private void TestEncodeDecodeMinDelta32<T>(int count, Func<int, T[]> factory, int shift, int skip)
            where T : unmanaged
        {
            var chunk = factory(count);
            var result = new T[count];

            var size = CodecExt.GetMaxEncodedSizeForMinDelta32(chunk.Length);
            var buffer = new UnsafeBuffer(size);

            // Copy partial data
            if (skip > 0)
            {
                fixed (T* src = chunk)
                fixed (T* dst = result)
                {
                    Extensions.CopyBlock((byte*)src, (byte*)dst, count * sizeof(T), skip, sizeof(T) - skip);
                }
            }

            fixed (T* p = chunk)
            {
                var dst = buffer.Ptr;
                CodecExt.EncodeMinDelta32((byte*)p + shift, count * sizeof(T) - shift, skip, ref dst);
            }

            fixed (T* p = result)
            {
                var src = buffer.Ptr;
                CodecExt.DecodeMinDelta32(ref src, skip, (byte*)p + shift);
            }

            chunk.IsEqualTo(result);
        }

        [Test]
        public void Test()
        {
            var sb = new StringBuilder();

            var val = 1;
            Write(sb, (byte*)(&val), sizeof(int));
            val = 1 << 8;
            Write(sb, (byte*)(&val), sizeof(int));
            val = 1 << 16;
            Write(sb, (byte*)(&val), sizeof(int));
            val = 1 << 24;
            Write(sb, (byte*)(&val), sizeof(int));

            Console.Write(sb);
        }

        private static void Write(StringBuilder sb, byte* p, int size)
        {
            for (var i = 0; i < sizeof(int); i++)
                sb.AppendFormat("{0:000}-", *(p + i));
            sb.AppendLine();
        }

        [Test]
        public void TestNoCompressionCodec()
        {
            CheckCodec(new VoidCodecFullStream(), p => Factory.CreateDoubleChunk(p), sizeof(DoubleEntry), checkDecodeHeadInMove: false);
            CheckCodec(new VoidCodecFullStream(), p => Factory.CreateInt32Chunk(p), sizeof(Int32Entry), checkDecodeHeadInMove: false);
        }

        [Test]
        public void CheckPerf()
        {
            var codec1 = new DeltaMeanLengthInt32Codec();
            var codec2 = new DeltaMeanLengthInt32Codec2();

            var chunk1024 = Factory.CreateInt32Chunk(1024 / sizeof(Int32Entry));
            var chunk2048 = Factory.CreateInt32Chunk(2048 / sizeof(Int32Entry));
            var chunk4096 = Factory.CreateInt32Chunk(4096 / sizeof(Int32Entry));
            var chunk8192 = Factory.CreateInt32Chunk(8192 / sizeof(Int32Entry));
            var chunk16384 = Factory.CreateInt32Chunk(16384 / sizeof(Int32Entry));

            var maxSize = Math.Max(codec1.GetMaxEncodedSize(chunk16384.Length), codec2.GetMaxEncodedSize(chunk16384.Length));
            var buffer = new UnsafeBuffer(maxSize);

            Check(codec1, codec2, chunk1024, buffer);
            Check(codec1, codec2, chunk2048, buffer);
            Check(codec1, codec2, chunk4096, buffer);
            Check(codec1, codec2, chunk8192, buffer);
            Check(codec1, codec2, chunk16384, buffer);

            Console.WriteLine("[1024] 1: {0}", Perf.Measure(Run, codec1, chunk1024, buffer));
            Console.WriteLine("[1024] 2: {0}", Perf.Measure(Run, codec2, chunk1024, buffer));

            Console.WriteLine("[2048] 1: {0}", Perf.Measure(Run, codec1, chunk2048, buffer));
            Console.WriteLine("[2048] 2: {0}", Perf.Measure(Run, codec2, chunk2048, buffer));

            Console.WriteLine("[4096] 1: {0}", Perf.Measure(Run, codec1, chunk4096, buffer));
            Console.WriteLine("[4096] 2: {0}", Perf.Measure(Run, codec2, chunk4096, buffer));

            Console.WriteLine("[8192] 1: {0}", Perf.Measure(Run, codec1, chunk8192, buffer));
            Console.WriteLine("[8192] 2: {0}", Perf.Measure(Run, codec2, chunk8192, buffer));

            Console.WriteLine("[16384] 1: {0}", Perf.Measure(Run, codec1, chunk16384, buffer));
            Console.WriteLine("[16384] 2: {0}", Perf.Measure(Run, codec2, chunk16384, buffer));

            buffer.Dispose();
        }

        private static void Check(ICodec<Int32Entry> x, ICodec<Int32Entry> y, Int32Entry[] chunk, UnsafeBuffer buffer)
        {
            var copy = chunk.ToArray();
            Run(x, copy, buffer);
            copy.IsEqualTo(chunk);
            Run(y, copy, buffer);
            copy.IsEqualTo(chunk);
        }

        private static void Run(ICodec<Int32Entry> codec, Int32Entry[] chunk, UnsafeBuffer buffer)
        {
            fixed (Int32Entry* p = chunk)
            {
                var length = codec.Encode(p, chunk.Length, buffer.Ptr);
                codec.Decode(buffer.Ptr, length, p);
            }
        }

        private static void CheckCodec<T>(ICodecFullStream codec, 
            Func<int, T[]> factory, 
            int sizeOfData,
            int bufferLength = 8192,
            bool checkDecodeInMove = true, 
            bool checkDecodeHeadInMove = true)
        {
            var buffer = new byte[bufferLength];
            var data = factory(bufferLength / sizeOfData);
            var dataLength = data.Length;

            fixed (byte* pbuf = buffer)
            {
                var pb = pbuf;

                var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
                try
                {
                    var pdata = (byte*)handle.AddrOfPinnedObject();

                    var pd = pdata;
                    Assert.AreEqual(dataLength, codec.Encode(ref pd, dataLength * sizeOfData, ref pb, bufferLength) / sizeOfData, "Number of data encoded");

                    Assert.AreEqual(dataLength, (pd - pdata) / sizeOfData, "Encoded data length");

                    var wrote = pb - pbuf;
                    Assert.Greater(wrote, 0, "No encoded data wrote");

                    pb = pbuf;

                    var dataRead = new byte[sizeOfData];
                    fixed (byte* pr = dataRead)
                    {
                        var dataToRead = sizeOfData;
                        var mem1 = pb;
                        codec.DecodeHead(ref pb, bufferLength);
                        if (checkDecodeHeadInMove)
                            Assert.Greater(pb - mem1, 0, "Input data didn't move");
                        else Assert.AreEqual(0, pb - mem1, "Input data shouldn't moved");

                        for (var i = 0; i < dataLength; i++)
                        {
                            var d = pr;
                            mem1 = pb;
                            var mem2 = d;

                            Assert.AreEqual(dataToRead, codec.Decode(ref pb, bufferLength - (int)(pb - pbuf), ref d, dataToRead));
                            if (checkDecodeInMove)
                                Assert.Greater(pb - mem1, 0, "Input data didn't move");
                            //else Assert.AreEqual(0, pb - mem1, "Input data shouldn't moved");
                            Assert.AreEqual(sizeOfData, d - mem2, "Output data didn't move");

                            for (var j = 0; j < sizeOfData; j++)
                                Assert.AreEqual(*(pdata + i * sizeOfData + j), *(pr + j), $"Value at idx {i}, at byte {j}");
                        }
                    }

                    Assert.True(wrote == pb - pbuf, "Buffer read");
                }
                finally
                {
                    handle.Free();
                }
            }
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(1024)]
        [TestCase(1024 * 2)]
        [TestCase(1024 * 2 * 2)]
        [TestCase(1024 * 2 * 2 * 2)]
        public void VoidCodecTest(int count)
        {
            CheckCodec(new VoidCodec<Int32Entry>(), count);
            CheckCodec(new VoidCodec<FloatEntry>(), count);
            CheckCodec(new VoidCodec<Int64Entry>(), count);
            CheckCodec(new VoidCodec<DoubleEntry>(), count);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(1024)]
        [TestCase(1024 * 2)]
        [TestCase(1024 * 2 * 2)]
        [TestCase(1024 * 2 * 2 * 2)]
        public void Lz4CodecTest(int count)
        {
            CheckCodec(new Lz4Codec<Int32Entry>(), count);
            CheckCodec(new Lz4Codec<FloatEntry>(), count);
            CheckCodec(new Lz4Codec<Int64Entry>(), count);
            CheckCodec(new Lz4Codec<DoubleEntry>(), count);
        }

        [TestCase(0)]
        [TestCase(1)]
        [TestCase(2)]
        [TestCase(1024)]
        [TestCase(1024 * 2)]
        [TestCase(1024 * 2 * 2)]
        [TestCase(1024 * 2 * 2 * 2)]
        public void MinDeltaIntCodecTest(int count)
        {
            CheckCodec(new MinDeltaInt32Codec(), count);
            CheckCodec(new MinDeltaInt64Codec(), count);
        }

        private static void CheckCodec<T>(ICodec<T> codec, int count) 
            where T : unmanaged
        {
            var data = Factory.CreateRandomBytes(count * sizeof(T));
            var encoded = new UnsafeBuffer(codec.GetMaxEncodedSize(data.Length));
            var decoded = new UnsafeBuffer(data.Length);

            int length;
            fixed(byte* p = data)
                length = codec.Encode((T*) p, count, encoded.Ptr);
            var result = codec.Decode(encoded.Ptr, length, (T*) decoded.Ptr);

            Assert.AreEqual(count, result, "Mismatch number of decoded data");
            data.Check(decoded.Data);

            decoded.Dispose();
            encoded.Dispose();
        }
    }
}
