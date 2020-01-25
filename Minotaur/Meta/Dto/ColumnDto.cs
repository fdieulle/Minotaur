using System.Collections.Generic;

namespace Minotaur.Meta.Dto
{
    public class ColumnDto : ColumnInfo
    {
        public List<FileTimeSlice> Timeline { get; set; }
    }
}