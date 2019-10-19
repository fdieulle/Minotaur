using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Minotaur.Core;
using Minotaur.IO;
using Minotaur.Providers;
using NSubstitute;
using NUnit.Framework;

namespace Minotaur.Tests
{
    [TestFixture]
    public class StreamProviderTests
    {
        [Test]
        public void NominalTest()
        {
            var rootFolder = Guid.NewGuid().ToString("N");

            try
            {
                var filePathProvider = new FilePathProvider(rootFolder);
                var dataProvider = Substitute.For<IDataProvider>();
                dataProvider.Fetch(Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<DateTime>())
                    .ReturnsForAnyArgs(p => p.ArgAt<DateTime>(1).SplitDaysTo(p.ArgAt<DateTime>(2))
                        .SelectMany(d => Fmds(filePathProvider, p.ArgAt<string>(0), new[] {"Column_1", "Column_2", "Column_3"}, d))
                        .Select(CreateFile));

                var streamFactory = Substitute.For<IStreamFactory<IStream>>();
                var mockStream = Substitute.For<IStream>();
                streamFactory.CreateReader(Arg.Any<string>()).Returns(mockStream);

                var provider = new StreamProvider<IStream>(filePathProvider, streamFactory, dataProvider);

                var symbol = "Symbol";

                // Initial request
                CheckDataCollect("2018-03-01", "2018-03-31");

                // 2nd request in the future from the first one
                CheckDataCollect("2018-05-01", "2018-05-31");

                // 3th request in the past from the first one
                CheckDataCollect("2018-01-01", "2018-01-31");

                // 4th request between 1st and 2nd
                CheckDataCollect("2018-04-01", "2018-04-30");

                // Request already collected data
                CheckDataReadOnly("2018-03-15", "2018-05-15");

                // Request both
                CheckMixed("2018-01-15", "2018-03-15", "2018-02-01", "2018-03-01");

                void CheckDataCollect(string startStr, string endStr)
                {
                    var start = startStr.ToDateTime();
                    var end = endStr.ToDateTime();
                    var dates = start.SplitDaysTo(end).ToArray();
                    var idx = 0;
                    foreach (var stream in provider.Fetch("Symbol", "Column_2", start, end))
                    {
                        var date = dates[idx++];

                        var filePath = filePathProvider.GetFilePath("Symbol", "Column_2", date);
                        streamFactory.Received(1).CreateReader(Arg.Is<string>(p => string.Equals(p, filePath)));

                        streamFactory.Received(1).CreateReader(Arg.Any<string>());

                        streamFactory.ClearReceivedCalls();
                        Assert.AreEqual(mockStream, stream);
                    }

                    Assert.AreEqual(dates.Length, idx);
                    dataProvider.Received(1).Fetch(
                        Arg.Is<string>(p => p == symbol),
                        Arg.Is<DateTime>(p => p == start),
                        Arg.Is<DateTime>(p => p == end));
                    dataProvider.ClearReceivedCalls();
                }

                void CheckDataReadOnly(string startStr, string endStr)
                {
                    var start = startStr.ToDateTime();
                    var end = endStr.ToDateTime();
                    var dates = start.SplitDaysTo(end).ToArray();
                    var idx = 0;
                    foreach (var stream in provider.Fetch("Symbol", "Column_2", start, end))
                    {
                        var date = dates[idx++];
                        var filePath = filePathProvider.GetFilePath("Symbol", "Column_2", date);
                        streamFactory.Received(1).CreateReader(Arg.Is<string>(p => string.Equals(p, filePath)));

                        streamFactory.Received(1).CreateReader(Arg.Any<string>());

                        streamFactory.ClearReceivedCalls();
                        Assert.AreEqual(mockStream, stream);
                    }

                    Assert.AreEqual(dates.Length, idx);
                    dataProvider.DidNotReceive().Fetch(Arg.Any<string>(), Arg.Any<DateTime>(), Arg.Any<DateTime>());
                }

                void CheckMixed(string startStr, string endStr, string collectStartStr, string collectEndStr)
                {
                    var start = startStr.ToDateTime();
                    var end = endStr.ToDateTime();
                    var dates = start.SplitDaysTo(end).ToArray();
                    var idx = 0;
                    foreach (var stream in provider.Fetch("Symbol", "Column_2", start, end))
                    {
                        var date = dates[idx++];
                        var filePath = filePathProvider.GetFilePath("Symbol", "Column_2", date);
                        streamFactory.Received(1).CreateReader(Arg.Is<string>(p => string.Equals(p, filePath)));

                        streamFactory.Received(1).CreateReader(Arg.Any<string>());

                        streamFactory.ClearReceivedCalls();
                        Assert.AreEqual(mockStream, stream);
                    }

                    Assert.AreEqual(dates.Length, idx);
                    dataProvider.Received(1).Fetch(
                        Arg.Is<string>(p => p == symbol),
                        Arg.Is<DateTime>(p => p == collectStartStr.ToDateTime()),
                        Arg.Is<DateTime>(p => p == collectEndStr.ToDateTime().AddTicks(-1)));
                    dataProvider.ClearReceivedCalls();
                }
            }
            finally
            {
                Directory.Delete(rootFolder, true);
            }
        }
        
        private static IEnumerable<FileMetaData> Fmds(IFilePathProvider provider, string symbol, string[] columns, DateTime start, DateTime? end = null)
            => (columns ?? new string[0]).Select(p => Fmd(provider, symbol, p, start, end));

        private static FileMetaData Fmd(IFilePathProvider provider, string symbol, string column, DateTime start, DateTime? end = null)
        {
            return new FileMetaData()
            {
                Symbol = symbol,
                Column = column,
                Start = start.Date,
                End = end ?? start.Date.AddDays(1).Date,
                FilePath = provider.GetFilePath(symbol, column, start)
            };
        }

        private static FileMetaData CreateFile(FileMetaData m)
        {
            m.FilePath.GetFolderPath().CreateFolderIfNotExist();
            File.WriteAllText(m.FilePath, "Test");
            return m;
        }
    }
}
