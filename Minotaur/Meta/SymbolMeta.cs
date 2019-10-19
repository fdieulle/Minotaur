using System.Collections.Generic;

namespace Minotaur.Meta
{
    public class SymbolMeta
    {
        public string Symbol { get; set; }

        public List<ColumnMeta> Columns { get; set; } = new List<ColumnMeta>();
    }
}