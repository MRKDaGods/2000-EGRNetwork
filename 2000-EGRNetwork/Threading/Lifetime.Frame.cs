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
            private readonly float _lifetime;
            private readonly Action<T> _dispose;
            private bool _disposed;

            public bool Running
            {
                get { return Time.RelativeTimeSeconds < _startTime + _lifetime && !_disposed; }
            }

            public Frame(float startTime, T obj, float lifetime, Action<T> dispose)
            {
                _startTime = startTime;
                _object = obj;
                _lifetime = lifetime;
                _dispose = dispose;
                _disposed = false;
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
