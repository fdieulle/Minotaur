using System;

namespace Minotaur.Cursors
{
    public interface IColumnCursor : IDisposable
    {
        void MoveNext(long ticks);

        void Reset();
    }

    public interface IColumnCursor<out T> : IColumnCursor, IFieldProxy<T>
        where T : struct
    { }

    public interface IFieldProxy<out T> where T : struct
    {
        DateTime Timestamp { get; }

        T Value { get; }
    }
}
