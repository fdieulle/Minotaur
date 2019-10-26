using System;
using System.Threading;

namespace Minotaur.Core.Concurrency
{
    public class ReadWriteLock : IDisposable
    {
        private readonly Semaphore _resourceAccess = new Semaphore(1, 1); // like a lock Todo: See to replace with Monitor.Enter/Exit
        private readonly Semaphore _readCountAccess = new Semaphore(1, 1); // like a lock Todo: See to replace with Monitor.Enter/Exit
        private readonly Semaphore _serviceQueue = new Semaphore(1, 1); // like a lock Todo: See to replace with Monitor.Enter/Exit
        private readonly Disposable _releaseRead;
        private readonly Disposable _releaseWrite;
        private int _readCount;

        public ReadWriteLock()
        {
            _releaseRead = new Disposable(ReleaseRead);
            _releaseWrite = new Disposable(ReleaseWrite);
        }

        public IDisposable AcquireWrite()
        {
            _serviceQueue.WaitOne(); // Wait in line to be served
            // <Enter>
            // Todo: Put a timeout here !
            _resourceAccess.WaitOne(); // Request exclusive access to resource 
            OnAcquireWrite();
            // </Enter>
            _serviceQueue.Release();

            // writing is performed until dispose this object
            return _releaseWrite;
        }

        private void ReleaseWrite()
        {
            // <Exit>
            OnReleaseWrite();
            _resourceAccess.Release(); // release resource access for next reader/writer
            // </Exit>
        }

        protected virtual void OnAcquireWrite() { }

        protected virtual void OnReleaseWrite() { }

        public IDisposable AcquireRead()
        {
            _serviceQueue.WaitOne(); // wait in line to be serviced
            _readCountAccess.WaitOne(); // request exclusive access to readCount
            // <Enter>
            if (_readCount == 0) // if there are no readers already reading:
            {
                _resourceAccess.WaitOne(); // request resource access for readers (writers blocked)
                OnAcquireRead();
            }
            _readCount++;
            // </Enter>
            _serviceQueue.Release(); // let next in line be serviced
            _readCountAccess.Release();// release access to readCount

            // <Read /> reading is performed until dispose this object.

            return _releaseRead;
        }

        private void ReleaseRead()
        {
            _readCountAccess.WaitOne(); // request exclusive access to readCount

            // <Exit>
            _readCount--; // update count of active readers
            if (_readCount == 0) // if there are no readers left:
            {
                OnReleaseRead();
                _resourceAccess.Release(); // release resource access for all
            }
            // </Exit>

            _readCountAccess.Release(); // release access to readCount
        }

        protected virtual void OnAcquireRead() { }

        protected virtual void OnReleaseRead() { }

        #region IDisposable

        public void Dispose()
        {
            AcquireWrite().Dispose();
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