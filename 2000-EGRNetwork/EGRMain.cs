using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using MRK.Networking.Packets;

using static System.Console;

namespace MRK.Networking {
    public class EGRMain {
        const int EGR_MAIN_NETWORK_PORT = 23466;
        const string EGR_MAIN_NETWORK_KEY = "ntxsdI8cp4JEVcosVwz1";

        static EGRMain ms_Instance;

        EGRNetwork m_Network;
        
        public static void Main(string[] args) {
            if (ms_Instance == null)
                ms_Instance = new EGRMain();

            ms_Instance._main();
        }

        void _main() {
            WriteLine("EGR Main Network v1");
            WriteLine("Starting...");
            m_Network = new EGRNetwork(EGR_MAIN_NETWORK_PORT, EGR_MAIN_NETWORK_KEY);
            if (!m_Network.Start()) {
                WriteLine("Failed to start..");
                Exit();
                return;
            }

            WriteLine($"Started EGR Main Network, port={EGR_MAIN_NETWORK_PORT}, key={EGR_MAIN_NETWORK_KEY}");

            while (true) {
                if (KeyAvailable) {
                    EGRCommandManager.Execute(ReadLine());
                }

                m_Network.UpdateNetwork();
                Thread.Sleep(100);
            }

            Exit();
        }

        void Exit() {
            WriteLine("\nPress any key to exit...");
            ReadLine();
        }
    }
}
