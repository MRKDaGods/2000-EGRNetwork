using System;
using SysInterlocked = System.Threading.Interlocked;

namespace MRK.Threading
{
    public class InterlockedAccess
    {
        private readonly object _lock;
        private int _accessible;

        public bool Accessible
        {
            get { return _accessible == 1; }
        }

        public InterlockedAccess()
        {
            _lock = new object();
            _accessible = 1;
        }

        public void Interlocked(Action action)
        {
            if (action == null) return;

            lock (_lock)
            {
                //by the time we acquire the lock, the object may not be valid for locking
                if (Accessible)
                {
                    action();
                }
            }
        }

        public T Interlocked<T>(Func<T> action)
        {
            if (action == null) return default;

            lock (_lock)
            {
                //by the time we acquire the lock, the object may not be valid for locking
                if (Accessible)
                {
                    return action();
                }
            }

            return default;
        }

        public void PreventFutureAccess()
        {
            SysInterlocked.Exchange(ref _accessible, 0);
        }
    }
}
