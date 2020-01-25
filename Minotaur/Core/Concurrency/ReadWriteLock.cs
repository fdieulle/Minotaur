using System;
using System.Threading;

namespace Minotaur.Core.Concurrency
{
    public class ReadWriteLock : IDisposable
    {
        private readonly ReaderWriterLockSlim _readerWriterLockSlim = new ReaderWriterLockSlim();
        private readonly Disposable _releaseRead;
        private readonly Disposable _releaseWrite;
        private int _nbReaders;

        public ReadWriteLock()
        {
            _releaseRead = new Disposable(ReleaseRead);
            _releaseWrite = new Disposable(ReleaseWrite);
        }

        public IDisposable AcquireWrite()
        {
            _readerWriterLockSlim.EnterWriteLock();
            OnAcquireWrite();
            return _releaseWrite;
        }

        private void ReleaseWrite()
        {
            try
            {
                OnReleaseWrite();
            }
            finally
            {
                _readerWriterLockSlim.ExitWriteLock();
            }
        }

        protected virtual void OnAcquireWrite() { }

        protected virtual void OnReleaseWrite() { }

        public IDisposable AcquireRead()
        {
            _readerWriterLockSlim.EnterReadLock();
            if (Interlocked.Increment(ref _nbReaders) > 1) return _releaseRead;

            OnAcquireRead();
            
            return _releaseRead;
        }

        private void ReleaseRead()
        {
            try
            {
                if (Interlocked.Decrement(ref _nbReaders) == 0)
                {
                    lock (_readerWriterLockSlim)
                    {
                        OnReleaseRead();
                    }
                }
            }
            finally
            {
                _readerWriterLockSlim.ExitReadLock();
            }
        }

        protected virtual void OnAcquireRead() { }

        protected virtual void OnReleaseRead() { }

        #region IDisposable

        public void Dispose()
        {
            AcquireWrite().Dispose();
            _readerWriterLockSlim.Dispose();
        }

        #endregion

        private class Disposable : IDisposable
        {
            private readonly Action _dispose;

            public Disposable(Action dispose) => _dispose = dispose;

            public void Dispose() => _dispose();
        }
    }
}