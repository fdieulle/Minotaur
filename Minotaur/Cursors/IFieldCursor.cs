using System;

namespace Minotaur.Cursors
{
    public interface IFieldCursor : IDisposable
    {
        void MoveNext(long ticks);

        void Reset();
    }

    public interface IFieldCursor<out T> : IFieldCursor, IFieldProxy<T>
        where T : struct
    { }

    public interface IFieldProxy<out T> where T : struct
    {
        DateTime Timestamp { get; }

        T Value { get; }
    }
}
