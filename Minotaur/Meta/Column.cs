using System;
using System.Collections.Generic;
using System.Linq;
using Minotaur.Core;
using Minotaur.Meta.Dto;

namespace Minotaur.Meta
{
    public class Column : ColumnInfo
    {
        private readonly BTree<DateTime, TimeSlice> _timeline = new BTree<DateTime, TimeSlice>(50);

        public int Revision { get; private set; }

        public Column(string name, FieldType type)
        {
            Name = name ?? string.Empty;
            Type = type;
        }

        public Column(ColumnDto dto)
            : this(dto.Name, dto.Type)
        {
            if (dto.Timeline == null) return;
            foreach (var slice in dto.Timeline.Where(p => p != null))
                _timeline.Insert(slice.Start, slice);
        }

        public void Insert(DateTime key, TimeSlice slice)
        {
            _timeline.Insert(key, slice);
            Revision += 1;
        }

        public void Delete(DateTime key)
        {
            _timeline.Delete(key);
            Revision += 1;
        }

        public bool HasChanged(int revision) => revision != Revision;

        public IEnumerable<Entry<DateTime, TimeSlice>> Search(DateTime start, DateTime end) 
            => _timeline.Search(start, end, p => start < p.End);

        public ColumnDto ToDto() => new ColumnDto
        {
            Name = Name,
            Type = Type,
            Timeline = _timeline.Select(t => t.Value).ToList()
        };
    }
}