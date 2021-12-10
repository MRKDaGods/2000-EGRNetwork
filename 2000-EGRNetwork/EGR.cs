using MRK.Networking;
using MRK.WTE;
using MRK.System;
using System;
using System.Threading;
using static MRK.Logger;
using static System.Console;
using ThreadPool = MRK.Threading.ThreadPool;
using MRK.Threading;

namespace MRK
{
    public class EGR
    {
        private static EGRNetworkConfig _config;
        private static Network _network;
        private static EGRContentDeliveryNetwork _cdnNetwork;
        private static ThreadPool _globalThreadPool;
        private static CommandLine _commandLine;

        public static bool IsRunning
        {
            get; set;
        }

        public static EGRNetworkConfig Config
        {
            get { return _config; }
        }

        public static Server Server
        {
            get; private set;
        }

        public static string WorkingDirectory
        {
            get; private set;
        }

        public static EGRAccountManager AccountManager
        {
            get; private set;
        }

        public static EGRPlaceManager PlaceManager
        {
            get; private set;
        }

        public static EGRTileManager TileManager
        {
            get; private set;
        }

        public static EGRWTE WTE
        {
            get; private set;
        }

        public static ThreadPool GlobalThreadPool
        {
            get { return _globalThreadPool; }
        }

        public static void Main(string[] args)
        {
            ForegroundColor = ConsoleColor.Green;

            Initialization.Initialize();

            IsRunning = true;

            LogInfo("2000-EGR Network - MODERN - MRKDaGods(Mohamed Ammar)");
            LogInfo("Starting...");

            //initialize global thread pool
            _globalThreadPool = new ThreadPool(15, 10);
            Lifetime.Initialize(_globalThreadPool); //move

            LogInfo("Parsing command line...");
            _commandLine = new CommandLine(args);
            LogInfo($"[DEBUG] {string.Join(" / ", args)}");

            if (!_commandLine.ParseArguments())
            {
                LogInfo("Invalid command line arguments");
                return;
            }

            _commandLine.PrintCommandLineOptions();

            _config = new EGRNetworkConfig();
            if (!_config.Load())
            {
                _config.WriteDefaultConfig(); //create config file
                LogError("Network config loading failed...");
                Exit();
                return;
            }

            MRKCryptography.CookSalt();

            Server = new Server();
            Server.Initialize();

            _network = new Network("MAIN", _config["NET_PORT"].Int, _config["NET_KEY"].String);
            if (!_network.Start())
            {
                LogInfo("Failed to start!");
                Exit();
                return;
            }

            LogInfo($"Network successfully initialized");

            WorkingDirectory = _config["NET_WORKING_DIR"].String;
            /*(AccountManager = new EGRAccountManager()).Initialize(WorkingDirectory);
            (PlaceManager = new EGRPlaceManager()).Initialize(_config["NET_PLACES_SRC_DIR"].String, WorkingDirectory);
            TileManager = new EGRTileManager(WorkingDirectory);//.Initialize(WorkingDirectory);
            WTE = new EGRWTE(_config["NET_WTE_DB_PATH"].String);

            int threadInterval = _config["NET_THREAD_INTERVAL"].Int;
            LogInfo($"Network thread interval={threadInterval}ms");

            _netThread = new Thread(() => MainNetworkThread(threadInterval));
            _netThread.Start();

            _cdnNetwork = new EGRContentDeliveryNetwork();
            _cdnNetwork.Start(); */

            int executionThreadInterval = _config["EXECUTION_THREAD_INTERVAL"].Int;
            LogInfo($"Execution thread interval={executionThreadInterval}");

            while (IsRunning)
            {
                if (KeyAvailable)
                {
                    EGRCommandManager.Execute(ReadLine());
                }

                Thread.Sleep(executionThreadInterval);
            }

            Server.Stop();

            Exit();
        }

        static void Exit()
        {
            WriteLine("\nPress any key to exit...");
            ReadLine();
            Environment.Exit(0);
        }
    }
}
