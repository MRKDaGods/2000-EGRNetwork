using System;
using System.Collections.Generic;
using System.Threading;

namespace MRK.Threading
{
    public partial class ThreadPool
    {
        private class InternalThread
        {
            private Thread _thread;
            private bool _running;
            private readonly ThreadPool _owner;
            private readonly int _interval;
            private DateTime? _inactivityStartTime;
            private readonly Queue<Action> _tasks;

            public int QueueSize
            {
                get { return _tasks.Count; }
            }

            public InternalThread(ThreadPool owner, int interval)
            {
                _owner = owner;
                _interval = interval;
                _tasks = new Queue<Action>();
            }

            public void Activate()
            {
                _running = true;
                _thread = new Thread(ThreadLoop);
                _thread.Start();
            }

            public void InterlockedQueueTask(Action action)
            {
                lock (_tasks)
                {
                    _tasks.Enqueue(action);
                }
            }

            public void Terminate(bool withOwnerSuspension = true)
            {
                _running = false;
                _thread = null;

                if (withOwnerSuspension)
                {
                    _owner.SuspendThread(this);
                }
            }

            private void ThreadLoop()
            {
                while (_running)
                {
                    if (_tasks.Count > 0)
                    {
                        _inactivityStartTime = null;

                        Action act;
                        //quick lock
                        lock (_tasks)
                        {
                            act = _tasks.Dequeue();
                        }

                        act.Invoke();
                    }
                    else
                    {
                        if (!_inactivityStartTime.HasValue)
                            _inactivityStartTime = DateTime.Now;

                        if ((DateTime.Now - _inactivityStartTime.Value).TotalMilliseconds > INACTIVITY_TIMER)
                        {
                            _inactivityStartTime = null;
                            Terminate();
                        }

                        Thread.Sleep(_interval);
                    }
                }
            }
        }
    }
}