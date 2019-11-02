using System.Collections.Generic;

namespace Minotaur.Meta.Dto
{
    public class ColumnDto : ColumnInfo
    {
        public List<TimeSlice> Timeline { get; set; }
    }
}