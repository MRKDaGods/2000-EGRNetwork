using System.Collections.Generic;

namespace MRK.Networking
{
    public class ServerNetworkUser : NetworkUser
    {
        private readonly HashSet<Network> _connectedNetworks;

        public HashSet<Network> ConnectedNetworks
        {
            get { return _connectedNetworks; }
        }

        public ServerNetworkUser(string hwid) : base(hwid)
        {
            _connectedNetworks = new HashSet<Network>();
        }

        public void AddConnectedNetwork(Network network)
        {
            _connectedNetworks.Add(network);
        }

        public void RemoveConnectedNetwork(Network network)
        {
            _connectedNetworks.Remove(network);
        }
    }
}
