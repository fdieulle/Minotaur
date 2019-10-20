using System;
using System.Linq;
using Minotaur.Core;

namespace Minotaur.Meta
{
    public class ColumnMeta : ColumnInfo
    {
        public BTree<DateTime, TimeSlice> Timeline { get; } = new BTree<DateTime, TimeSlice>(50);

        public ColumnMeta(string name, FieldType type)
        {
            Name = name ?? string.Empty;
            Type = type;
        }

        public ColumnMeta(ColumnMetaDto dto)
            : this(dto.Name, dto.Type)
        {
            if (dto.Timeline == null) return;
            foreach (var slice in dto.Timeline.Where(p => p != null))
                Timeline.Insert(slice.Start, slice);
        }
    }
}