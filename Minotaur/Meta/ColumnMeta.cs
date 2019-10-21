﻿using System;
using System.Collections.Generic;
using System.Linq;
using Minotaur.Core;

namespace Minotaur.Meta
{
    public class ColumnMeta : ColumnInfo
    {
        private readonly BTree<DateTime, TimeSlice> _timeline = new BTree<DateTime, TimeSlice>(50);

        public int Revision { get; private set; }

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
            => _timeline.Search(start, end);

        public ColumnMetaDto ToDto() => new ColumnMetaDto
        {
            Name = Name,
            Type = Type,
            Timeline = _timeline.Select(t => t.Value).ToList()
        };
    }
}