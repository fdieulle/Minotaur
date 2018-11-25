using System;

namespace Minotaur.Core.Anonymous
{
    public class AnonymousDisposable : IDisposable
    {
        public static IDisposable Empty { get; } =  new AnonymousDisposable(null);

        private Action _onDispose;

        public AnonymousDisposable(Action onDispose)
        {
            _onDispose = onDispose;
        }

        #region IDisposable

        public void Dispose()
        {
            var a = _onDispose;
            if (a == null) return;
            a();
            _onDispose = null;
        }

        #endregion
    }
}
