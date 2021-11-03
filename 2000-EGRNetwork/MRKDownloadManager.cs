using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace MRK {
    public class MRKDownloadManager : MRKBehaviour {
        public class DownloadInfo {
            public string Request;
            public byte[] Data;
            public Action<DownloadInfo> Callback;
            public Exception Failure;
            public float ElapsedTime;
        }

        readonly HttpClient m_Client;
        readonly HashSet<DownloadInfo> m_ActiveDownloads;
        readonly int m_MaxDownloadCount;
        readonly Queue<DownloadInfo> m_QueuedDownloads;

        public MRKDownloadManager() {
            m_Client = new HttpClient();
            m_ActiveDownloads = new HashSet<DownloadInfo>();

            m_MaxDownloadCount = Client.Config["DOWNLOAD_MGR_MAX_DOWNLOADS"].Int;
            ServicePointManager.DefaultConnectionLimit = m_MaxDownloadCount;

            m_QueuedDownloads = new Queue<DownloadInfo>();
        }

        public void Download(string url, Action<DownloadInfo> callback) {
            if (callback == null) //we must have a callback, else download would be useless
                return;

            DownloadInfo info = new DownloadInfo {
                Request = url,
                Callback = callback,
                Failure = null,
                Data = null
            };

            if (m_ActiveDownloads.Count > m_MaxDownloadCount) {
                QueueDownload(info);
                return;
            }

            StartDownload(info);
        }

        void QueueDownload(DownloadInfo info) {
            lock (m_QueuedDownloads) {
                m_QueuedDownloads.Enqueue(info);
            }
        }

        void StartDownload(DownloadInfo info) {
            lock (m_ActiveDownloads) {
                m_ActiveDownloads.Add(info);
            }

            Task.Run(async () => await HandleDownload(info));
        }

        async Task HandleDownload(DownloadInfo info) {
            try {
                DateTime startTime = DateTime.Now;
                info.Data = await m_Client.GetByteArrayAsync(info.Request);
                info.ElapsedTime = (float)(DateTime.Now - startTime).TotalSeconds;
            }
            catch (Exception ex) {
                info.Failure = ex;
            }

            info.Callback(info);

            lock (m_ActiveDownloads) {
                m_ActiveDownloads.Remove(info);
            }

            if (m_QueuedDownloads.Count > 0) {
                lock (m_QueuedDownloads) {
                    DownloadInfo queued = m_QueuedDownloads.Dequeue();
                    StartDownload(queued);
                }
            }
        }
    }
}
