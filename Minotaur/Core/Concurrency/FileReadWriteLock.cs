using System;
using System.IO;
using System.Threading;

namespace Minotaur.Core.Concurrency
{
    /// <summary>
    /// File concurrency access for many readers and an exclusive access for one writer.
    /// This locking mechanism should only be use for multi processes and multi machines concurrency problem.
    ///
    /// This implementation plays with the following data.
    ///     - A lock file for mutex purpose
    ///     - Creation file time. Writes only by writer.
    ///     - Last write file access. Writes only by readers. This timestamp is used as readers counter.
    ///       I prefer to use the write access than read access because we never know if another program
    ///       or a user open the file manually. If another write access is did the file will probably corrupt.
    ///
    /// Lock file usage:
    ///     - Writer: Keep the lock between <see cref="OnAcquireWrite()"/> and <see cref="OnReleaseWrite()"/>. So during the whole writing process.
    ///     - Reader: Acquire and release the lock on <see cref="OnAcquireRead()"/> and <see cref="OnReleaseRead()"/> each other. So no lock keep during the reading process.
    /// 
    /// Writer access:
    ///     1. Acquire file lock
    ///     2. Read creation time and last access time
    ///     3. Set the creation time in the future to indicates for new readers that a writer is waiting. This mechanism avoid writer starvation.
    ///     4. if the last access time lower or equals => success
    ///        else wait until timeout
    /// Writer release:
    ///     1. Enforce the creation time and last access time. Means reset counter
    ///     2. Release file lock
    ///
    /// Reader access:
    ///     1. Acquire file lock
    ///     2. Check if no writer is waiting otherwise wait until the writer ends
    ///     3. Increment the counter
    ///     4. Release file lock
    /// Reader release:
    ///     1. Acquire file lock
    ///     2. Decrement the counter
    ///     3. Release file lock
    /// 
    /// </summary>
    public class FileReadWriteLock : ReadWriteLock
    {
        private static readonly Random random = new Random();

        private readonly string _filePath;
        private readonly int _writeTimeoutMs;
        private IDisposable _lock;

        public FileReadWriteLock(string filePath, int writeTimeoutMs = 30000)
        {
            _filePath = filePath;
            _writeTimeoutMs = writeTimeoutMs;
        }

        protected override void OnAcquireWrite()
        {
            // When the lock is taken no more new readers and writers can access to the file.
            // But it can still have some older readers on the file.
            _lock = _filePath.LockFile();

            // Stop here if the file doesn't exist yet or anymore.
            if (!_filePath.FileExists()) return;

            var creationUtc = File.GetCreationTimeUtc(_filePath);
            var lastAccessUtc = File.GetLastWriteTimeUtc(_filePath);

            // Invalidate the creation Time to indicate readers that a writer is waiting and avoid writer starvation
            File.SetCreationTimeUtc(_filePath, lastAccessUtc.AddDays(2));

            // Try acquire write through readers
            // Wait until readers end or an access timeout.
            var totalWait = 0;
            while (lastAccessUtc > creationUtc)
            {
                _lock.Dispose();
                var waitMs = Sleep();
                _lock = _filePath.LockFile();

                totalWait += waitMs;
                if (totalWait > _writeTimeoutMs) break; // Timeout
                
                lastAccessUtc = File.GetLastWriteTimeUtc(_filePath);
            }
        }

        protected override void OnReleaseWrite()
        {
            // Be sure that the check points are consistent by overwrite them
            if (_filePath.FileExists())
            {
                var utcNow = DateTime.UtcNow;
                File.SetCreationTimeUtc(_filePath, utcNow);
                File.SetLastWriteTimeUtc(_filePath, utcNow);
            }
            
            _lock?.Dispose();
        }

        protected override void OnAcquireRead()
        {
            while (true)
            {
                using (_filePath.LockFile())
                {
                    // Stop here if the file doesn't exist yet or anymore.
                    if (!_filePath.FileExists()) break;

                    // Read counter
                    var lastAccessUtc = File.GetLastWriteTimeUtc(_filePath);

                    // A writer is waiting so we block the new readers access
                    if (File.GetCreationTimeUtc(_filePath) > lastAccessUtc)
                    {
                        Sleep();
                        continue;
                    }

                    // Increment counter
                    lastAccessUtc = new DateTime(lastAccessUtc.Ticks + 1, DateTimeKind.Utc);
                    // Write counter
                    File.SetLastWriteTimeUtc(_filePath, lastAccessUtc);
                    break;
                }
            }
        }

        protected override void OnReleaseRead()
        {
            using (_filePath.LockFile())
            {
                // Stop here if the file doesn't exist yet or anymore.
                if (!_filePath.FileExists()) return;

                // Read counter
                var lastAccessUtc = File.GetLastWriteTimeUtc(_filePath);
                // Decrement counter
                lastAccessUtc = new DateTime(lastAccessUtc.Ticks + 1, DateTimeKind.Utc);
                // Write counter
                File.SetLastWriteTimeUtc(_filePath, lastAccessUtc);
            }
        }

        private int Sleep()
        {
            var waitMs = random.Next(1, 1000); // Randomize the waiting time
            Thread.Sleep(waitMs);
            return waitMs;
        }
    }
}