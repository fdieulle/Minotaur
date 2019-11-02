using System.Collections.Generic;

namespace Minotaur.Meta.Dto
{
    public class TimeSeriesDto
    {
        public string Symbol { get; set; }

        public List<ColumnDto> Columns { get; set; } = new List<ColumnDto>();
    }
}