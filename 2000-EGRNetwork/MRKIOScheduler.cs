using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using static MRK.EGRLogger;

namespace MRK {
    public enum MRKIOSchedulerPriority {
        Low, High
    }

    public class MRKIOScheduler : MRKBehaviour {
        struct FileInfo {
            public string Path { get; set; }
            public bool IsDirectory { get; set; }

            public override string ToString() {
                return $"Path=\"{Path}\" IsDirectory={IsDirectory}";
            }
        }

        readonly Queue<FileInfo> m_DeletionQueue;

        public MRKIOScheduler() {
            m_DeletionQueue = new Queue<FileInfo>();
        }

        public void Start() {
            LogInfo("Initializing IOScheduler");
            Task.Run(DeletionThread);
        }

        void QueueDeletion(string path, bool isDir) {
            lock (m_DeletionQueue) {
                m_DeletionQueue.Enqueue(new FileInfo { Path = path, IsDirectory = isDir });
            }
        }

        void DeleteImmediate(string path, bool isDir) {
            Task.Run(async () => {
                FileInfo fileInfo = new() {
                    Path = path,
                    IsDirectory = isDir
                };

                await DeleteFileInfo(fileInfo);
            });
        }

        public void DeleteDirectory(string path, MRKIOSchedulerPriority priority) {
            if (priority == MRKIOSchedulerPriority.Low) {
                QueueDeletion(path, true);
                return;
            }

            DeleteImmediate(path, true);
        }

        public void DeleteFile(string path, MRKIOSchedulerPriority priority) {
            if (priority == MRKIOSchedulerPriority.Low) {
                QueueDeletion(path, false);
                return;
            }

            DeleteImmediate(path, false);
        }

        async Task DeleteFileAsync(string path) {
            //assuming the file exists
            await Task.Run(() => { File.Delete(path); });
        }

        async Task DeleteDirectoryAsync(string path) {
            //assuming the dir exists
            await Task.Run(() => { Directory.Delete(path); });
        }

        async Task DeletionThread() {
            LogInfo("[IO Scheduler] Deletion thread starting");

            int interval = Client.Config["IO_SCHED_DELETE_INTERVAL"].Int;
            LogInfo($"[IO Scheduler] Deletion thread interval={interval}");

            while (Client.IsRunning) {
                if (m_DeletionQueue.Count > 0) {
                    FileInfo fileInfo = default;

                    lock (m_DeletionQueue) {
                        fileInfo = m_DeletionQueue.Dequeue();
                    }

                    await DeleteFileInfo(fileInfo);
                }

                await Task.Delay(interval);
            }

            LogInfo("[IO Scheduler] Deletion thread exitting");
        }

        async Task DeleteFileInfo(FileInfo fileInfo) {
            try {
                if ((fileInfo.IsDirectory && Directory.Exists(fileInfo.Path))
                    || (!fileInfo.IsDirectory && File.Exists(fileInfo.Path))) {
                    if (!fileInfo.IsDirectory) {
                        await DeleteFileAsync(fileInfo.Path);
                    }
                    else {
                        await DeleteDirectoryAsync(fileInfo.Path);
                    }
                }
            }
            catch {
                LogError($"[IO Scheduler] Error deleting {fileInfo}");
            }
        }
    }
}
