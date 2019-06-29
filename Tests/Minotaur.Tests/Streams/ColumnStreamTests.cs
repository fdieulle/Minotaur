using System;
using System.Runtime.InteropServices;
using Minotaur.Codecs;
using Minotaur.Core;
using Minotaur.Pocs.Codecs;
using Minotaur.Pocs.Streams;
using Minotaur.Streams;
using NUnit.Framework;

namespace Minotaur.Tests.Streams
{
    [TestFixture]
    public unsafe class ColumnStreamTests
    {
        [SetUp]
        public void Setup()
        {
            OnSetup();
        }

        [TearDown]
        public void Teardown()
        {
            OnTeardown();
        }

        protected virtual void OnSetup() { }
        protected virtual void OnTeardown() { }

        protected virtual IStream CreateColumnStream<TEntry>(int bufferSize)
            where TEntry : unmanaged
            => new ColumnStream<TEntry>(
                new System.IO.MemoryStream(),
                new VoidCodec<TEntry>(),
                bufferSize);

        [Test]
        public void ReadWriteWorkflowTest()
        {
            var ms = new System.IO.MemoryStream();

            const int bufferSize = 1024;
            var wrapSize = sizeof(PayloadHeader) + sizeof(byte);
            var bufferSizeWithoutWrapSize = bufferSize - wrapSize;
            const int fullBufferSize = bufferSize * 5 + 512;

            var writeBuffer = new UnsafeBuffer(fullBufferSize);
            var readBuffer = new UnsafeBuffer(fullBufferSize);

            writeBuffer.SetAll(2);
            readBuffer.SetAll(0);

            var stream = new ColumnStream<byte>(ms, new VoidCodec<byte>(), bufferSize);

            // 1. Test write less than buffer
            var write = 100;
            var wrote = stream.Write(writeBuffer.Ptr, write);
            wrote.Check(write);
            ms.Position.Check(0);

            stream.Flush();
            ms.Position.Check(write + wrapSize);
            
            ReadWf(stream, readBuffer, fullBufferSize, 2, write);

            // 2. Test write more than buffer
            writeBuffer.SetAll(2);
            stream.Reset();
            ms.SetLength(0);

            write = bufferSize * 4 + 512;
            wrote = stream.Write(writeBuffer.Ptr, write);
            wrote.Check(write);
            ms.Position.Check((bufferSize + wrapSize) * 4);

            stream.Flush();
            ms.Position.Check(write + 5 * wrapSize);
            
            ReadWf(stream, readBuffer, fullBufferSize, 2, write);

            // 2. Test write exactly buffer size
            writeBuffer.SetAllUntil(bufferSizeWithoutWrapSize, 2);
            stream.Reset();
            ms.SetLength(0);

            write = bufferSize;
            wrote = stream.Write(writeBuffer.Ptr, write);
            wrote.Check(write);
            ms.Position.Check(bufferSize + wrapSize);

            stream.Flush();
            ms.Position.Check(bufferSize + wrapSize);
            stream.Reset();

            ReadWf(stream, readBuffer, fullBufferSize, 2, write);

            stream.Dispose();
            writeBuffer.Dispose();
            readBuffer.Dispose();
        }

        private static void ReadWf(IStream stream, UnsafeBuffer rData, int len, byte wVal, int wLen)
        {
            // Clean
            rData.SetAll(0);
            stream.Reset();

            // 1.1 Read more than wrote
            var read = stream.Read(rData.Ptr, wLen * 2);
            read.Check(wLen);
            rData.AllUntil(wLen - 1, wVal);
            rData.AllFrom(wLen, 0);

            // Clean
            rData.SetAll(0);
            stream.Reset();

            // 1.2 Read less than wrote and until after end
            var split = 7;
            var splitLen = wLen / split;
            for (var i = 0; i < split; i++)
            {
                read = stream.Read(rData.Ptr + (splitLen * i), splitLen);
                read.Check(splitLen);
                rData.AllUntil(splitLen * i - 1, wVal);
                rData.AllFrom(splitLen * (i + 1), 0);
            }

            var remaining = wLen - splitLen * split;
            read = stream.Read(rData.Ptr + splitLen * split, splitLen);
            read.Check(remaining);
            rData.AllUntil(wLen - 1, wVal);
            rData.AllFrom(wLen, 0);

            // Clean
            rData.SetAll(0);
            stream.Reset();

            // 1.3 Read exactly the neumber of byte wrote
            read = stream.Read(rData.Ptr, wLen);
            read.Check(wLen);
            rData.AllUntil(wLen - 1, wVal);
            rData.AllFrom(wLen, 0);

            read = stream.Read(rData.Ptr + wLen, wLen);
            read.Check(0);
            rData.AllUntil(wLen - 1, wVal);
            rData.AllFrom(wLen, 0);

            // Clean
            rData.SetAll(0);
            stream.Reset();
        }

        [Test]
        public void TimelineTicksTest()
            => CheckStream(p => Factory.CreateTimelineTicks(p));

        [Test]
        public void VoidCodecForInt32EntryTest()
            => CheckStream(p => Factory.CreateInt32Chunk(p));

        //[Test]
        //public void MinDelta32CodecForInt32Test()
        //    => CheckStream(p => Factory.CreateInt32Chunk(p), new MinDeltaInt32Codec());

        [Test]
        public void VoidCodecForDoubleEntryTest()
            => CheckStream(p => Factory.CreateDoubleChunk(p));

        [Test]
        public void VoidCodecForInt64EntryTest()
            => CheckStream(p => Factory.CreateInt64Chunk(p));

        [Test]
        public void VoidCodecForFloatEntryTest()
            => CheckStream(p => Factory.CreateFloatChunk(p));

        [Test]
        public void VoidCodecForStringEntryTest()
            => CheckStream(p => Factory.CreateStringChunk(p));

        private void CheckStream<T>(Func<int, T[]> factory, ICodec<T> codec = null)
            where T : unmanaged
        {
            const int bufferLength = 8192;

            var data = factory(bufferLength / sizeof(T) * 21);
            var entry = new byte[sizeof(T)];

            var handle = GCHandle.Alloc(data, GCHandleType.Pinned);
            try
            {
                var stream = codec == null 
                    ? CreateColumnStream<T>(bufferLength) 
                    : new ColumnStreamWithRetry<T, ICodec<T>>(
                        new System.IO.MemoryStream(),
                        codec);

                var pdata = (byte*)handle.AddrOfPinnedObject();

                stream.Write(pdata, data.Length * sizeof(T));
                stream.Flush();

                stream.Reset();

                var counter = 0;
                fixed (byte* p = entry)
                {
                    while (stream.Read(p, sizeof(T)) > 0)
                    {
                        for (var j = 0; j < sizeof(T); j++)
                            Assert.AreEqual(*(pdata + counter * sizeof(T) + j), *(p + j), $"At idx : {counter}, Byte: {j}");
                        counter++;
                    }
                }

                Assert.AreEqual(data.Length, counter);
            }
            finally
            {
                handle.Free();
            }
        }

        private static ICodecFullStream[] A(params ICodecFullStream[] array) => array;
    }
}
