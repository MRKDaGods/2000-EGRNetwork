using MRK.Networking;
using MRK.WTE;
using System;
using System.Threading;
using static MRK.EGRLogger;
using static System.Console;

namespace MRK {
    public class EGRMain {
        EGRNetworkConfig m_Config;
        EGRNetwork m_Network;
        EGRContentDeliveryNetwork m_CDNNetwork;
        Thread m_NetThread;

        public static EGRMain Instance { get; private set; }
        public bool IsRunning { get; set; }
        public EGRNetworkConfig Config => m_Config;
        public string WorkingDirectory { get; private set; }
        public EGRAccountManager AccountManager { get; private set; }
        public EGRPlaceManager PlaceManager { get; private set; }
        public EGRTileManager TileManager { get; private set; }
        public EGRWTE WTE { get; private set; }
        public DateTime StartTime { get; private set; }
        public TimeSpan RelativeTime => DateTime.Now - StartTime;
        public EGRNetwork MainNetwork => m_Network;
        public EGRContentDeliveryNetwork CDNNetwork => m_CDNNetwork;
        public MRKIOScheduler IOScheduler { get; private set; }
        public MRKDownloadManager DownloadManager { get; private set; }

        public static void Main(string[] _) {
            if (Instance == null)
                Instance = new EGRMain();

            Instance._main();
        }

        void _main() {
            StartTime = DateTime.Now;
            IsRunning = true;

            LogInfo("2000-EGR Network - MRKDaGods(Mohamed Ammar)");
            LogInfo("Starting...");

            m_Config = new EGRNetworkConfig();
            if (!m_Config.Load()) {
                m_Config.WriteDefaultConfig(); //create config file
                LogError("Network config loading failed...");
                Exit();
                return;
            }

            MRKCryptography.CookSalt();

            IOScheduler = new();
            IOScheduler.Start();

            m_Network = new EGRNetwork("MAIN", m_Config["NET_PORT"].Int, m_Config["NET_KEY"].String);
            if (!m_Network.Start()) {
                LogInfo("Failed to start!");
                Exit();
                return;
            }

            LogInfo($"Network successfully initialized");

            WorkingDirectory = m_Config["NET_WORKING_DIR"].String;
            (AccountManager = new EGRAccountManager()).Initialize(WorkingDirectory);
            (PlaceManager = new EGRPlaceManager()).Initialize(m_Config["NET_PLACES_SRC_DIR"].String, WorkingDirectory);
            TileManager = new EGRTileManager(WorkingDirectory);//.Initialize(WorkingDirectory);
            WTE = new EGRWTE(m_Config["NET_WTE_DB_PATH"].String);

            DownloadManager = new MRKDownloadManager();

            int threadInterval = m_Config["NET_THREAD_INTERVAL"].Int;
            LogInfo($"Network thread interval={threadInterval}ms");

            m_NetThread = new Thread(() => MainNetworkThread(threadInterval));
            m_NetThread.Start();

            m_CDNNetwork = new EGRContentDeliveryNetwork();
            m_CDNNetwork.Start();

            int executionThreadInterval = m_Config["EXECUTION_THREAD_INTERVAL"].Int;
            LogInfo($"Execution thread interval={executionThreadInterval}");

            while (IsRunning) {
                if (KeyAvailable) {
                    EGRCommandManager.Execute(ReadLine());
                }

                Thread.Sleep(executionThreadInterval);
            }

            Exit();
        }

        void MainNetworkThread(int interval) {
            LogInfo($"Main network thread has started");

            while (IsRunning) {
                m_Network.UpdateNetwork();
                Thread.Sleep(interval);
            }

            LogInfo("Main network thread is exiting...");
        }

        void Exit() {
            WriteLine("\nPress any key to exit...");
            ReadLine();
            Environment.Exit(0);
        }
    }
}
