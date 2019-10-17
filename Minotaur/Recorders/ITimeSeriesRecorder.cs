using System;

namespace Minotaur.Recorders
{
    public interface ITimeSeriesRecorder : IDisposable
    {
        IRowRecorder<ITimeSeriesRecorder> AddRow(DateTime timestamp);

        void Commit();

        void Revert();
    }
}
