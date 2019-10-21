namespace Minotaur.Recorders
{
    public interface IArrayRecorder
    {
        void Record(IRowRecorder recorder, int i);
    }

    public class ArrayRecorder<T> : IArrayRecorder
        where T : unmanaged
    {
        private readonly string _column;
        private readonly T[] _array;

        public ArrayRecorder(string column, T[] array)
        {
            _column = column;
            _array = array;
        }

        public void Record(IRowRecorder recorder, int i) 
            => recorder.Record<T>(_column, _array[i]);
    }
}
