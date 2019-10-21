using System;

namespace Minotaur.Recorders
{
    public interface ITimeSeriesRecorder : IDisposable
    {
        IRowRecorder AddRow(DateTime timestamp);

        void Commit();

        void Revert();
    }
}
