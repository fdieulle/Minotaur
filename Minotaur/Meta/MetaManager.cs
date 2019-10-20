using System;
using System.Collections.Generic;
using System.IO;
using Minotaur.Core;
using Minotaur.Core.Anonymous;
using Minotaur.Providers;

namespace Minotaur.Meta
{
    public class MetaManager
    {
        // Todo: Find a way to support many reads but only 1 write by symbol
        private readonly IFilePathProvider _filePathProvider;
        private readonly Dictionary<string, SymbolMeta> _symbols = new Dictionary<string, SymbolMeta>();

        public MetaManager(IFilePathProvider filePathProvider)
        {
            _filePathProvider = filePathProvider;
        }

        public IDisposable OpenMetaToRead(string symbol, out ISymbolMeta symbolMeta)
        {
            var metaFile = _filePathProvider.GetMetaFilePath(symbol);
            lock (_symbols) // Protection against other threads
            {
                var locker = metaFile.FileLock(); // Protection against other processes and machines

                if(!_symbols.TryGetValue(symbol, out var meta))
                    _symbols.Add(symbol, meta = new SymbolMeta(symbol));
                symbolMeta = meta;

                var lastWriteTime = File.GetLastWriteTimeUtc(metaFile);
                if (lastWriteTime > meta.LastWriteTimeUtc)
                {
                    meta.Restore(metaFile);
                    meta.LastWriteTimeUtc = lastWriteTime;
                }

                return locker;
            }
        }

        public IDisposable OpenMetaToWrite(string symbol, out ISymbolMeta symbolMeta)
        {
            var metaFile = _filePathProvider.GetMetaFilePath(symbol);
            lock (_symbols) // Protection against other threads
            {
                var locker = metaFile.FileLock(); // Protection against other processes and machines

                if (!_symbols.TryGetValue(symbol, out var meta))
                    _symbols.Add(symbol, meta = new SymbolMeta(symbol));
                symbolMeta = meta;

                var lastWriteTime = File.GetLastWriteTimeUtc(metaFile);
                if (lastWriteTime > meta.LastWriteTimeUtc)
                {
                    meta.Restore(metaFile);
                    meta.LastWriteTimeUtc = lastWriteTime;
                }

                return new AnonymousDisposable(() =>
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