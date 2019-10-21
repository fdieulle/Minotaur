namespace Minotaur.Recorders
{
    public interface IRowRecorder
    {
        IRowRecorder Record<T>(string column, T value) where T : unmanaged;
    }
}