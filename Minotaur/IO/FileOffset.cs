using System;
using Minotaur.Core;

namespace Minotaur.IO
{
    public class FileOffset
    {
        public string FilePath { get; }
        public long Offset { get; }

        public FileOffset(string filePath, long offset)
        {
            FilePath = filePath;
            Offset = offset;
        }

        public bool FileExists() => FilePath.FileExists();
        public IDisposable AcquireReadLock() => FilePath.AcquireReadLock();
    }
}