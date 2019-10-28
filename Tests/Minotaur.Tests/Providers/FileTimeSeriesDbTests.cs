using System;
using System.Collections.Generic;
using System.Linq;
using Minotaur.Core;
using Minotaur.Cursors;
using Minotaur.Db;
using Minotaur.Providers;
using NUnit.Framework;

namespace Minotaur.Tests.Providers
{
    [TestFixture]
    public class FileTimeSeriesDbTests : AbstractTests
    {
        [Test]
        public void InsertDataThroughRecorderTest()
        {
            const string symbol = "MySymbol";
            const string int32Column = "Int32Column";
            const string floatColumn = "FloatColumn";
            const string doubleColumn = "DoubleColumn";
            const string int64Column = "Int64Column";

            using (CreateDb(out var db))
            {
                var recorder = db.CreateRecorder("MySymbol");

                recorder.AddRow(Dt("08:00:00"))
                    .Record(int32Column, 1)
                    .Record(doubleColumn, 1.2)
                    .Record(int64Column, 2L);

                recorder.AddRow("08:00:01")
                    .Record(floatColumn, 1.4f)
                    .Record(int64Column, 23L);

                recorder.AddRow("08:00:02")
                    .Record(int32Column, 12);

                recorder.AddRow("08:00:03")
                    .Record(doubleColumn, 12.4)
                    .Record(int32Column, 13);

                recorder.AddRow("08:00:04");

                recorder.AddRow("08:00:05")
                    .Record(doubleColumn, 12.3)
                    .Record(int32Column, 13);

                recorder.AddRow("08:00:06")
                    .Record(doubleColumn, 12.5)
                    .Record(doubleColumn, 13.5);

                recorder.Commit();

                var cursor = db.GetCursor(symbol, Dt("08:00:00"), Dt("08:00:10"));
                var int32Cursor = cursor.GetProxy<int>(int32Column);
                var floatCursor = cursor.GetProxy<float>(floatColumn);
                var int64Cursor = cursor.GetProxy<long>(int64Column);
                var doubleCursor = cursor.GetProxy<double>(doubleColumn);

                cursor.MoveNext(Dt("07:00:00"));

                int32Cursor.Check(0);
                floatCursor.Check(float.NaN);
                int64Cursor.Check(0);
                doubleCursor.Check(double.NaN);

                cursor.MoveNext(Dt("08:00:00"));

                int32Cursor.Check(1, "08:00:00");
                floatCursor.Check(float.NaN);
                int64Cursor.Check(2L, "08:00:00");
                doubleCursor.Check(1.2, "08:00:00");

                cursor.MoveNext(Dt("08:00:01"));

                int32Cursor.Check(1, "08:00:00");
                floatCursor.Check(1.4f, "08:00:01");
                int64Cursor.Check(23L, "08:00:01");
                doubleCursor.Check(1.2, "08:00:00");

                cursor.MoveNext(Dt("08:00:02"));

                int32Cursor.Check(12, "08:00:02");
                floatCursor.Check(1.4f, "08:00:01");
                int64Cursor.Check(23L, "08:00:01");
                doubleCursor.Check(1.2, "08:00:00");

                cursor.MoveNext(Dt("08:00:03"));

                int32Cursor.Check(13, "08:00:03");
                floatCursor.Check(1.4f, "08:00:01");
                int64Cursor.Check(23L, "08:00:01");
                doubleCursor.Check(12.4, "08:00:03");


                cursor.MoveNext(Dt("08:00:04"));

                int32Cursor.Check(13, "08:00:03");
                floatCursor.Check(1.4f, "08:00:01");
                int64Cursor.Check(23L, "08:00:01");
                doubleCursor.Check(12.4, "08:00:03");

                cursor.MoveNext(Dt("08:00:05"));

                int32Cursor.Check(13, "08:00:05");
                floatCursor.Check(1.4f, "08:00:01");
                int64Cursor.Check(23L, "08:00:01");
                doubleCursor.Check(12.3, "08:00:05");

                cursor.MoveNext(Dt("08:00:06"));

                int32Cursor.Check(13, "08:00:05");
                floatCursor.Check(1.4f, "08:00:01");
                int64Cursor.Check(23L, "08:00:01");
                doubleCursor.Check(13.5, "08:00:06");
            }
        }

