using System;
using System.Collections.Generic;
using Minotaur.Providers;

namespace Minotaur.Recorders
{
    public interface IDataCollector
    {
        IEnumerable<FileMetaData> Collect(string symbol, DateTime start, DateTime end);
    }
}