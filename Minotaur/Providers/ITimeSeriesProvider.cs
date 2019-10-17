using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Minotaur.Providers
{
    public class DataFrame : IEnumerable<KeyValuePair<string, Array>>
    {
        private readonly Dictionary<string, Array> _columns = new Dictionary<string, Array>();

        public Array this[string column]
        {
            get => _columns.TryGetValue(column ?? string.Empty, out var data) ? data : null;
            set => AddColumn(column, value);
        }

        public IEnumerable<string> Names => _columns.Keys;

        public T[] GetColumn<T>(string column)
            => ((T[])this[column] ?? Array.Empty<T>());

        public void AddColumn(string name, Array data)
        {
            if(_columns.Count > 0 && _columns.Values.First().Length != data.Length)
                throw new InvalidDataException($"The column length: {data.Length} doesn't match the DataFrame Length: {_columns.Values.First().Length}");

            _columns[name ?? string.Empty] = data;
        }

        #region Implementation of IEnumerable

        public IEnumerator<KeyValuePair<string, Array>> GetEnumerator() 
            => _columns.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        #endregion
    }
    

    public class ColumnInfo
    {
        public string Name { get; set; }
        public FieldType Type { get; set; }

        public override string ToString() => $"[{Name}] {Type}";
    }
}
