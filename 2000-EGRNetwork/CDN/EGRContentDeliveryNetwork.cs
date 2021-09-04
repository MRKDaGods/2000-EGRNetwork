using System.Collections.Generic;
using System.Threading;
using static MRK.EGRLogger;

namespace MRK.Networking {
    public class EGRContentDeliveryNetwork : EGRBase {
        public class CDN {
            public EGRNetwork Network;
            public Thread Thread;
        }

        readonly List<CDN> m_CDNs;
        int m_CDNIndex;

        public EGRCDNResourceManager ResourceManager { get; private set; }

        public EGRContentDeliveryNetwork() {
            EGRNetworkConfig config = Client.Config;
            int cdnCount = config["NET_CDN_COUNT"].Int;
            int cdnBasePort = config["NET_CDN_BASE_PORT"].Int;
            string cdnKey = config["NET_CDN_KEY"].String;
            m_CDNs = new List<CDN>(cdnCount);
            for (int i = 0; i < cdnCount; i++) {
                CDN cdn = new CDN {
                    Network = new EGRNetwork($"CDN{i}", cdnBasePort + i, cdnKey, true)
                };

                m_CDNs.Add(cdn);
            }

            ResourceManager = new EGRCDNResourceManager();
        }

        public void Start() {
            ResourceManager.Initialize(Client.Config);

            //init our cdn threads
            int cdnInterval = Client.Config["NET_CDN_THREAD_INTERVAL"].Int;
            foreach (CDN cdn in m_CDNs) {
                cdn.Network.Start();
                cdn.Thread = new Thread(() => CDNThread(cdn, cdnInterval));
                cdn.Thread.Start();
            }
        }

        void CDNThread(CDN cdn, int interval) {
            LogInfo($"Starting CDN thread, name={cdn.Network.Name} interval={interval}ms");

            while (Client.IsRunning) {
                cdn.Network.UpdateNetwork();
                Thread.Sleep(interval);
            }

            LogInfo($"CDN({cdn.Network.Name}) thread is exiting");
        }

        public EGRCDNInfo AllocateCDN() {
            if (m_CDNs.Count == 0)
                return null;

            CDN cdn = m_CDNs[m_CDNIndex++ % m_CDNs.Count];
            return new EGRCDNInfo {
                Port = cdn.Network.Port,
                Key = cdn.Network.Key,
                CDN = cdn
            };
        }
    }
}
