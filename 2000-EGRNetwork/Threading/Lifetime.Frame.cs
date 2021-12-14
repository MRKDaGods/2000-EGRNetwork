using System;
using MRK.System;

namespace MRK.Threading
{
    public partial class Lifetime
    {
        public class Frame<T> : InterlockedAccess
        {
            private readonly float _startTime;
            private readonly T _object;
            private readonly Action<T> _dispose;
            private bool _disposed;
            private readonly Predicate<T> _running;

            public bool Running
            {
                get { return !_disposed && _running(_object); }
            }

            public Frame(float startTime, T obj, float lifetime, Action<T> dispose)
            {
                _startTime = startTime;
                _object = obj;
                _dispose = dispose;
                _disposed = false;

                _running = (x) => {
                    return Time.RelativeTimeSeconds < _startTime + lifetime;
                };
            }

            public Frame(float startTime, T obj, Predicate<T> running, Action<T> dispose)
            {
                _startTime = startTime;
                _object = obj;
                _dispose = dispose;
                _disposed = false;
                _running = running;
            }

            public void Dispose()
            {
                if (_disposed) return;

                _disposed = true;
                _dispose(_object);
            }
        }
    }
}
