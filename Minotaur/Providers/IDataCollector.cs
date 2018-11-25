using System;
using System.Collections.Generic;

namespace Minotaur.Providers
{
    public interface IDataCollector
    {
        IEnumerable<FileMetaData> Collect(string symbol, DateTime start, DateTime end);
    }
}