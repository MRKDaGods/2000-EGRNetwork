using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace MRK {
    public class MRKWorker {
        class RunLock {
            public volatile bool Running;
        }

        readonly Queue<Action> m_QueuedActions;
        readonly Thread m_Thread;
        readonly RunLock m_RunLock;

        public MRKWorker() {
            m_QueuedActions = new Queue<Action>();
            m_Thread = new Thread(WorkerLoop);
            m_RunLock = new RunLock {
                Running = true
            };
        }

        void WorkerLoop() {
            while (m_RunLock.Running) {
                lock (m_QueuedActions) {
                    if (m_QueuedActions.Count > 0) {
                        Action action = m_QueuedActions.Dequeue();
                        action();
                    }
                }

                Thread.Sleep(500);
            }
        }

        public void Queue(Action action) {
            lock (m_QueuedActions) {
                m_QueuedActions.Enqueue(action);
            }
        }

        public void StartWorker() {
            m_Thread.Start();
        }

        public void Terminate() {
            lock (m_RunLock) {
                m_RunLock.Running = false;
            }
        }
    }
}
