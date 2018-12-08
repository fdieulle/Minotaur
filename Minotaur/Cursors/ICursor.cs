using System;

namespace Minotaur.Cursors
{
    public interface ICursor : IDisposable
    {
        DateTime Timestamp { get; }

        DateTime MoveNext(DateTime timestamp);

        DateTime MoveNextTick();

        void Reset();

        IFieldProxy<T> GetProxy<T>(string column) where T : struct;
    }
}
