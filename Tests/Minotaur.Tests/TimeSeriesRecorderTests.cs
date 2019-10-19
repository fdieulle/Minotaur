using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Minotaur.Core;
using Minotaur.Core.Platform;
using Minotaur.Native;
using Minotaur.Providers;
using Minotaur.Recorders;
using Minotaur.Streams;
using NSubstitute;
using NUnit.Framework;

namespace Minotaur.Tests
{
    [TestFixture]
    public unsafe class TimeSeriesRecorderTests
    {
        [Test]
        public void RecordTimelineWithManyColumns()
        {
            const string symbol = "Symbol";
            const string intColumn1 = "Int32Column1";
            const string intColumn2 = "Int32Column2";
            const string intColumn3 = "Int32Column3";
            const string longColumn = "Int64Column";
            const string floatColumn = "FloatColumn";
            const string doubleColumn = "DoubletColumn";

            var columnNames = new[]
            {
                intColumn1, intColumn2, intColumn3,
                longColumn, floatColumn, doubleColumn
            };

            var mockDbUpdater = Substitute.For<ITimeSeriesDbUpdater>();
            var streams = columnNames.ToDictionary(p => p, p => new MockStream());
            foreach (var pair in streams)
            {
                mockDbUpdater.CreateColumnWriter(
                        Arg.Any<string>(), 
                        Arg.Is<ColumnInfo>(i => i.Name == pair.Key), 
                        Arg.Any<DateTime>())
                    .Returns(streams[pair.Key]);
            }
            var allocator = new DummyPinnedAllocator();

            var recorder = new TimeSeriesRecorder(
                symbol,
                mockDbUpdater,
                allocator);

            var now = "08:00:00";

            recorder.AddRow(now)
                .Record(intColumn1, 1)
                .Record(intColumn2, 2)
                .Record(intColumn3, 3)
                .Record(longColumn, 100L)
                .Record(floatColumn, 1.23f)
                .Record(doubleColumn, 7.89);

            foreach (var pair in streams)
            {
                var timestamp = now.ToDateTime();

                mockDbUpdater.Received(1).CreateColumnWriter(
                    Arg.Is<string>(i => i == symbol),
                    Arg.Is<ColumnInfo>(i => i.Name == pair.Key),
                    Arg.Is<DateTime>(i => i == timestamp));

                pair.Value.CheckCallsThenClear();
            }
            mockDbUpdater.ClearReceivedCalls();

            streams[intColumn1].CheckData(E(now, 1));
            streams[intColumn2].CheckData(E(now, 2));
            streams[intColumn3].CheckData(E(now, 3));
            streams[longColumn].CheckData(E(now, 100L));
            streams[floatColumn].CheckData(E(now, 1.23f));
            streams[doubleColumn].CheckData(E(now, 7.89));

            // Partial update
            now = "08:00:01";
            recorder.AddRow(now)
                .Record(intColumn1, 10)
                .Record(intColumn3, 30)
                .Record(longColumn, 101L)
                .Record(doubleColumn, 7.99);

            streams[intColumn1].CheckData(E(now, 10));
            streams[intColumn2].CheckNoData();
            streams[intColumn3].CheckData(E(now, 30));
            streams[longColumn].CheckData(E(now, 101L));
            streams[floatColumn].CheckNoData();
            streams[doubleColumn].CheckData(E(now, 7.99));

            // Partial update 
            now = "08:00:02";
            recorder.AddRow(now)
                .Record(intColumn1, 11)
                .Record(intColumn2, 21)
                .Record(floatColumn, 1.24f);

            streams[intColumn1].CheckData(E(now, 11));
            streams[intColumn2].CheckData(E(now, 21));
            streams[intColumn3].CheckNoData();
            streams[longColumn].CheckNoData();
            streams[floatColumn].CheckData(E(now, 1.24f));
            streams[doubleColumn].CheckNoData();

            // Many ticks on the same timestamp
            now = "08:00:03";
            recorder.AddRow(now)
                .Record(intColumn1, 12)
                .Record(intColumn1, 13)
                .Record(intColumn1, 14);

            streams[intColumn1].CheckData(E(now, 12), E(now, 13), E(now, 14));
            streams[intColumn2].CheckNoData();
            streams[intColumn3].CheckNoData();
            streams[longColumn].CheckNoData();
            streams[floatColumn].CheckNoData();
            streams[doubleColumn].CheckNoData();

            foreach (var pair in streams)
                pair.Value.CheckCallsThenClear();
            mockDbUpdater.DidNotReceive().CreateColumnWriter(
                Arg.Any<string>(),
                Arg.Any<ColumnInfo>(),
                Arg.Any<DateTime>());
            mockDbUpdater.DidNotReceive().CommitColumn(
                Arg.Any<string>(),
                Arg.Any<ColumnInfo>(),
                Arg.Any<DateTime>(),
                Arg.Any<DateTime>());
            mockDbUpdater.DidNotReceive().RevertColumn(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<DateTime>());

            recorder.Commit();

            foreach (var pair in streams)
                pair.Value.CheckCallsThenClear(1, 0, 1);
            Check(mockDbUpdater, symbol, intColumn1, FieldType.Int32, "08:00:00", "08:00:03");
            Check(mockDbUpdater, symbol, intColumn2, FieldType.Int32, "08:00:00", "08:00:02");
            Check(mockDbUpdater, symbol, intColumn3, FieldType.Int32, "08:00:00", "08:00:01");
            Check(mockDbUpdater, symbol, longColumn, FieldType.Int64, "08:00:00", "08:00:01");
            Check(mockDbUpdater, symbol, floatColumn, FieldType.Float, "08:00:00", "08:00:02");
            Check(mockDbUpdater, symbol, doubleColumn, FieldType.Double, "08:00:00", "08:00:01");

            mockDbUpdater.Received(6).CommitColumn(
                Arg.Any<string>(),
                Arg.Any<ColumnInfo>(),
                Arg.Any<DateTime>(),
                Arg.Any<DateTime>());
            mockDbUpdater.DidNotReceive().CreateColumnWriter(
                Arg.Any<string>(),
                Arg.Any<ColumnInfo>(),
                Arg.Any<DateTime>());
            mockDbUpdater.DidNotReceive().RevertColumn(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<DateTime>());
        }

