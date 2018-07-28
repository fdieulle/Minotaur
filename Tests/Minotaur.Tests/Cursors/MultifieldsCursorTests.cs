using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Minotaur.Codecs;
using Minotaur.Cursors;
using Minotaur.IO;
using Minotaur.Native;
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

            #region Prepare memory
            var bufferSnapshotSize = sizeof(DoubleEntry) * 2 + sizeof(Int32Entry) * 2; // snapshot Field cursor requested size
            const int blockSize = 8192;
            const int fbs = 30;
            var bufferColumnStreamSize = (2 + 2) * blockSize; // buffer reader sizes
            var bufferCursorSize = fbs.Floor(sizeof(DoubleEntry)) * 2 + fbs.Floor(sizeof(Int32Entry)) * 2; // field buffer sizes
            var fullBufferSize = bufferSnapshotSize + bufferColumnStreamSize + bufferCursorSize;

            var buffer = Marshal.AllocHGlobal(fullBufferSize);
            try
            {
                var pSnapFields = new Dictionary<int, IntPtr>
                {
                    {1, buffer},
                    {2, buffer + sizeof(DoubleEntry)},
                    {3, buffer + sizeof(DoubleEntry) + sizeof(Int32Entry)},
                    {4, buffer + sizeof(DoubleEntry) + sizeof(Int32Entry) + sizeof(DoubleEntry)},
                };

                var pBufFields = new Dictionary<int, IntPtr>
                {
                    {1, buffer + bufferSnapshotSize + blockSize * 0},
                    {2, buffer + bufferSnapshotSize + blockSize * 1},
                    {3, buffer + bufferSnapshotSize + blockSize * 2},
                    {4, buffer + bufferSnapshotSize + blockSize * 3},
                };

                var pFieldBufFields = new Dictionary<int, IntPtr>
                {
                    {1, buffer + bufferSnapshotSize + bufferColumnStreamSize},
                    {2, buffer + bufferSnapshotSize + bufferColumnStreamSize + fbs.Floor(sizeof(DoubleEntry))},
                    {3, buffer + bufferSnapshotSize + bufferColumnStreamSize + fbs.Floor(sizeof(DoubleEntry)) + fbs.Floor(sizeof(Int32Entry))},
                    {4, buffer + bufferSnapshotSize + bufferColumnStreamSize + fbs.Floor(sizeof(DoubleEntry)) + fbs.Floor(sizeof(Int32Entry)) + fbs.Floor(sizeof(DoubleEntry))},
                };

                #endregion

                #region Prepare streams

                // Create streams
                var streams = new Dictionary<int, IStream>
                {
                    {1, CreateColumnStream(new VoidCodec(), pBufFields[1], blockSize)},
                    {2, CreateColumnStream(new VoidCodec(), pBufFields[2], blockSize)},
                    {3, CreateColumnStream(new VoidCodec(), pBufFields[3], blockSize)},
                    {4, CreateColumnStream(new VoidCodec(), pBufFields[4], blockSize)}
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

                var fieldCursors = new Dictionary<int, FieldCursor>
                {
                    {1, new DoubleCursor((byte*) pSnapFields[1], (byte*)pFieldBufFields[1], fbs.Floor(sizeof(DoubleEntry)), streams[1])},
                    {2, new Int32Cursor((byte*) pSnapFields[2], (byte*)pFieldBufFields[2], fbs.Floor(sizeof(Int32Entry)), streams[2])},
                    {3, new DoubleCursor((byte*) pSnapFields[3], (byte*)pFieldBufFields[3], fbs.Floor(sizeof(DoubleEntry)), streams[3])},
                    {4, new Int32Cursor((byte*) pSnapFields[4], (byte*)pFieldBufFields[4], fbs.Floor(sizeof(Int32Entry)), streams[4])}
                };

                var cursor = new MultiFieldsCursor(fieldCursors);

                snapshots.RunMoveNext(cursor);
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }
        }

        protected static ColumnStream<MemoryStream, ICodec> CreateColumnStream(ICodec codec, IntPtr buf, int bufLen)
        {
            return new ColumnStream<MemoryStream, ICodec>(
                new MemoryStream(),
                codec,
                (byte*)buf,
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
                var bidProxy = cursor.GetProxy<double>(_bidId);
                var bsizeProxy = cursor.GetProxy<int>(_bsizeId);
                var askProxy = cursor.GetProxy<double>(_askId);
                var asizeProxy = cursor.GetProxy<int>(_asizeId);

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
