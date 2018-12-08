using System;
using Minotaur.Cursors;

namespace Minotaur.Providers
{
    public interface ITimeSeriesProvider
    {
        ICursor GetCursor(string symbol, DateTime start, DateTime? end = null, string[] columns = null);
    }

    public class TimeSeriesProvider : ITimeSeriesProvider
    {
        #region Implementation of ITimeSeriesProvider

        public ICursor GetCursor(string symbol, DateTime start, DateTime? end = null, string[] columns = null)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
