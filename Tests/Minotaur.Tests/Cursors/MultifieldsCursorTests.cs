using System;
using System.Collections.Generic;
using Minotaur.Codecs;
using Minotaur.Core;
using Minotaur.Cursors;
using Minotaur.Native;
using Minotaur.Pocs.Codecs;
using Minotaur.Pocs.Streams;
using Minotaur.Streams;
using NUnit.Framework;

namespace Minotaur.Tests.Cursors
{
    [TestFixture]
    public unsafe class MultifieldsCursorTests
    {
        [Test]
        public void MoveNextTest()
        {
            var bid = new List<DoubleEntry>()
                .Add("08:00:00", 12.2)
                .Add("08:00:01", 12.3)
                .Add("08:00:02", 12.4)
                .Add("08:00:03", 12.5)
                .Add("08:00:04", 12.6)
                .ToArray();
            var bsize = new List<Int32Entry>()
                .Add("08:00:00", 1e6)
                .Add("08:00:02", 2e6)
                .ToArray();

            var ask = new List<DoubleEntry>()
                .Add("08:00:00", 13.2)
                .Add("08:00:02", 13.3)
                .Add("08:00:04", 13.4)
                .ToArray();
            var asize = new List<Int32Entry>()
                .Add("08:00:00", 2e6)
                .Add("08:00:04", 1e6)
                .ToArray();

            var snapshots = new SnapshotTimeSerie(1, 2, 3, 4)
                .Snap("07:59:58", "Min", "08:00:00", double.NaN, 0, double.NaN, 0)
                .Snap("07:59:59", "Min", "08:00:00", double.NaN, 0, double.NaN, 0)
                .Snap("08:00:00", "08:00:00", "08:00:01", 12.2, 1e6, 13.2, 2e6)
                .Snap("08:00:00.500", "08:00:00", "08:00:01", 12.2, 1e6, 13.2, 2e6)
                .Snap("08:00:00.700", "08:00:00", "08:00:01", 12.2, 1e6, 13.2, 2e6)
                .Snap("08:00:01.200", "08:00:01", "08:00:02", 12.3, 1e6, 13.2, 2e6)
                .Snap("08:00:03.900", "08:00:03", "08:00:04", 12.5, 2e6, 13.3, 2e6)
                .Snap("08:00:04", "08:00:04", "Max", 12.6, 2e6, 13.4, 1e6)
                .Snap("08:00:05", "08:00:04", "Max", 12.6, 2e6, 13.4, 1e6)
                .Snap("08:00:06", "08:00:04", "Max", 12.6, 2e6, 13.4, 1e6);

            var allocator = new DummyUnmanagedAllocator();
            const int blockSize = 8192;

            try
            {


                #region Prepare streams

                // Create streams
                var streams = new Dictionary<int, IStream>
                {
                    {1, CreateColumnStream(new VoidCodecFullStream(), allocator, blockSize)},
                    {2, CreateColumnStream(new VoidCodecFullStream(), allocator, blockSize)},
                    {3, CreateColumnStream(new VoidCodecFullStream(), allocator, blockSize)},
                    {4, CreateColumnStream(new VoidCodecFullStream(), allocator, blockSize)}
                };

                // Fill streams
                streams[1].Write(bid);
                streams[2].Write(bsize);
                streams[3].Write(ask);
                streams[4].Write(asize);

                // Reset streams
                foreach (var stream in streams.Values)
                {
                    stream.Flush();
                    stream.Reset();
                }

                #endregion

                var fieldCursors = new Dictionary<string, IColumnCursor>
                {
                    {"1", new ColumnCursor<DoubleEntry, double, IStream>(allocator, streams[1])},
                    {"2", new ColumnCursor<Int32Entry, int, IStream>(allocator, streams[2])},
                    {"3", new ColumnCursor<DoubleEntry, double, IStream>(allocator, streams[3])},
                    {"4", new ColumnCursor<Int32Entry, int, IStream>(allocator, streams[4])},
                };

                var cursor = new TimeSeriesCursor<IStream>(fieldCursors);

                snapshots.RunMoveNext(cursor);
            }
            finally
            {
                allocator.Dispose();
            }
        }

        protected static IStream CreateColumnStream(ICodecFullStream codec, IAllocator allocator, int bufLen)
        {
            return new ColumnStreamFullStream<MemoryStream, ICodecFullStream>(
                new MemoryStream(),
                codec,
                allocator,
                bufLen);
        }

        #region Helpers

        protected class SnapshotTimeSerie
        {
            private readonly int _bidId;
            private readonly int _bsizeId;
            private readonly int _askId;
            private readonly int _asizeId;
            private readonly List<Snapshot> _snapshots = new List<Snapshot>();

            public SnapshotTimeSerie(int bidId, int bsizeId, int askId, int asizeId)
            {
                _bidId = bidId;
                _bsizeId = bsizeId;
                _askId = askId;
                _asizeId = asizeId;
            }

            public SnapshotTimeSerie Snap(string time, string currentTime, string nextTime,
                double bid, double bsize, double ask, double asize)
            {
                _snapshots.Add(new Snapshot
                {
                    RequestTimestamp = time.ToDateTime(),
                    CurrentTimestamp = currentTime.ToDateTime(),
                    NextTimestamp = nextTime.ToDateTime(),
                    Bid = bid,
                    Ask = ask,
                    BSize = (int)bsize,
                    ASize = (int)asize
                });
                return this;
            }

            public void RunMoveNext(ICursor cursor)
            {
                var bidProxy = cursor.GetProxy<double>(_bidId.ToString());
                var bsizeProxy = cursor.GetProxy<int>(_bsizeId.ToString());
                var askProxy = cursor.GetProxy<double>(_askId.ToString());
                var asizeProxy = cursor.GetProxy<int>(_asizeId.ToString());

                for (var i = 0; i < 5; i++)
                {
                    foreach (var snapshot in _snapshots)
                    {
                        var nextTimestamp = cursor.MoveNext(snapshot.RequestTimestamp);

                        Assert.AreEqual(snapshot.CurrentTimestamp, cursor.Timestamp, $"Current Timestamp, for request: {snapshot.RequestTimestamp:HH:mm:ss.fff}, on iteration: {i}");
                        Assert.AreEqual(snapshot.NextTimestamp, nextTimestamp, $"Next Timestamp, for request: {snapshot.RequestTimestamp:HH:mm:ss.fff}, on iteration: {i}");
                        Assert.AreEqual(snapshot.Bid, bidProxy.Value, $"Bid value, for request: {snapshot.RequestTimestamp:HH:mm:ss.fff}, on iteration: {i}");
                        Assert.AreEqual(snapshot.BSize, bsizeProxy.Value, $"Bid size value, for request: {snapshot.RequestTimestamp:HH:mm:ss.fff}, on iteration: {i}");
                        Assert.AreEqual(snapshot.Ask, askProxy.Value, $"Ask value, for request: {snapshot.RequestTimestamp:HH:mm:ss.fff}, on iteration: {i}");
                        Assert.AreEqual(snapshot.ASize, asizeProxy.Value, $"Ask size value, for request: {snapshot.RequestTimestamp:HH:mm:ss.fff}, on iteration: {i}");
                    }

                    cursor.Reset();
                }
            }
        }

        protected class Snapshot
        {
            public DateTime RequestTimestamp;
            public DateTime CurrentTimestamp;
            public DateTime NextTimestamp;
            public double Bid;
            public int BSize;
            public double Ask;
            public int ASize;
        }

        #endregion
    }
}
