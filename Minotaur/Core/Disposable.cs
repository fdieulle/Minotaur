using System;

namespace Minotaur.Core
{
    public class Disposable<T> : IDisposable
    {
        private Action<T> _onDispose;
        private T _parameter;

        protected Disposable(Action<T> onDispose, T parameter)
        {
            _onDispose = onDispose;
            _parameter = parameter;
        }

        #region IDisposable

        public void Dispose()
        {
            var a = _onDispose;
            if (a == null) return;
            a(_parameter);
            _onDispose = null;
            _parameter = default;
        }

        #endregion

        public static IDisposable Create(Action<T> onDispose, T parameter)
            => new Disposable<T>(onDispose, parameter);
    }

    public class Disposable : Disposable<object>
    {
        public static IDisposable Empty { get; } = new Disposable(null, null);

        public static IDisposable Create(Action onDispose)
            => new Disposable(p => onDispose(), null);

        public static IDisposable Create<T>(Action<T> onDispose, T parameter)
            => Disposable<T>.Create(onDispose, parameter);

        protected Disposable(Action<object> onDispose, object parameter) 
            : base(onDispose, parameter) { }
    }
}
