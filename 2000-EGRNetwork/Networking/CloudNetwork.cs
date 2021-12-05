using MRK.Networking.CloudActions;
using MRK.Networking.Internal;
using MRK.Security;
using System.Net;

namespace MRK.Networking
{
    public class CloudNetwork : Network
    {
        public const int CloudAPIVersion = 1;

        public CloudNetwork(string name, int port, string key) : base(name, port, key, true, 50)
        {
            //enable unconnected messages
            _netManager.UnconnectedMessagesEnabled = true;

            _eventListener.NetworkReceiveUnconnectedEvent += OnNetworkReceiveUnconnected;
        }

        protected override void OnNetworkConnectRequest(ConnectionRequest request)
        {
            //we are not accepting any connections
            request.RejectForce();
        }

        private bool AuthenticateCloudConnection(IPEndPoint remoteEndPoint, NetPacketReader reader, out CloudNetworkUser networkUser)
        {
            networkUser = null;

            try
            {
                //1- key
                string key = reader.GetString();
                if (key == Key)
                {
                    //2- hwid
                    string hwid = reader.GetString();
                    hwid = Xor.Single(hwid, (char)(key[0] ^ key[^1]));
                    if (hwid.StartsWith("egr"))
                    {
                        networkUser = new CloudNetworkUser(hwid, remoteEndPoint);
                        return true;
                    }
                }
            }
            catch
            {
            }

            return false;
        }

        private void OnNetworkReceiveUnconnected(IPEndPoint remoteEndPoint, NetPacketReader reader, UnconnectedMessageType messageType)
        {
            _threadPool.Run(() => ProcessCloudAction(remoteEndPoint, reader));
        }

        private void ProcessCloudAction(IPEndPoint remoteEndPoint, NetPacketReader reader)
        {
            Logger.PreserveStream();
            Logger.LogInfo($"Processing cloud request from {remoteEndPoint}");

            CloudNetworkUser networkUser;
            if (!AuthenticateCloudConnection(remoteEndPoint, reader, out networkUser))
            {
                Logger.LogError($"Cannot authenticate connection, endpoint={remoteEndPoint}");
                goto __exit;
            }

            string rawCloudActionToken = null;
            try
            {
                string cloudActionToken = reader.GetString();

                if (!ValidateCloudActionToken(cloudActionToken, networkUser.HWID, remoteEndPoint, out rawCloudActionToken))
                {
                    Logger.LogError($"Could not validate action token, t={cloudActionToken}");
                    goto __exit;
                }

                Logger.LogInfo($"valid action token={rawCloudActionToken}");

                string cloudActionPath = reader.GetString();
                Logger.LogInfo($"cloud action path={cloudActionPath}");

                CloudAction cloudAction = CloudActionFactory.GetCloudAction(cloudActionPath);
                if (cloudAction != null)
                {
                    Logger.LogInfo("Executing cloud action");
                    CloudActionContext cloudActionContext = new(this, networkUser, reader, rawCloudActionToken);
                    if (!cloudActionContext.Valid)
                    {
                        Logger.LogError("Invalid request data");
                        goto __exit;
                    }

                    cloudAction.Execute(cloudActionContext);
                    goto __exit;
                }
            }
            catch
            {
            }

            //we cant reply to a client with no CloudActionToken
            if (rawCloudActionToken == null)
            {
                goto __exit;
            }

            //failed
            CloudAction responseAction = CloudActionFactory.GetCloudAction(CloudActionFactory.GetCloudActionPath(CloudAPIVersion, "response"));
            CloudActionContext responseActionContext = new(this, networkUser, reader, rawCloudActionToken)
            {
                Response = CloudResponse.Failure
            };
            responseAction.Execute(responseActionContext);

        __exit:
            Logger.FlushPreservedStream();
        }

        public void CloudSend(CloudActionContext context)
        {
            if (context == null || context.NetworkUser == null)
            {
                return;
            }

            try
            {
                _netManager.SendUnconnectedMessage(context.DataWriter, context.NetworkUser.RemoteEndPoint);
            }
            catch
            {
                Logger.LogError("Failed to CloudSend to " + context.NetworkUser.RemoteEndPoint);
            }
        }

        private bool ValidateCloudActionToken(string token, string hwid, IPEndPoint remoteEndPoint, out string rawToken)
        {
            rawToken = null;
            if (token == null || hwid == null || remoteEndPoint == null)
            {
                return false;
            }

            //THIS BREAKS VPNs
            byte[] addr = System.Text.Encoding.UTF8.GetBytes(hwid); //remoteEndPoint.Address.GetAddressBytes();
            //xor addr with hwid.length
            Xor.SingleNonAlloc(addr, (char)hwid.Length);

            //xor with hwid[0] ^ hwid[^1]
            Xor.SingleNonAlloc(addr, (char)(hwid[0] ^ hwid[^1]));

            rawToken = Xor.Multiple(token, addr);
            return rawToken.StartsWith("egr");
        }
    }
}
