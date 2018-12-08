using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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

            var mockStreamFactory = Substitute.For<IStreamFactory<Win32>>();
            var streams = columnNames.ToDictionary(p => p, p => new MockStream());
            foreach (var pair in streams)
            {
                mockStreamFactory.Create(Arg.Is<FileMetaData>(p => p.Column == pair.Key))
                    .Returns(pair.Value);
            }

            var mockFilePahtProvider = Substitute.For<IFilePathProvider>();
            mockFilePahtProvider.GetFilePath(Arg.Is<string>(p => p == symbol), Arg.Any<string>(), Arg.Any<DateTime>())
                .Returns(p => $"{p.ArgAt<string>(0)}_{p.ArgAt<string>(1)}_{p.ArgAt<DateTime>(2):yyyy-MM-dd_HH:mm:ss.fff}.min");

            var recorder = new TimeSeriesRecorder<Win32>(symbol, mockStreamFactory, mockFilePahtProvider);

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
                mockStreamFactory.Received(1).Create(Arg.Is<FileMetaData>(p =>
                    p.Column == pair.Key &&
                    p.Symbol == symbol &&
                    p.Start == now.ToDateTime() && 
                    pair.Key.StartsWith(p.Type.ToString()) &&
                    p.FilePath == mockFilePahtProvider.GetFilePath(symbol, pair.Key, p.Start)));
                pair.Value.CheckCallsThenClear();
            }
            mockStreamFactory.ClearReceivedCalls();

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

            foreach (var pair in streams)
            {
                mockStreamFactory.DidNotReceive().Create(Arg.Any<FileMetaData>());
                pair.Value.CheckCallsThenClear();
            }
            mockStreamFactory.ClearReceivedCalls();

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

            foreach (var pair in streams)
            {
                mockStreamFactory.DidNotReceive().Create(Arg.Any<FileMetaData>());
                pair.Value.CheckCallsThenClear();
            }
            mockStreamFactory.ClearReceivedCalls();

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

            foreach (var pair in streams)
            {
                mockStreamFactory.DidNotReceive().Create(Arg.Any<FileMetaData>());
                pair.Value.CheckCallsThenClear();
            }
            mockStreamFactory.ClearReceivedCalls();

            streams[intColumn1].CheckData(E(now, 12), E(now, 13), E(now, 14));
            streams[intColumn2].CheckNoData();
            streams[intColumn3].CheckNoData();
            streams[longColumn].CheckNoData();
            streams[floatColumn].CheckNoData();
            streams[doubleColumn].CheckNoData();

            var meta = recorder.MetaData.ToList();
            Assert.AreEqual(columnNames.Length, meta.Count);

            meta[0].Check(symbol, intColumn1, FieldType.Int32, "08:00:00", "08:00:03");
            meta[1].Check(symbol, intColumn2, FieldType.Int32, "08:00:00", "08:00:02");
            meta[2].Check(symbol, intColumn3, FieldType.Int32, "08:00:00", "08:00:01");
            meta[3].Check(symbol, longColumn, FieldType.Int64, "08:00:00", "08:00:01");
            meta[4].Check(symbol, floatColumn, FieldType.Float, "08:00:00", "08:00:02");
            meta[5].Check(symbol, doubleColumn, FieldType.Double, "08:00:00", "08:00:01");
        }

        [Test]
        public void RollStreamTest()
        {
            const string symbol = "Symbol";
            const string column = "Column";

            var mockStreamFactory = Substitute.For<IStreamFactory<Win32>>();
            var mockStream1 = new MockStream(3);
            mockStreamFactory.Create(Arg.Any<FileMetaData>())
                .Returns(mockStream1);

            var mockFilePahtProvider = Substitute.For<IFilePathProvider>();
            mockFilePahtProvider.GetFilePath(Arg.Is<string>(p => p == symbol), Arg.Any<string>(), Arg.Any<DateTime>())
                .Returns(p => $"{p.ArgAt<string>(0)}_{p.ArgAt<string>(1)}_{p.ArgAt<DateTime>(2):yyyy-MM-dd_HH:mm:ss.fff}.min");

            var recorder = new TimeSeriesRecorder<Win32>(symbol, mockStreamFactory, mockFilePahtProvider);

            var now = "08:00:00";

            recorder.AddRow(now)
                .Record(column, 1)
                .Record(column, 2);

            mockStreamFactory.Received(1).Create(Arg.Is<FileMetaData>(p =>
                p.Column == column &&
                p.Symbol == symbol &&
                p.Start == now.ToDateTime() &&
                p.Type == FieldType.Int32 &&
                p.FilePath == mockFilePahtProvider.GetFilePath(symbol, column, p.Start)));
            mockStreamFactory.ClearReceivedCalls();
            mockStream1.CheckCallsThenClear();

            var mockStream2 = new MockStream(3);
            mockStreamFactory.Create(Arg.Any<FileMetaData>())
                .Returns(mockStream2);

            now = "08:00:01";

            recorder.AddRow(now)
                .Record(column, 3)
                .Record(column, 4)
                .Record(column, 5);

            mockStreamFactory.Received(1).Create(Arg.Is<FileMetaData>(p =>
                p.Column == column &&
                p.Symbol == symbol &&
                p.Start == now.ToDateTime() &&
                p.Type == FieldType.Int32 &&
                p.FilePath == mockFilePahtProvider.GetFilePath(symbol, column, p.Start)));
            mockStreamFactory.ClearReceivedCalls();
            mockStream1.CheckCallsThenClear(1);
            mockStream2.CheckCallsThenClear();

            mockStream1.CheckData(E("08:00:00", 1), E("08:00:00", 2), E("08:00:01", 3));
            mockStream2.CheckData(E("08:00:01", 4), E("08:00:01", 5));

            var meta = recorder.MetaData.ToList();
            Assert.AreEqual(2, meta.Count);

            now = "08:00:02";
            recorder.AddRow(now)
                .Record(column, 6);

            meta[0].Check(symbol, column, FieldType.Int32, "08:00:00", "08:00:01");
            meta[1].Check(symbol, column, FieldType.Int32, "08:00:01", "08:00:02");
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

        private class MockStream : IStream
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
                if(data.Length == 0) Assert.AreEqual(0, _data.Count, "Data queue isn't empty");
                fixed (T* y = data)
                {
                    var py = (byte*) y;
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

            public int Seek(int seek, SeekOrigin origin)
            {
                throw new NotImplementedException();
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
