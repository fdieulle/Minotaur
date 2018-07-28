using System;
using System.IO;
using System.Runtime.InteropServices;
using Minotaur.Codecs;
using Minotaur.IO;
using Minotaur.Native;
using NUnit.Framework;
using MemoryStream = Minotaur.IO.MemoryStream;

namespace Minotaur.Tests.IO
{
    [TestFixture]
    public unsafe class ColumnStreamTests
    {
        [Test]
        public void ReadWriteWorkflowTest()
        {
            
            var ms = new MemoryStream();
            const int bufferSize = 1024;
            const int wrapSize = sizeof(int) * 2 + sizeof(byte);
            const int bufferSizeWithoutWrapSize = bufferSize - wrapSize;
            const int fullBufferSize = bufferSize * 5 + 512;

            var buffer = Marshal.AllocHGlobal(bufferSize);
            var wData = Marshal.AllocHGlobal(fullBufferSize);
            var rData = Marshal.AllocHGlobal(fullBufferSize);

            
            wData.SetAll(fullBufferSize, 2);
            rData.SetAll(fullBufferSize, 0);

            var stream = new ColumnStream<MemoryStream, VoidCodec>(ms, new VoidCodec(), (byte*)buffer, bufferSize);

            // 1. Test write less than buffer
            var write = 100;
            var wrote = stream.Write((byte*)wData, write);
            wrote.Check(write);
            ms.Position.Check(0);

            stream.Flush();
            ms.Position.Check(bufferSize);
            
            ReadWf(stream, rData, fullBufferSize, 2, write);

            // 2. Test write more than buffer
            wData.SetAll(fullBufferSize, 2);
            stream.Reset();
            ms.SetLength(0);

            write = bufferSize * 4 + 512;
            wrote = stream.Write((byte*)wData, write);
            wrote.Check(write);
            ms.Position.Check(bufferSize * 4);

            stream.Flush();
            ms.Position.Check(bufferSize * 5);
            
            ReadWf(stream, rData, fullBufferSize, 2, write);

            // 2. Test write exactly buffer size
            wData.SetAll(bufferSizeWithoutWrapSize, 2);
            stream.Reset();
            ms.SetLength(0);

            write = bufferSizeWithoutWrapSize;
            wrote = stream.Write((byte*)wData, write);
            wrote.Check(write);
            ms.Position.Check(bufferSize);

            stream.Flush();
            ms.Position.Check(bufferSize);
            stream.Reset();

            ReadWf(stream, rData, fullBufferSize, 2, write);

            Marshal.FreeHGlobal(buffer);
            Marshal.FreeHGlobal(wData);
            Marshal.FreeHGlobal(rData);
        }

        private static void ReadWf<TStream>(TStream stream, IntPtr rData, int len, byte wVal, int wLen)
            where TStream : IStream
        {
            // Clean
            rData.SetAll(len, 0);
            stream.Reset();

            // 1.1 Read more than wrote
            var read = stream.Read((byte*)rData, wLen * 2);
            read.Check(wLen);
            rData.CheckAll(wLen, wVal);
            rData.CheckAll(len - wLen, 0, wLen);

            // Clean
            rData.SetAll(len, 0);
            stream.Reset();

            // 1.2 Read less than wrote and until after end
            var split = 7;
            var splitLen = wLen / split;
            for (var i = 0; i < split; i++)
            {
                read = stream.Read(((byte*)rData) + (splitLen * i), splitLen);
                read.Check(splitLen);
                rData.CheckAll(splitLen * i, wVal);
                rData.CheckAll(len - splitLen * (i + 1), 0, splitLen * (i + 1));
            }

            var remaining = wLen - splitLen * split;
            read = stream.Read((byte*)rData + splitLen * split, splitLen);
            read.Check(remaining);
            rData.CheckAll(wLen, wVal);
            rData.CheckAll(len - wLen, 0, wLen);

            // Clean
            rData.SetAll(len, 0);
            stream.Reset();

            // 1.3 Read exactly the neumber of byte wrote
            read = stream.Read((byte*)rData, wLen);
            read.Check(wLen);
            rData.CheckAll(wLen, wVal);
            rData.CheckAll(len - wLen, 0, wLen);

            read = stream.Read((byte*)rData + wLen, wLen);
            read.Check(0);
            rData.CheckAll(wLen, wVal);
            rData.CheckAll(len - wLen, 0, wLen);

            // Clean
            rData.SetAll(len, 0);
            stream.Reset();
        }

        [Test]
        public void TimelineTicksTest()
        {
            CheckStream(new VoidCodec(), p => Factory.CreateTimelineTicks(p), sizeof(long));
        }

        [Test]
        public void VoidCodecForInt32EntryTest()
        {
            CheckStream(new VoidCodec(), p => Factory.CreateInt32Chunk(p), sizeof(Int32Entry));
        }

        [Test]
        public void VoidCodecForDoubleEntryTest()
        {
            CheckStream(new VoidCodec(), p => Factory.CreateDoubleChunk(p), sizeof(DoubleEntry));
        }

        [Test]
        public void VoidCodecForInt64EntryTest()
        {
            CheckStream(new VoidCodec(), p => Factory.CreateInt64Chunk(p), sizeof(Int64Entry));
        }

        [Test]
        public void VoidCodecForFloatEntryTest()
        {
            CheckStream(new VoidCodec(), p => Factory.CreateFloatChunk(p), sizeof(FloatEntry));
        }

        [Test]
        public void VoidCodecForStringEntryTest()
        {
            CheckStream(new VoidCodec(), p => Factory.CreateStringChunk(p), sizeof(StringEntry));
        }

        private static void CheckStream<T>(ICodec codec, Func<int, T[]> factory, int sizeOfT)
            where  T : struct
        {
            var memory = new MemoryStream();
            
            const int bufferLength = 8192;
            var buffer = Marshal.AllocHGlobal(bufferLength);
            
            var data = factory(bufferLength / sizeOfT * 21);
            var entry = new byte[sizeOfT];

            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                var stream = new ColumnStream<MemoryStream, ICodec>(
                    memory, codec, (byte*)buffer, bufferLength);

                var pdata = (byte*)handle.AddrOfPinnedObject();

                stream.Write(pdata, data.Length * sizeOfT);
                stream.Flush();

                stream.Reset();

                var counter = 0;
                fixed (byte* p = entry)
                {
                    while (stream.Read(p, sizeOfT) > 0)
                    {
                        for (var j = 0; j < sizeOfT; j++)
                            Assert.AreEqual(*(pdata + counter * sizeOfT + j), *(p + j), $"At idx : {counter}, Byte: {j}");
                        counter++;
                    }
                }

                Assert.AreEqual(data.Length, counter);
            }
            finally
            {
                handle.Free();
                Marshal.FreeHGlobal(buffer);
            }
        }

        private static ICodec[] A(params ICodec[] array)
        {
            return array;
        }
    }
}