        [Test]
        public void ReadSymbolWhenItsModified()
        {
            const string symbol = "SymbolTest";
            const string c1 = "Column1";
            const string c2 = "Column2";

            using (CreateDb(out var db))
            {
                var data1 = CreateRandomData("08:00:00", "08:10:00", c1, c2);
                var data2 = CreateRandomData("08:20:00", "08:30:00", c1, c2);
                var data3 = CreateRandomData("08:40:00", "08:50:00", c1, c2);

                db.Insert(symbol, data1);
                db.Insert(symbol, data2);
                db.Insert(symbol, data3);

                var cursor = db.GetCursor(symbol, "08:00:00".ToDateTime(), "09:00:00".ToDateTime());

                var columns = GetColumnProxies(cursor, c1, c2);

                cursor.MoveNext("08:00:05");
                Check(data1, "08:00:05", columns);

                // Simulate data2 changed
                var data2Modified = CreateRandomData("08:15:00", "08:25:00", c1, c2);
                db.Insert(symbol, data2Modified);
                var data2Merged = Merge(data2, data2Modified);

                // Continue on the first chunk
                cursor.MoveNext("08:05:00");
                Check(data1, "08:05:00", columns);

                // Be sure that the update has been taken into account
                cursor.MoveNext("08:17:00");
                Check(data2Merged, "08:17:00", columns);

                // Be sure that the update has overwrite the previous data
                cursor.MoveNext("08:23:00");
                Check(data2Merged, "08:23:00", columns);

                // The third chunk stays unchanged
                cursor.MoveNext("08:47:00");
                Check(data3, "08:47:00", columns);
            }
        }

        [Test]
        public void ReadSymbolWhenItsModifiedByAnotherProcess()
        {
            // Todo find a way to test the LastWriteTimeUtc check
        }

        [Test]
        public void ReadWhenTheCurrentCursorFileIsModified()
        {
            // Todo
        }

        private static Dictionary<string, Array> CreateRandomData(string start, string end, params string[] columns)
        {
            var timeline = Factory.CreateRandomDateTime(start.ToDateTime(), end.ToDateTime());
            var data = new Dictionary<string, Array>
            {
                ["timestamp"] = timeline,
            };
            foreach (var column in columns)
                data[column] = Factory.CreateRandomDouble(timeline.Length);
            return data;
        }

        private static Dictionary<string, IFieldProxy<double>> GetColumnProxies(ICursor cursor, params string[] columns) 
            => columns.ToDictionary(p => p, cursor.GetProxy<double>);

        private static Dictionary<string, Array> Merge(Dictionary<string, Array> x, Dictionary<string, Array> y)
        {
            var tx = (DateTime[])x["timestamp"];
            var ty = (DateTime[])y["timestamp"];

            var columns = x.Keys.Union(y.Keys).Distinct().Where(p => p != "timestamp").ToArray();

            var merge = columns.ToDictionary(p => p, p => new List<double>());

            var t = new List<DateTime>();
            var i = 0;
            var j = 0;
            while (i < tx.Length && j < ty.Length)
            {
                if (tx[i] < ty[j])
                {
                    t.Add(tx[i]);
                    foreach (var column in columns)
                        merge[column].Add(((double[]) x[column])[i]);

                    i++;
                }
                else if (tx[i] > ty[j])
                {
                    t.Add(ty[j]);
                    foreach (var column in columns)
                        merge[column].Add(((double[])y[column])[j]);

                    j++;
                }
                else
                {
                    t.Add(tx[i]);
                    foreach (var column in columns)
                        merge[column].Add(((double[])x[column])[i]);

                    i++;
                    j++;
                }
            }

            for (; i < tx.Length; i++)
            {
                t.Add(tx[i]);
                foreach (var column in columns)
                    merge[column].Add(((double[])x[column])[i]);
            }
            for (; j < ty.Length; j++)
            {
                t.Add(ty[j]);
                foreach (var column in columns)
                    merge[column].Add(((double[])y[column])[j]);
            }

            var result = merge.ToDictionary(p => p.Key, p => (Array)p.Value.ToArray());
            result["timestamp"] = t.ToArray();
            return result;
        }

        private static int GetDataIdx(Dictionary<string, Array> data, string timestamp)
        {
            var t = timestamp.ToDateTime();
            var timeline = (DateTime[])data["timestamp"];
            var idx = -1;
            for (var i = 0; i < timeline.Length; i++)
            {
                if (timeline[i] > t) break;
                idx = i;
            }

            return idx;
        }

        private static void Check(Dictionary<string, Array> data, string timestamp, Dictionary<string, IFieldProxy<double>> columns)
        {
            var idx = GetDataIdx(data, timestamp);
            foreach (var pair in columns)
                Assert.AreEqual(pair.Value.Value, ((double[])data[pair.Key])[idx]);
        }

        private static IDisposable CreateDb(out ITimeSeriesDb db)
        {
            var fpp = new FilePathProviderExt(Guid.NewGuid().ToString("N"));
            var allocator = new DummyUnmanagedAllocator();
            db = new FileTimeSeriesDb(fpp, allocator);

            return Disposable.Create(() =>
            {
                fpp.Dispose();
                allocator.Dispose();
            });
        }
        
        private class FilePathProviderExt : FilePathProvider, IDisposable
        {
            private readonly string _root;

            public FilePathProviderExt(string root) 
                : base(root) => _root = root;

            public void Dispose() => _root.DeleteFolder();
        }
    }
}
