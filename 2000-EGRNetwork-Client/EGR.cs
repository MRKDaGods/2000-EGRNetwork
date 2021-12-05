using MRK.Networking.CloudActions.Transport;
using MRK.Networking.CloudAPI;
using MRK.Networking.CloudActions;
using MRK.Networking.Internal;
using MRK.Networking.Internal.Utils;
using MRK.Security;
using MRK.System;
using System.Net;
using System.Text;
using ThreadPool = MRK.Threading.ThreadPool;
using MRK.Networking.CloudAPI.V1.Authentication;

namespace MRK
{
    public static class EGR
    {
        private static readonly string DummyHWID = "NB(FBF(#bf9d3nincmni3ncinc3n21";
        private static readonly string CloudKey = @"?Ip)C;N8x|A~Fh<K$;R0iiq`w+8V45x\Q&CT:<%IsDq0gjeFiO>BNTLeK24b&f";

        private static readonly Reference<bool> _running;
        private static NetManager _netManager;
        private static ThreadPool _threadPool;
        private static CloudAuthentication _cloudAuthentication;
        private static TrackedTransport _transport;

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
            Initialization.Initialize();

            Logger.LogInfo("2000-EGRNetwork Client");

            StartTime = DateTime.Now;

            _threadPool = new ThreadPool(10, 20);

            //setup netmanager
            EventBasedNetListener listener = new();
            //uncomment for standard non-protocol testing
            //listener.NetworkReceiveUnconnectedEvent += OnNetworkReceiveUnconnected;
            _netManager = new(listener);
            _netManager.UnconnectedMessagesEnabled = true;
            _netManager.Start();

            Thread netThread = new(NetThread);
            netThread.Start();

            _cloudAuthentication = new CloudAuthentication(CloudKey, DummyHWID);
            _transport = new TrackedTransport(new IPEndPoint(IPAddress.Parse("127.0.0.1"), 23470), _netManager, listener, _cloudAuthentication);

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

                        case "sp":
                            _threadPool.Run(SendNewProtocolTest);
                            break;

                        case "sl":
                            _threadPool.Run(SendLoginTest);
                            break;

                        case "sr":
                            Console.Write("Email: ");
                            string email = Console.ReadLine();

                            Console.Write("Pwd: ");
                            string pwd = Console.ReadLine();

                            Console.Write("FirstName: ");
                            string fName = Console.ReadLine();

                            Console.Write("LastName: ");
                            string lName = Console.ReadLine();

                            _threadPool.Run(() => SendRegisterTest(email, pwd, fName, lName));
                            break;

                        case "sendm":
                            int num = int.Parse(Console.ReadLine()??"0");
                            Parallel.For(0, num, (i) => {
                                _threadPool.Run(SendUnconnectedTest);
                            });
                            break;

                        case "exit":
                            lock (_running)
                            {
                                _running.Value = false;
                            }
                            break;
                    }
                }

                Thread.Sleep(50);
            }

            _netManager.Stop();
            _transport.Stop();
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

        private static void SendLoginTest()
        {
            Logger.LogInfo("[Login] Starting login test");
            CloudActionContext context = new CloudActionContext(_transport, 1);
            Login cloudAction = new Login("", "", "", context);

            Logger.LogInfo("[Login] Sending...");
            cloudAction.Send();
            Logger.LogInfo($"[Login] Sent tk={context.RequestHeader.CloudActionToken}");

            while (cloudAction.State != CloudActionState.Received)
            {
                Thread.Sleep(50);
            }

            Logger.LogInfo($"[Login] Received, result={cloudAction.Response}, failInfo={cloudAction.Context.FailInfo}");
        }

        private static void SendRegisterTest(string email, string pwd, string firstName, string lastName)
        {
            Logger.LogInfo("[Register] Starting Register test");
            CloudActionContext context = new CloudActionContext(_transport, 1);
            Register cloudAction = new Register(email, EGRUtils.CalculateRawHash(Encoding.UTF8.GetBytes(pwd)), firstName, lastName, context);

            Logger.LogInfo("[Register] Sending...");
            cloudAction.Send();
            Logger.LogInfo($"[Register] Sent tk={context.RequestHeader.CloudActionToken}");

            while (cloudAction.State != CloudActionState.Received)
            {
                Thread.Sleep(50);
            }

            Logger.LogInfo($"[Register] Received, result={cloudAction.Response}, failInfo={cloudAction.Context.FailInfo}");
        }

        private static void SendNewProtocolTest()
        {
            Logger.LogInfo("Starting new protocol test");
            CloudActionContext context = new CloudActionContext(_transport, 1);
            Liveness cloudAction = new Liveness(context, "hi this is ex");

            Logger.LogInfo("Sending...");
            cloudAction.Send();
            Logger.LogInfo($"Sent tk={context.RequestHeader.CloudActionToken}");

            while (cloudAction.State != CloudActionState.Received)
            {
                Thread.Sleep(50);
            }

            Logger.LogInfo("Received");
        }

        private static void SendUnconnectedTest()
        {
            Logger.PreserveStream();

            Logger.LogInfo($"[{Thread.CurrentThread.ManagedThreadId}] Sending request with params:");
            Logger.LogInfoIndented($"HWID\t{DummyHWID}", 1);
            Logger.LogInfoIndented($"CloudKey\t{CloudKey}", 1);

            NetDataWriter writer = new();
            writer.Put(CloudKey); //key

            //xor it with key[0] ^ key[^1]
            string rawHwid = $"egr{DummyHWID}";
            string hwid = Xor.Single(rawHwid, (char)(CloudKey[0] ^ CloudKey[^1]));
            writer.Put(hwid);

            string token = EGRUtils.GetRandomString(32);
            token = token.Insert(0, "egr");
            
            string localIP = NetUtils.GetLocalIp(LocalAddrType.IPv4);
            Logger.LogInfo($"Local IP\t{localIP}");

            byte[] addr = Encoding.UTF8.GetBytes(rawHwid); //IPAddress.Parse(localIP).GetAddressBytes();
            //xor addr with hwid.length
            Xor.SingleNonAlloc(addr, (char)rawHwid.Length);

            //xor with hwid[0] ^ hwid[^1]
            Xor.SingleNonAlloc(addr, (char)(rawHwid[0] ^ rawHwid[^1]));
            string outToken = Xor.Multiple(token, addr);
            Logger.LogInfo($"Raw token\t{token}");
            Logger.LogInfo($"Out token\t{outToken}");
            writer.Put(outToken);

            writer.Put("Hey there from client");

            bool result = _netManager.SendUnconnectedMessage(writer, new IPEndPoint(IPAddress.Parse("127.0.0.1"), 23470));
            Logger.LogInfo($"Unconnected result={result}");

            Logger.FlushPreservedStream();
        }

        private static void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            _threadPool.Run(() => {
                Logger.PreserveStream();

                //read header
                try
                {
                    int apiVersion = reader.GetInt();
                    string token = reader.GetString();
                    byte response = reader.GetByte();

                    Logger.LogInfo($"[NET] {remoteEndPoint} {messageType}");
                    Logger.LogInfo($"[NET] Api version\t{apiVersion}");
                    Logger.LogInfo($"[NET] Token\t{token}");
                    Logger.LogInfo($"[NET] Response\t{response}");
                }
                catch
                {
                    Logger.LogError("err");
                }

                Logger.FlushPreservedStream();
            });
        }
    }
}