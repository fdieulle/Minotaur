using System;
using Minotaur.Core;
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

            using (var fpp = GetCacheFolder())
            {
                var allocator = new DummyUnmanagedAllocator();
                var db = new FileTimeSeriesDb(fpp, allocator);

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
        
        private static FilePathProviderExt GetCacheFolder() 
            => new FilePathProviderExt(Guid.NewGuid().ToString("N"));

        private class FilePathProviderExt : FilePathProvider, IDisposable
        {
            private readonly string _root;

            public FilePathProviderExt(string root) 
                : base(root) => _root = root;

            public void Dispose() => _root.DeleteFolder();
        }
    }
}
