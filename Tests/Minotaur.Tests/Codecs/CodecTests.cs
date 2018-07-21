using System;
using System.Runtime.InteropServices;
using Minotaur.Codecs;
using Minotaur.Native;
using NUnit.Framework;

namespace Minotaur.Tests.Codecs
{
    [TestFixture]
    public class CodecTests
    {
        [Test]
        public unsafe void TestNoCompressionCodec()
        {
            CheckCodec(new VoidCodec(), p => Factory.CreateDoubleChunk(p), sizeof(DoubleEntry), checkDecodeHeadInMove: false);
            CheckCodec(new VoidCodec(), p => Factory.CreateInt32Chunk(p), sizeof(Int32Entry), checkDecodeHeadInMove: false);
        }

        private static unsafe void CheckCodec<T>(ICodec codec, 
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
