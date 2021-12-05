using System;
using System.Collections.Generic;

namespace MRK.Threading
{
    public partial class ThreadPool
    {
        const int INACTIVITY_TIMER = 5000;

        private readonly int _interval;
        private readonly int _maxThreadCount;
        private readonly List<InternalThread> _freeThreads;
        private readonly List<InternalThread> _activeThreads;
        private readonly object _threadLock;

        public ThreadPool(int interval, int maxThreadCount)
        {
            _interval = interval;
            _maxThreadCount = maxThreadCount;
            _freeThreads = new List<InternalThread>();
            _activeThreads = new List<InternalThread>();
            _threadLock = new object();
        }

        public void Run(Action action)
        {
            if (!InternalQueueTask(action))
            {
                //need to spawn a new thread
                AcquireNewThread().InterlockedQueueTask(action);
            }
        }

        bool InternalQueueTask(Action action)
        {
            if (_activeThreads.Count == 0)
            {
                return false;
            }

            InternalThread lowestThread = null;
            int queueSz = 100;

            lock (_threadLock)
            {
                EGRUtils.Iterator(_activeThreads, (item, exit) => {
                    if (item.QueueSize < queueSz)
                    {
                        lowestThread = item;
                        queueSz = item.QueueSize;
                    }

                    if (queueSz == 0)
                    {
                        exit.Value = true;
                    }
                });
            }

            //spawn a new thread and queue to it instead
            if (queueSz > 0 && _activeThreads.Count < _maxThreadCount)
                return false;

            lowestThread.InterlockedQueueTask(action);
            return true;
        }

        InternalThread AcquireNewThread()
        {
            InternalThread thread;
            if (_freeThreads.Count > 0)
            {
                thread = _freeThreads[0];
                _freeThreads.RemoveAt(0);
            }
            else
            {
                thread = new InternalThread(this, _interval);
            }

            thread.Activate();

            lock (_threadLock)
            {
                _activeThreads.Add(thread);
            }

            return thread;
        }

        void SuspendThread(InternalThread thread)
        {
            lock (_threadLock)
            {
                _activeThreads.Remove(thread);
                _freeThreads.Add(thread);
            }
        }

        public void Terminate()
        {
            if (_activeThreads.Count > 0)
            {
                lock (_activeThreads)
                {
                    foreach (InternalThread thread in _activeThreads)
                    {
                        thread.Terminate(false);
                    }
                }
            }
        }
    }
}