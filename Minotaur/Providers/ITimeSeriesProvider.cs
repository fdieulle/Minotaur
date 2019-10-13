using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Minotaur.Codecs;
using Minotaur.Core;
using Minotaur.Cursors;
using Minotaur.Native;
using Minotaur.Streams;

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

    public interface ITimeSeriesProvider
    {
        ICursor GetCursor(string symbol, DateTime start, DateTime? end = null, string[] columns = null);
    }

    public class TimeSeriesProvider<TStream> : ITimeSeriesProvider
        where TStream : IStream
    {
        private readonly IStreamProvider<TStream> _streamProvider;
        private readonly IColumnFactory _columnFactory;
        private readonly IColumnStore _columnStore;
        private readonly IAllocator _allocator;

        public TimeSeriesProvider(
            IStreamProvider<TStream> streamProvider,
            IColumnFactory columnFactory,
            IColumnStore columnStore, 
            IAllocator allocator = null)
        {
            _streamProvider = streamProvider;
            _columnFactory = columnFactory;
            _columnStore = columnStore;
            _allocator = allocator ?? new DummyUnmanagedAllocator();
        }

        #region Implementation of ITimeSeriesProvider

        public ICursor GetCursor(string symbol, DateTime start, DateTime? end = null, string[] columns = null)
        {
            end = end ?? start.AddDays(1);

            var cursors = new Dictionary<string, IColumnCursor>();
            foreach (var column in _columnStore.GetColumns(symbol, columns))
            {
                var streams = _streamProvider.Fetch(symbol, column.Name, start, end.Value);
                cursors[column.Name] = _columnFactory.CreateCursor(column, new MultiStream<TStream>(streams), _allocator);
            }

            return new TimeSeriesCursor(cursors);
        }

        #endregion
    }

    public interface IColumnStore
    {
        IEnumerable<ColumnInfo> GetColumns(string symbol, string[] columns);

        void AddColumn(string symbol, ColumnInfo column);
    }

    public class ColumnInfo
    {
        public string Name { get; set; }
        public FieldType Type { get; set; }
    }
}
