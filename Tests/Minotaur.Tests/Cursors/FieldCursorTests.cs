using System;
using Minotaur.Codecs;
using Minotaur.Cursors;
using Minotaur.IO;
using Minotaur.Native;
using NUnit.Framework;
using MemoryStream = Minotaur.IO.MemoryStream;

namespace Minotaur.Tests.Cursors
{
    [TestFixture]
    public unsafe class FieldCursorTests
    {
        #region Float

        [Test]
        public void FloatCursorTest()
        {
            TestFloatEntryCursor(p => CreateCursor(p, sizeof(FloatEntry),
                (sPtr, bPtr, bLen, s) => new FloatCursor((byte*)sPtr, (byte*)bPtr, bLen, s)));
        }

        [Test]
        public void FloatCursorWithVoidCodecTest()
        {
            TestFloatEntryCursor(p => CreateCursor(p, new VoidCodec(),
                (sPtr, bPtr, bLen, s) => new FloatCursor((byte*)sPtr, (byte*)bPtr, bLen, s)));
        }

        #endregion

        #region Double

        [Test]
        public void DoubleCursorTest()
        {
            TestDoubleEntryCursor(p => CreateCursor(p, sizeof(DoubleEntry),
                (sPtr, bPtr, bLen, s) => new DoubleCursor((byte*)sPtr, (byte*)bPtr, bLen, s)));
        }

        [Test]
        public void DoubleCursorWithVoidCodecTest()
        {
            TestDoubleEntryCursor(p => CreateCursor(p, new VoidCodec(),
                (sPtr, bPtr, bLen, s) => new DoubleCursor((byte*)sPtr, (byte*)bPtr, bLen, s)));
        }

        #endregion

        #region Int32

        [Test]
        public void Int32CursorTest()
        {
            TestInt32EntryCursor(p => CreateCursor(p, sizeof(Int32Entry),
                (sPtr, bPtr, bLen, s) => new Int32Cursor((byte*)sPtr, (byte*)bPtr, bLen, s)));
        }

        [Test]
        public void Int32CursorWithVoidCodecTest()
        {
            TestInt32EntryCursor(p => CreateCursor(p, new VoidCodec(),
                (sPtr, bPtr, bLen, s) => new Int32Cursor((byte*)sPtr, (byte*)bPtr, bLen, s)));
        }

        #endregion

        #region Int64

        [Test]
        public void Int64CursorTest()
        {
            TestInt64EntryCursor(p => CreateCursor(p, sizeof(Int64Entry),
                (sPtr, bPtr, bLen, s) => new Int64Cursor((byte*)sPtr, (byte*)bPtr, bLen, s)));
        }

        [Test]
        public void Int64CursorWithVoidCodecTest()
        {
            TestInt64EntryCursor(p => CreateCursor(p, new VoidCodec(),
                (sPtr, bPtr, bLen, s) => new Int64Cursor((byte*)sPtr, (byte*)bPtr, bLen, s)));
        }

        #endregion

        protected static PinnedFieldCursor<T> CreateCursor<TEntry, T>(
            TEntry[] chunk, int entrySize, Func<IntPtr, IStream, IFieldCursor<T>> factory)
            where T : struct
        {
            var length = chunk.Length * entrySize;
            var memory = new MemoryStream(length);
            memory.WriteAndReset(chunk, entrySize);

            return new PinnedFieldCursor<T>(entrySize, p => factory(p, memory));
        }

        protected static PinnedFieldCursor<T> CreateCursor<TEntry, T>(
            TEntry[] chunk, int entrySize, Func<IntPtr, IntPtr, int, IStream, IFieldCursor<T>> factory)
            where T : struct
        {
            var length = chunk.Length * entrySize;
            var memory = new MemoryStream(length);
            memory.WriteAndReset(chunk, entrySize);

            var bufLen = Math.Max(2, chunk.Length / 3) * entrySize;
            return new PinnedFieldCursor<T>(entrySize, bufLen, (sPtr, bPtr, bLen) => factory(sPtr, bPtr, bLen, memory));
        }

