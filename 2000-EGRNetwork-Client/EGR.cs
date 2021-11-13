using MRK.Networking.Internal;
using MRK.Networking.Internal.Utils;
using MRK.Security;
using System.Net;
using System.Threading;

namespace MRK
{
    public static class EGR
    {
        private static string DummyHWID = "NB(FBF(#bf9d3nincmni3ncinc3n21";
        private static string CloudKey = "?Ip)C;N8x|A~Fh<K$;R0iiq`w+8V45x\Q&CT:<%IsDq0gjeFiO>BNTLeK24b&f";

        private static readonly Reference<bool> _running;
        private static NetManager _netManager;

        public static DateTime StartTime
        {
            get; private set;
        }

        public static TimeSpan RelativeTime
        {
            get { return DateTime.Now - StartTime; }
        }

        static EGR()
        {
            _running = new Reference<bool>(true);
        }

        private static void Main()
        {
            Logger.LogInfo("2000-EGRNetwork Client");

            StartTime = DateTime.Now;

            //send unconnected to local?

            //setup netmanager
            EventBasedNetListener listener = new();
            listener.NetworkReceiveUnconnectedEvent += OnNetworkReceiveUnconnected;
            _netManager = new(listener);
            _netManager.UnconnectedMessagesEnabled = true;
            _netManager.Start();

            Thread netThread = new(NetThread);
            netThread.Start();

            while (_running.Value)
            {
                if (Console.KeyAvailable)
                {
                    string cmd = Console.ReadLine();
                    switch (cmd)
                    {
                        case "send":
                            SendUnconnectedTest();
                            break;

                        case "exit":
                            lock (_running)
                            {
                                _running.Value = false;
                            }
                            break;
                    }
                }
            }

            _netManager.Stop();
            Logger.LogInfo("Exiting...");

            Console.ReadLine();
        }

        private static void NetThread()
        {
            while (_running.Value)
            {
                _netManager.PollEvents();
                Thread.Sleep(50);
            }
        }

        private static void SendUnconnectedTest()
        {
            NetDataWriter writer = new();
            writer.Put(CloudKey); //key

            //xor it with key[0] ^ key[^1]
            string hwid = Xor.Single(DummyHWID, (char)(CloudKey[0] ^ CloudKey[^1]));

            writer.Put("Hey there from client");

            bool result = _netManager.SendUnconnectedMessage(writer, new IPEndPoint(IPAddress.Parse("127.0.0.1"), 23470));
            Logger.LogInfo($"Unconnected result={result}");
        }

        private static void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            Logger.LogInfo($"[NET] {remoteEndPoint} {messageType} {reader.GetString()}");
        }
    }
}