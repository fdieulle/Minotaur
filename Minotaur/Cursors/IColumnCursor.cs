using System;

namespace Minotaur.Cursors
{
    public interface IColumnCursor : IDisposable
    {
        long Ticks { get; }

        long NextTicks { get; }

        void MoveNext(long ticks);

        void Reset();
    }

    public interface IColumnCursor<out T> : IColumnCursor, IFieldProxy<T> { }

    public interface IFieldProxy<out T>
    {
        DateTime Timestamp { get; }

        T Value { get; }
    }
}
