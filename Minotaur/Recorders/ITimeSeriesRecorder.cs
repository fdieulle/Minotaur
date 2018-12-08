using System;
using System.Collections.Generic;
using Minotaur.Providers;

namespace Minotaur.Recorders
{
    public interface ITimeSeriesRecorder
    {
        IEnumerable<FileMetaData> MetaData { get; }

        IRowRecorder<ITimeSeriesRecorder> AddRow(DateTime timestamp);
    }
}