        [Test]
        public void CreateStreamTest()
        {
            const string symbol = "Symbol";
            const string column = "Column";

            var mockStream1 = new MockStream(3);
            var mockDbUpdater = Substitute.For<ITimeSeriesDbUpdater>();
            mockDbUpdater.CreateColumnWriter(Arg.Any<string>(), Arg.Any<ColumnInfo>(), Arg.Any<DateTime>())
                .Returns(mockStream1);
            var allocator = new DummyPinnedAllocator();

            var recorder = new TimeSeriesRecorder(
                symbol,
                mockDbUpdater,
                allocator);

            var now = "08:00:00";

            recorder.AddRow(now)
                .Record(column, 1)
                .Record(column, 2);

            mockDbUpdater.Received(1)
                .CreateColumnWriter(
                    Arg.Is<string>(i => i == symbol),
                    Arg.Is<ColumnInfo>(i => i.Name == column && i.Type == FieldType.Int32),
                    Arg.Is<DateTime>(i => i == "08:00:00".ToDateTime()));

            mockDbUpdater.ClearReceivedCalls();
            mockStream1.CheckCallsThenClear();

            now = "08:00:01";

            recorder.AddRow(now)
                .Record(column, 3)
                .Record(column, 4)
                .Record(column, 5);

            mockDbUpdater.ClearReceivedCalls();
            mockStream1.CheckCallsThenClear();

            mockStream1.CheckData(E("08:00:00", 1), E("08:00:00", 2), E("08:00:01", 3), E("08:00:01", 4), E("08:00:01", 5));

            now = "08:00:02";
            recorder.AddRow(now)
                .Record(column, 6);

            recorder.Commit();
            mockStream1.CheckCallsThenClear(1, 0, 1);
            mockDbUpdater.Received(1).CommitColumn(
                Arg.Is<string>(i => i == symbol),
                Arg.Is<ColumnInfo>(i => i.Name == column && i.Type == FieldType.Int32),
                Arg.Is<DateTime>(i => i == "08:00:00".ToDateTime()),
                Arg.Is<DateTime>(i => i == "08:00:02".ToDateTime()));
        }

