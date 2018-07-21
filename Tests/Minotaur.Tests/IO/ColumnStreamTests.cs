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
                const int nbWriteBlocks = 5;

                var pw = pdata;
                for (var i = 0; i <= nbWriteBlocks; i++)
                {
                    var l = Math.Min(data.Length / nbWriteBlocks * sizeOfT, data.Length * sizeOfT - (pw - pdata));
                    pw += stream.Write(pw, (int)l);
                }

                memory.Seek(0, SeekOrigin.Begin);

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
