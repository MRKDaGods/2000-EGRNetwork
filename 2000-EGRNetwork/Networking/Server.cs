using System.Collections.Generic;

namespace MRK.Networking
{
    public class Server
    {
        private readonly HashSet<Network> _runningNetworks;
        private readonly Dictionary<string, ServerNetworkUser> _connectedUsers;
        private CloudNetwork _cloudNetwork;

        public Server()
        {
            _runningNetworks = new HashSet<Network>();
            _connectedUsers = new Dictionary<string, ServerNetworkUser>();
        }

        public void Initialize()
        {
            EGRNetworkConfig config = EGR.Config;
            _cloudNetwork = new CloudNetwork("CLOUD", config["NET_CLOUD_PORT"].Int, config["NET_CLOUD_KEY"].String);
            _cloudNetwork.Start();
        }

        public void Stop()
        {
            _cloudNetwork.Stop();
        }

        public NetworkUser GetOrAddUser(string hwid)
        {
            ServerNetworkUser networkUser;
            if (!_connectedUsers.TryGetValue(hwid, out networkUser))
            {
                networkUser = new ServerNetworkUser(hwid);
                _connectedUsers[hwid] = networkUser;
            }

            return networkUser;
        }

        public void RemoveUser(Network network, ServerNetworkUser networkUser)
        {
            if (_connectedUsers.ContainsKey(networkUser.HWID))
            {
                networkUser.ConnectedNetworks.Remove(network);
                if (networkUser.ConnectedNetworks.Count == 0)
                {
                    //remove user from Server COMPLETELY
                    _connectedUsers.Remove(networkUser.HWID);
                }
            }
        }
    }
}
