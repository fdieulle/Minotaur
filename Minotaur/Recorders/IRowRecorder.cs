namespace Minotaur.Recorders
{
    public interface IRowRecorder<out TParent>
    {
        TParent Parent { get; }

        IRowRecorder<TParent> Record<T>(string column, T value) where T : unmanaged;
    }
}