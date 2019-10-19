using System;
using System.Collections.Generic;
using Minotaur.Cursors;
using Minotaur.Recorders;

namespace Minotaur.Db
{
    public interface ITimeSeriesDb
    {
        ICursor GetCursor(string symbol, DateTime start, DateTime? end = null, string[] columns = null);

        ITimeSeriesRecorder CreateRecorder(string symbol);

        void Insert(string symbol, Dictionary<string, Array> data);

        void Delete(string symbol, DateTime start, DateTime end, string[] columns = null);
    }
}