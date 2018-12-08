using System;
using System.Collections.Generic;

namespace Minotaur.Providers
{
    public interface IDataProvider
    {
        IEnumerable<FileMetaData> Fetch(string symbol, DateTime start, DateTime end);
    }
}