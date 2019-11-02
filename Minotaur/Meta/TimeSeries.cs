using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Serialization;
using Minotaur.Core;
using Minotaur.Meta.Dto;

namespace Minotaur.Meta
{
    public class TimeSeries : ITimeSeries
    {
        private static readonly XmlSerializer serializer = new XmlSerializer(typeof(TimeSeriesDto));

        private readonly Dictionary<string, Column> _columns = new Dictionary<string, Column>();

        public string Symbol { get; }

        public DateTime LastWriteTimeUtc { get; set; }

        public TimeSeries(string symbol)
        {
            Symbol = symbol;
        }

        public Column GetOrCreateColumn(string column, FieldType type = FieldType.Unknown)
        {
            if (!_columns.TryGetValue(column ?? string.Empty, out var meta))
            {
                if (type == FieldType.Unknown)
                    throw new InvalidDataException($"Column type Unknown isn't supported and has to be defined for {column}.");

                _columns.Add(column ?? string.Empty, meta = new Column(column, type));
            }

            if (type != FieldType.Unknown && meta.Type != type)
                throw new InvalidDataException($"Column type doesn't match for {column}. Request for {type} but it's store as {meta.Type}");

            return meta;
        }

        public Column[] GetColumns(string[] names = null)
        {
            return names == null 
                ? _columns.Values.ToArray() 
                : names.Select(p => GetOrCreateColumn(p)).ToArray();
        }

        public void Restore(string filePath)
        {
            var dto = serializer.Deserialize<TimeSeriesDto>(filePath);
            if (dto == null) return;

            if(!string.Equals(dto.Symbol, Symbol)) // Todo: Maybe log and restart from an empty file instead of raise an exception
                throw new CorruptedDataException($"The symbol stored into the meta file: {dto.Symbol} is different than the Symbol time series: {Symbol}. File: {filePath}");

            if (dto.Columns != null)
            {
                foreach (var column in dto.Columns)
                    _columns[column.Name ?? string.Empty] = new Column(column);
            }
        }

        public void Persist(string filePath)
        {
            var dto = new TimeSeriesDto
            {
                Symbol = Symbol,
                Columns = _columns.Values
                    .Select(p => p.ToDto())
                    .ToList()
            };

            serializer.Serialize(filePath, dto);
        }
    }
}