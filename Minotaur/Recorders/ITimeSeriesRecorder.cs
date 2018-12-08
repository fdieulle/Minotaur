using System;
using System.Collections.Generic;
using Minotaur.Providers;

namespace Minotaur.Recorders
{
    public interface ITimeSeriesRecorder : IDisposable
    {
        IEnumerable<FileMetaData> MetaData { get; }

        IRowRecorder<ITimeSeriesRecorder> AddRow(DateTime timestamp);
    }
}
