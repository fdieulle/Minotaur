using System.Collections.Generic;

namespace Minotaur.Meta
{
    public class ColumnMeta : ColumnInfo
    {
        public List<TimeSlice> Timeline { get; set; }
    }
}