        private static void Check(ITimeSeriesDbUpdater mock, string symbol, string column, FieldType type, string start, string end)
        {
            mock.Received(1).CommitColumn(
                Arg.Is<string>(i => i == symbol),
                Arg.Is<ColumnInfo>(i => i.Name == column && i.Type == type),
                Arg.Is<DateTime>(i => i == start.ToDateTime()),
                Arg.Is<DateTime>(i => i == end.ToDateTime()));
        }

        [DebuggerStepThrough]
        private static Int32Entry E(string timestamp, int value)
        {
            return new Int32Entry
            {
                ticks = timestamp.ToDateTime().Ticks,
                value = value,
            };
        }

        [DebuggerStepThrough]
        private static Int64Entry E(string timestamp, long value)
        {
            return new Int64Entry
            {
                ticks = timestamp.ToDateTime().Ticks,
                value = value,
            };
        }

        [DebuggerStepThrough]
        private static FloatEntry E(string timestamp, float value)
        {
            return new FloatEntry
            {
                ticks = timestamp.ToDateTime().Ticks,
                value = value,
            };
        }

        [DebuggerStepThrough]
        private static DoubleEntry E(string timestamp, double value)
        {
            return new DoubleEntry
            {
                ticks = timestamp.ToDateTime().Ticks,
                value = value,
            };
        }

        private class MockStream : IColumnStream
        {
            private readonly int _maxCount;
            private readonly Queue<byte[]> _data = new Queue<byte[]>();
            private int _flushCallCount;
            private int _resetCallCount;
            private int _disposeCallCount;

            public MockStream(int maxCount = int.MaxValue)
            {
                _maxCount = maxCount;
            }

            public void CheckData<T>(params T[] data) where T : unmanaged
            {
                if (data.Length == 0) Assert.AreEqual(0, _data.Count, "Data queue isn't empty");
                fixed (T* y = data)
                {
                    var py = (byte*)y;
                    var j = 0;
                    var x = _data.Dequeue();
                    Assert.AreEqual(sizeof(T), x.Length, "Size of data");
                    fixed (byte* px = x)
                    {
                        for (var i = 0; i < sizeof(T); i++, j++)
                        {
                            Assert.AreEqual(*(py + j), *(px + i));
                        }
                    }
                }
            }

            public void CheckNoData()
            {
                Assert.AreEqual(0, _data.Count, "Data queue isn't empty");
            }

            public void CheckCallsThenClear(int flush = 0, int reset = 0, int dispose = 0)
            {
                Assert.AreEqual(flush, _flushCallCount, "Flush method calls count");
                Assert.AreEqual(reset, _resetCallCount, "Flush method calls count");
                Assert.AreEqual(dispose, _disposeCallCount, "Flush method calls count");

                _flushCallCount = 0;
                _resetCallCount = 0;
                _disposeCallCount = 0;
            }

            #region Implementation of IStream

            public int Read(byte* p, int length)
            {
                throw new NotImplementedException();
            }

            public int Write(byte* p, int length)
            {
                if (_data.Count >= _maxCount) return 0;

                var data = new byte[length];
                for (var i = 0; i < length; i++)
                    data[i] = *(p + i);
                _data.Enqueue(data);

                return length;
            }

            public void Reset()
            {
                _resetCallCount++;
            }

            public void Flush()
            {
                _flushCallCount++;
            }

            #endregion

            #region Implementation of IDisposable

            public void Dispose()
            {
                _disposeCallCount++;
            }

            #endregion
        }
    }
}
