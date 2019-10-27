using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Minotaur.Core;
using Minotaur.IO;

namespace Minotaur.Providers
{
    // Todo: remove it. Kept now only for its lazy data collector algorithm based on BTree
    public interface IStreamFactory<TStream> where TStream : IStream
    {
        TStream CreateReader(string filePath);
        TStream CreateWriter(string filePath);
    }
    // Todo: remove it but keep it for lazy data collector algorithm
    public class StreamProvider<TStream> : IStreamProvider<TStream>
        where TStream : IStream
    {
        // Todo: Do a scan once a day to merge and resize all files and so update FileMetada btrees.
        // Todo: FilePath creation is delayed to DataCollector
        // Todo: Implements a thread safety version

        private readonly Dictionary<string, BTree<DateTime, FileMetaData>> _bTrees = new Dictionary<string, BTree<DateTime, FileMetaData>>();
        private readonly IFilePathProvider _filePathProvider;
        private readonly IStreamFactory<TStream> _streamFactory;
        private readonly IDataProvider _dataProvider;

        public StreamProvider(
            IFilePathProvider filePathProvider,
            IStreamFactory<TStream> streamFactory,
            IDataProvider dataProvider)
        {
            _filePathProvider = filePathProvider;
            _streamFactory = streamFactory;
            _dataProvider = dataProvider;
        }

        #region Implementation of IStreamProvider

        public IEnumerable<TStream> Fetch(string symbol, string column, DateTime start, DateTime end)
        {
            var bTree = GetBTree(symbol, column);

            foreach (var entry in bTree.Search(start, end).ToList())
            {
                if (start < entry.Key || !entry.Value.FilePath.FileExists())
                {
                    // Load from start to entry.Key
                    foreach (var stream in CollectAndFetch(symbol, column, start, entry.Key.AddTicks(-1)))
                        yield return stream;

                    Persist(_filePathProvider.GetMetaFilePath(symbol/*, column*/), bTree);
                }

                var reader = _streamFactory.CreateReader(entry.Value.FilePath);
                if(reader != null)
                    yield return reader;
                start = entry.Value.End;
            }

            if (start < end)
            {
                foreach (var stream in CollectAndFetch(symbol, column, start, end))
                    yield return stream;

                Persist(_filePathProvider.GetMetaFilePath(symbol/*, column*/), bTree);
            }
        }

        private IEnumerable<TStream> CollectAndFetch(string symbol, string column, DateTime start, DateTime end)
        {
            // Load from start to entry.Key
            foreach (var meta in _dataProvider.Fetch(symbol, start, end))
            {
                // Todo: Update btree entries by locking and save metadata
                AddMeta(meta);
                if (meta.Column == column)
                {
                    var reader = _streamFactory.CreateReader(meta.FilePath);
                    if(reader != null)
                        yield return reader;
                }
            }
        }

        #endregion

        private BTree<DateTime, FileMetaData> GetBTree(string symbol, string column)
        {
            var key = GetKey(symbol, column);
            if (!_bTrees.TryGetValue(key, out var bTree))
            {
                bTree = LoadBTree(_filePathProvider.GetMetaFilePath(symbol/*, column*/)) ?? CreateBTree();
                _bTrees.Add(key, bTree);
            }
            return bTree;
        }

        private void AddMeta(FileMetaData meta)
            => GetBTree(meta.Symbol, meta.Column).Insert(meta.Start, meta);

        private static BTree<DateTime, FileMetaData> CreateBTree()
            => new BTree<DateTime, FileMetaData>(50);

        private static string GetKey(string symbol, string column) => $"{symbol}_{column}";

        #region Meta persistency

        // ReSharper disable once StaticMemberInGenericType
        private static readonly XmlSerializer serializer = new XmlSerializer(typeof(List<FileMetaData>));

        private static BTree<DateTime, FileMetaData> LoadBTree(string metaFilePath)
        {
            // SpinLock file meta for multi processes concurrency
            using (metaFilePath.LockFile())
            {
                var meta = serializer.Deserialize<List<FileMetaData>>(metaFilePath);
                if (meta == null) return null;

                var bTree = CreateBTree();
                foreach (var m in meta)
                    bTree.Insert(m.Start, m);
                return bTree;
            }   
        }

        private static void Persist(string metaFilePath, BTree<DateTime, FileMetaData> bTree)
        {
            using (metaFilePath.LockFile())
            {
                // Merge meta and drop collisions
                var meta = serializer.Deserialize<List<FileMetaData>>(metaFilePath);
                if (meta != null)
                {
                    foreach (var m in meta.Where(p => p.FilePath.FileExists() && bTree.Search(p.Start) == null))
                        bTree.Insert(m.Start, m);
                }

                serializer.Serialize(metaFilePath, bTree.ToList());
            }
        }

        #endregion
    }

    public interface IStreamProvider<out TStream> where TStream : IStream
    {
        IEnumerable<TStream> Fetch(string symbol, string column, DateTime start, DateTime end);
    }

    public class FileMetaData
    {
        public string Symbol { get; set; }

        public string Column { get; set; }

        public FieldType Type { get; set; }

        public DateTime Start { get; set; }

        public DateTime End { get; set; }

        // Todo: The file path creation has to be owmn by the data collector
        public string FilePath { get; set; }

        //public string GetFilePath(string folder)
        //    => Path.Combine(folder, Start.Year.ToString(), Symbol, $"{Symbol}_{Column}_{Start:yyyy-MM-dd_HH:mm:ss}.min");
    }

    public interface IDataProvider
    {
        IEnumerable<FileMetaData> Fetch(string symbol, DateTime start, DateTime end);
    }
}