        public static PinnedFieldCursor<T> CreateCursor<TEntry, T>(
            TEntry[] chunk, ICodec codec, Func<IntPtr, IStream, IFieldCursor<T>> factory)
            where T : struct
        {
            var columnStream = new PinnedColumnStream(
                new MemoryStream(), codec, 1024);
            columnStream.WriteAndReset(chunk, Natives.SizeOfEntry<TEntry>());

            return new PinnedFieldCursor<T>(Natives.SizeOfEntry<TEntry>(), p => factory(p, columnStream));
        }

        public static PinnedFieldCursor<T> CreateCursor<TEntry, T>(
            TEntry[] chunk, ICodec codec, Func<IntPtr, IntPtr, int, IStream, IFieldCursor<T>> factory)
            where T : struct
        {
            var columnStream = new PinnedColumnStream(
                new MemoryStream(), codec, 1024);
            columnStream.WriteAndReset(chunk, Natives.SizeOfEntry<TEntry>());

            var bufLen = Math.Max(2, chunk.Length / 3) * Natives.SizeOfEntry<TEntry>();
            return new PinnedFieldCursor<T>(Natives.SizeOfEntry<TEntry>(), bufLen, (sPtr, bPtr, bLen) => factory(sPtr, bPtr, bLen, columnStream));
        }

        protected static void TestFloatEntryCursor(Func<FloatEntry[], IFieldCursor<float>> factory)
        {
            TestCursor(p => p.ToFloat(), p => p.value, factory, float.NaN);
        }

        protected static void TestDoubleEntryCursor(Func<DoubleEntry[], IFieldCursor<double>> factory)
        {
            TestCursor(p => p, p => p.value, factory, double.NaN);
        }

        protected static void TestInt32EntryCursor(Func<Int32Entry[], IFieldCursor<int>> factory)
        {
            TestCursor(p => p.ToInt32(), p => p.value, factory);
        }

        protected static void TestInt64EntryCursor(Func<Int64Entry[], IFieldCursor<long>> factory)
        {
            TestCursor(p => p.ToInt64(), p => p.value, factory);
        }

        private static void TestCursor<TEntry, T>(
            Func<DoubleEntry[], TEntry[]> convert,
            Func<TEntry, T> getValue,
            Func<TEntry[], IFieldCursor<T>> factory,
            T defaultValue = default(T))
            where T : struct
        {
            const int iterations = 5;
            var chunk = new DoubleEntry[0]
                .Add(0, 12.2)
                .Add(4, 15.2)
                .Add(5, 16.2)
                .Add(6, 13.2)
                .Add(7, 14.2);

            var convertedChunk = convert(chunk);
            var cursor = factory(convertedChunk);
            // Test MoveNext
            for (var i = 0; i < iterations; i++)
            {
                Assert.AreEqual(getValue(convertedChunk[0]), cursor.GetNext(0));
                Assert.AreEqual(getValue(convertedChunk[0]), cursor.GetNext(1));
                Assert.AreEqual(getValue(convertedChunk[0]), cursor.GetNext(2));
                Assert.AreEqual(getValue(convertedChunk[1]), cursor.GetNext(4));
                Assert.AreEqual(getValue(convertedChunk[2]), cursor.GetNext(5));
                Assert.AreEqual(getValue(convertedChunk[3]), cursor.GetNext(6));
                Assert.AreEqual(getValue(convertedChunk[4]), cursor.GetNext(7));
                Assert.AreEqual(getValue(convertedChunk[4]), cursor.GetNext(8));
                Assert.AreEqual(getValue(convertedChunk[4]), cursor.GetNext(9));
                cursor.Reset();
            }

            cursor.Dispose();

            chunk.Set(0, 1, 12.2);
            cursor = factory(convert(chunk));
            for (var i = 0; i < iterations; i++)
            {
                Assert.AreEqual(defaultValue, cursor.GetNext(0));
                Assert.AreEqual(getValue(convertedChunk[0]), cursor.GetNext(1));
                Assert.AreEqual(getValue(convertedChunk[3]), cursor.GetNext(6));
                Assert.AreEqual(getValue(convertedChunk[4]), cursor.GetNext(7));
                cursor.Reset();
            }

            cursor.Dispose();
        }
    }
}
