using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Minotaur.Core;

namespace Minotaur.Providers
{
    public class FilePathProvider : IFilePathProvider
    {
        private readonly string _metaFolder;
        private readonly string _dataFolder;
        private readonly Tuple<DateTime, string>[] _rootByDates;

        public FilePathProvider(string root, IEnumerable<Tuple<DateTime, string>> rootByDates = null)
        {
            root = root ?? string.Empty;
            _metaFolder = Path.Combine(root, "Meta").CreateFolderIfNotExist();
            _dataFolder = Path.Combine(root, "Data").CreateFolderIfNotExist();

            _rootByDates = (rootByDates ?? Enumerable.Empty<Tuple<DateTime, string>>())
                .Where(p => p.Item2 != null)
                .OrderBy(p => p.Item1).ToArray();
        }

        #region Implementation of IFilePathProvider

        public string GetMetaFilePath(string symbol, string column)
            => Path.Combine(_metaFolder, $"{symbol}_{column}.meta");

        public string GetFilePath(string symbol, string column, DateTime timestamp)
            => Path.Combine(GetRootFolder(timestamp), 
                timestamp.ToString("yyyy"),
                symbol ?? string.Empty, 
                column ?? string.Empty,
                $"{symbol}_{column}_{timestamp:yyyy-MM-dd_HH-mm-ss}.min");

        #endregion

        private string GetRootFolder(DateTime timestamp)
        {
            var idx = -1;
            for (var i = 0; i < _rootByDates.Length; i++)
            {
                if (timestamp >= _rootByDates[i].Item1)
                    idx++;
                else break;
            }

            return idx < 0 ? _dataFolder : _rootByDates[idx].Item2;
        }
    }
}
