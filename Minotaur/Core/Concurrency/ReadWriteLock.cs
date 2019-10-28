using System;
using System.Threading;

namespace Minotaur.Core.Concurrency
{
    public class ReadWriteLock : IDisposable
    {
        private readonly ReaderWriterLockSlim _readerWriterLockSlim = new ReaderWriterLockSlim();
        private readonly Disposable _releaseRead;
        private readonly Disposable _releaseWrite;
        private bool _isAcquired;

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
            OnReleaseWrite();
            _readerWriterLockSlim.ExitWriteLock();
        }

        protected virtual void OnAcquireWrite() { }

        protected virtual void OnReleaseWrite() { }

        public IDisposable AcquireRead()
        {
            _readerWriterLockSlim.EnterReadLock();
            if (_isAcquired) return _releaseRead;

            lock (_readerWriterLockSlim)
            {
                OnAcquireRead();
                _isAcquired = true;
            }

            return _releaseRead;
        }

        private void ReleaseRead()
        {
            _readerWriterLockSlim.ExitReadLock();
            if (_readerWriterLockSlim.CurrentReadCount != 0) return;

            lock (_readerWriterLockSlim)
            {
                if (_readerWriterLockSlim.CurrentReadCount != 0) return;

                OnReleaseRead();
                _isAcquired = false;
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