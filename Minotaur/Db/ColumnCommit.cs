using System;
using Minotaur.Meta;

namespace Minotaur.Db
{
    public class ColumnCommit : ColumnInfo
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        public override string ToString()
            => $"{base.ToString()}, [{Start:yyyy-MM-dd HH:mm:ss.fff}; {End:yyyy-MM-dd HH:mm:ss.fff}]";
    }
}