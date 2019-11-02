using System;
using System.Collections.Generic;
using System.IO;
using Minotaur.Core;
using Minotaur.Providers;

namespace Minotaur.Meta
{
    public class Schema
    {
        // Todo: Find a way to support many reads but only 1 write by symbol
        private readonly IFilePathProvider _filePathProvider;
        private readonly Dictionary<string, TimeSeries> _symbols = new Dictionary<string, TimeSeries>();

        public Schema(IFilePathProvider filePathProvider)
        {
            _filePathProvider = filePathProvider;
        }

        public bool HasChanged(string symbol)
        {
            var metaFile = _filePathProvider.GetMetaFilePath(symbol);
            lock (_symbols) // Protection against other threads
            {
                using (metaFile.LockFile()) // Protection against other processes and machines
                {
                    if (!_symbols.TryGetValue(symbol, out var meta))
                        _symbols.Add(symbol, meta = new TimeSeries(symbol));

                    var lastWriteTime = File.GetLastWriteTimeUtc(metaFile);
                    if (lastWriteTime > meta.LastWriteTimeUtc)
                    {
                        meta.Restore(metaFile);
                        meta.LastWriteTimeUtc = lastWriteTime;
                        return true;
                    }

                    return false;
                }
            }
        }

        public IDisposable OpenToRead(string symbol, out ITimeSeries timeSeries)
        {
            var metaFile = _filePathProvider.GetMetaFilePath(symbol);
            lock (_symbols) // Protection against other threads
            {
                var locker = metaFile.LockFile(); // Protection against other processes and machines

                if(!_symbols.TryGetValue(symbol, out var meta))
                    _symbols.Add(symbol, meta = new TimeSeries(symbol));
                timeSeries = meta;

                var lastWriteTime = File.GetLastWriteTimeUtc(metaFile);
                if (lastWriteTime > meta.LastWriteTimeUtc)
                {
                    meta.Restore(metaFile);
                    meta.LastWriteTimeUtc = lastWriteTime;
                }

                return locker;
            }
        }

        public IDisposable OpenToWrite(string symbol, out ITimeSeries timeSeries)
        {
            var metaFile = _filePathProvider.GetMetaFilePath(symbol);
            lock (_symbols) // Protection against other threads
            {
                var locker = metaFile.LockFile(); // Protection against other processes and machines

                if (!_symbols.TryGetValue(symbol, out var meta))
                    _symbols.Add(symbol, meta = new TimeSeries(symbol));
                timeSeries = meta;

                var lastWriteTime = File.GetLastWriteTimeUtc(metaFile);
                if (lastWriteTime > meta.LastWriteTimeUtc)
                {
                    meta.Restore(metaFile);
                    meta.LastWriteTimeUtc = lastWriteTime;
                }

                return Disposable.Create(() =>
                {
                    try
                    {
                        meta.Persist(metaFile);
                        meta.LastWriteTimeUtc = File.GetLastWriteTimeUtc(metaFile);
                    }
                    catch (Exception)
                    {
                        // Todo: LogError "An exception happens when persist symbol meta data. Symbol: {symbol}, File: {metaFile}"
                    }
                    finally
                    {
                        locker.Dispose();
                    }
                });
            }
        }
    }
}