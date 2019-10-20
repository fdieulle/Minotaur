using System.Collections.Generic;

namespace Minotaur.Meta
{
    public class SymbolMetaDto
    {
        public string Symbol { get; set; }

        public List<ColumnMetaDto> Columns { get; set; } = new List<ColumnMetaDto>();
    }
}