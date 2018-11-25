using System;
using System.Collections.Generic;
using Minotaur.Streams;

namespace Minotaur.Providers
{
    public interface IStreamProvider
    {
        IEnumerable<IStream> Fetch(string symbol, string column, DateTime start, DateTime end);
    }
}