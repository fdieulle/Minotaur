using System;
using System.Collections.Generic;
using Minotaur.Streams;

namespace Minotaur.Providers
{
    public interface IStreamProvider<out TStream> where TStream : IStream
    {
        IEnumerable<TStream> Fetch(string symbol, string column, DateTime start, DateTime end);
    }
}