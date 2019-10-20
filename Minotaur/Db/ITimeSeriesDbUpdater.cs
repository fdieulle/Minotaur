using System;
using Minotaur.Meta;
using Minotaur.Streams;

namespace Minotaur.Db
{
    public interface ITimeSeriesDbUpdater
    {
        IColumnStream CreateColumnWriter(string symbol, ColumnInfo column, DateTime start);
        void Commit(string symbol, ColumnCommit[] columns);
        void Revert(string symbol, ColumnCommit[] columns);
    }
}