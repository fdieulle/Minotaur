using System;
using Minotaur.Cursors;

namespace Minotaur.Providers
{
    public interface ITimeSeriesProvider
    {
        ICursor GetCursor(string symbol, DateTime start, DateTime? end = null, int[] fields = null);
    }
}
