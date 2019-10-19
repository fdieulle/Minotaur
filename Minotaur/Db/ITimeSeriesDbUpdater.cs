using System;
using Minotaur.Meta;
using Minotaur.Providers;
using Minotaur.Streams;

namespace Minotaur.Db
{
    public interface ITimeSeriesDbUpdater
    {
        IColumnStream CreateColumnWriter(string symbol, ColumnInfo column, DateTime start);
        void CommitColumn(string symbol, ColumnInfo column, DateTime start, DateTime end);
        void RevertColumn(string symbol, string column, DateTime start);
    }
}