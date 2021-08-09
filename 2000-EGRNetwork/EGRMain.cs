using MRK.Networking;
using System.Threading;
using static MRK.EGRLogger;
using static System.Console;

namespace MRK {
    public class EGRMain {
        EGRNetworkConfig m_Config;
        EGRNetwork m_Network;

        public static EGRMain Instance { get; private set; }
        public bool IsRunning { get; set; }
        public EGRNetworkConfig Config => m_Config;

        public static void Main(string[] _) {
            if (Instance == null)
                Instance = new EGRMain();

            Instance._main();
        }

        void _main() {
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

            m_Network = new EGRNetwork("MAIN", int.Parse(m_Config["NET_PORT"]), m_Config["NET_KEY"], m_Config["NET_WORKING_DIR"], 
                m_Config["NET_PLACES_SRC_DIR"], m_Config["NET_WTE_DB_PATH"]);
            if (!m_Network.Start()) {
                LogInfo("Failed to start!");
                Exit();
                return;
            }

            LogInfo($"Network successfully initialized");

            int threadInterval = int.Parse(m_Config["NET_THREAD_INTERVAL"]);
            LogInfo($"Network thread interval={threadInterval}ms");

            while (IsRunning) {
                if (KeyAvailable) {
                    EGRCommandManager.Execute(ReadLine());
                }

                m_Network.UpdateNetwork();
                Thread.Sleep(threadInterval);
            }

            Exit();
        }

        void Exit() {
            WriteLine("\nPress any key to exit...");
            ReadLine();
        }
    }
}
