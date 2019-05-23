using System;
using System.Runtime.InteropServices;
using Minotaur.Codecs;
using Minotaur.Native;
using Minotaur.Pocs.Codecs;
using NUnit.Framework;

namespace Minotaur.Tests.Codecs
{
    [TestFixture]
    public class CodecTests
    {
        [Test]
        public unsafe void TestEncodeDecodeUInt64()
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
        public unsafe void TestEncodeDecodeInt64()
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
        public unsafe void TestEncodeDecodeUInt32()
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
        public unsafe void TestEncodeDecodeInt32()
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
            for (var i = 63; i < values.Length; i++)
            {
                var p = buffer;
                Codec.EncodeInt32(values[i], ref p);

                p = buffer;
                var value = Codec.DecodeInt32(ref p);
                Assert.AreEqual(values[i], value, "At idx: " + i);
            }
        }

        [Test]
        public unsafe void TestEncodeDecodeFlagedInt32()
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
            for (var i = 63; i < values.Length; i++)
            {
                var p = buffer;
                Codec.EncodeInt32(true, values[i], ref p);

                p = buffer;
                var value = Codec.DecodeInt32(ref p, out var flag);
                Assert.AreEqual(values[i], value, "At idx: " + i);
                Assert.IsTrue(flag);
            }

            for (var i = 63; i < values.Length; i++)
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
        public unsafe void TestNoCompressionCodec()
        {
            CheckCodec(new VoidCodecFullStream(), p => Factory.CreateDoubleChunk(p), sizeof(DoubleEntry), checkDecodeHeadInMove: false);
            CheckCodec(new VoidCodecFullStream(), p => Factory.CreateInt32Chunk(p), sizeof(Int32Entry), checkDecodeHeadInMove: false);
        }

        private static unsafe void CheckCodec<T>(ICodecFullStream codec, 
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
    }
}
