using System;
using System.Runtime.InteropServices;
using Minotaur.Codecs;
using Minotaur.Core;
using Minotaur.Cursors;
using Minotaur.Native;
using Minotaur.Streams;
using NUnit.Framework;
using MemoryStream = Minotaur.Streams.MemoryStream;

namespace Minotaur.Tests.Cursors
{
    [TestFixture]
    public unsafe class FieldCursorTests
    {
        #region Float

        [Test]
        public void FloatCursorTest()
        {
            TestFloatEntryCursor(CreateCursor<FloatEntry, float>);
        }

        [Test]
        public void FloatCursorWithVoidCodecTest()
        {
            TestFloatEntryCursor(p => CreateCursor<FloatEntry, float>(p, new VoidCodec()));
        }

        #endregion

        #region Double

        [Test]
        public void DoubleCursorTest()
        {
            TestDoubleEntryCursor(CreateCursor<DoubleEntry, double>);
        }

        [Test]
        public void DoubleCursorWithVoidCodecTest()
        {
            TestDoubleEntryCursor(p => CreateCursor<DoubleEntry, double>(p, new VoidCodec()));
        }

        #endregion

        #region Int32

        [Test]
        public void Int32CursorTest()
        {
            TestInt32EntryCursor(CreateCursor<Int32Entry, int>);
        }

        [Test]
        public void Int32CursorWithVoidCodecTest()
        {
            TestInt32EntryCursor(p => CreateCursor<Int32Entry, int>(p, new VoidCodec()));
        }

        #endregion

        #region Int64

        [Test]
        public void Int64CursorTest()
        {
            TestInt64EntryCursor(CreateCursor<Int64Entry, long>);
        }

        [Test]
        public void Int64CursorWithVoidCodecTest()
        {
            TestInt64EntryCursor(p => CreateCursor<Int64Entry, long>(p, new VoidCodec()));
        }

        #endregion

        private static IColumnCursor<T> CreateCursor<TEntry, T>(TEntry[] chunk)
            where TEntry : unmanaged, IFieldEntry<T>
        {
            var memory = new MemoryStream(chunk.Length * sizeof(TEntry));
            memory.WriteAndReset(chunk, sizeof(TEntry));

            return new ColumnCursor<TEntry, T, IStream>(new DummyPinnedAllocator(), memory);
        }

        public static IColumnCursor<T> CreateCursor<TEntry, T>(TEntry[] chunk, ICodec codec)
            where TEntry : unmanaged, IFieldEntry<T>
        {
            var columnStream = new PinnedColumnStream(
                new MemoryStream(), codec, 1024);
            columnStream.WriteAndReset(chunk, Natives.SizeOfEntry<TEntry>());

            return new ColumnCursor<TEntry, T, IStream>(new DummyPinnedAllocator(), columnStream);
        }

        protected static void TestFloatEntryCursor(Func<FloatEntry[], IColumnCursor<float>> factory)
        {
            TestCursor(p => p.ToFloat(), p => p.value, factory, float.NaN);
        }

        protected static void TestDoubleEntryCursor(Func<DoubleEntry[], IColumnCursor<double>> factory)
        {
            TestCursor(p => p, p => p.value, factory, double.NaN);
        }

        protected static void TestInt32EntryCursor(Func<Int32Entry[], IColumnCursor<int>> factory)
        {
            TestCursor(p => p.ToInt32(), p => p.value, factory);
        }

        protected static void TestInt64EntryCursor(Func<Int64Entry[], IColumnCursor<long>> factory)
        {
            TestCursor(p => p.ToInt64(), p => p.value, factory);
        }

        private static void TestCursor<TEntry, T>(
            Func<DoubleEntry[], TEntry[]> convert,
            Func<TEntry, T> getValue,
            Func<TEntry[], IColumnCursor<T>> factory,
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
