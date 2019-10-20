using System.Collections.Generic;

namespace Minotaur.Meta
{
    public class ColumnMetaDto : ColumnInfo
    {
        public List<TimeSlice> Timeline { get; set; }
    }
}