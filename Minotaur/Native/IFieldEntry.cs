namespace Minotaur.Native
{
    public interface IFieldEntry<out T>
    {
        long Ticks { get; }
        T Value { get; }

        void Reset();
    }
}
