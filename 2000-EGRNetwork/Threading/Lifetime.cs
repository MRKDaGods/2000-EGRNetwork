using System;
using MRK.System;
using System.Threading;

namespace MRK.Threading
{
    public partial class Lifetime
    {
        private static ThreadPool _threadPool;

        public static void Initialize(ThreadPool threadPool)
        {
            _threadPool = threadPool;
        }

        public static Frame<T> Attach<T>(T obj, float lifetime, Action<T> dispose) where T : class
        {
            if (_threadPool == null) throw new NullReferenceException("Lifetime has not been initialized properly");
            if (obj == null || lifetime <= float.Epsilon || dispose == null) return null;

            Frame<T> frame = new(Time.RelativeTimeSeconds, obj, lifetime, dispose);
            _threadPool.Run(() => InternalAttach(frame));
            return frame;
        }

        public static Frame<T> Attach<T>(T obj, Predicate<T> running, Action<T> dispose) where T : class
        {
            if (_threadPool == null) throw new NullReferenceException("Lifetime has not been initialized properly");
            if (obj == null || running == null || dispose == null) return null;

            Frame<T> frame = new(Time.RelativeTimeSeconds, obj, running, dispose);
            _threadPool.Run(() => InternalAttach(frame));
            return frame;
        }

        private static void InternalAttach<T>(Frame<T> frame)
        {
            do
            {
                //allow time for external ondemand disposal
                Thread.Sleep(150);
            }
            while (frame.Interlocked(() => frame.Running));

            frame.Dispose();
        }
    }
}
