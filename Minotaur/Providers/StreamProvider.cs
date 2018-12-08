using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Serialization;
using Minotaur.Core;
using Minotaur.Core.Platform;
using Minotaur.Recorders;
using Minotaur.Streams;

namespace Minotaur.Providers
{
    public class StreamProvider<TPlatform> : IStreamProvider
        where TPlatform : IPlatform
    {
        // Todo: Do a scan once a day to merge and resize all files and so update FileMetada btrees.
        // Todo: FilePath creation is delayed to DataCollector
        // Todo: Implements a thread safety version

        private readonly Dictionary<string, BTree<DateTime, FileMetaData>> _bTrees = new Dictionary<string, BTree<DateTime, FileMetaData>>();
        private readonly IFilePathProvider _filePathProvider;
        private readonly IDataProvider _dataProvider;
        private readonly IStreamFactory<TPlatform> _factory;

        public StreamProvider(
            IFilePathProvider filePathProvider,
            IDataProvider dataProvider, 
            IStreamFactory<TPlatform> factory)
        {
            _filePathProvider = filePathProvider;
            _dataProvider = dataProvider;
            _factory = factory;
        }

        #region Implementation of IStreamProvider

        public IEnumerable<IStream> Fetch(string symbol, string column, DateTime start, DateTime end)
        {
            var bTree = GetBTree(symbol, column);

            foreach (var entry in bTree.Search(start, end).ToList())
            {
                if (start < entry.Key || !entry.Value.FilePath.FileExists())
                {
                    // Load from start to entry.Key
                    foreach (var stream in CollectAndFetch(symbol, column, start, entry.Key.AddTicks(-1)))
                        yield return stream;

                    Persist(_filePathProvider.GetMetaFilePath(symbol, column), bTree);
                }

                yield return _factory.Create(entry.Value);
                start = entry.Value.End;
            }

            if (start < end)
            {
                foreach (var stream in CollectAndFetch(symbol, column, start, end))
                    yield return stream;

                Persist(_filePathProvider.GetMetaFilePath(symbol, column), bTree);
            }
        }

        private IEnumerable<IStream> CollectAndFetch(string symbol, string column, DateTime start, DateTime end)
        {
            // Load from start to entry.Key
            foreach (var meta in _dataProvider.Fetch(symbol, start, end))
            {
                // Todo: Update btree entries by locking and save metadata
                AddMeta(meta);
                if (meta.Column == column && meta.FilePath.FileExists())
                    yield return _factory.Create(meta);
            }
        }

        #endregion

        private BTree<DateTime, FileMetaData> GetBTree(string symbol, string column)
        {
            var key = GetKey(symbol, column);
            if (!_bTrees.TryGetValue(key, out var bTree))
            {
                bTree = LoadBTree(_filePathProvider.GetMetaFilePath(symbol, column), key) ?? CreateBTree();
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

        private static BTree<DateTime, FileMetaData> LoadBTree(string metaFilePath, string key)
        {
            // SpinLock file meta for multi processes concurrency
            using (metaFilePath.FileLock())
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
            using (metaFilePath.FileLock())
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
}