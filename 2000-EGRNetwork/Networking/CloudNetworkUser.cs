using System.Net;

namespace MRK.Networking
{
    public class CloudNetworkUser : NetworkUser
    {
        private readonly IPEndPoint _remoteEndPoint;

        public override bool IsServerUser
        {
            get { return false; }
        }

        public IPEndPoint RemoteEndPoint
        {
            get { return _remoteEndPoint; }
        }

        public CloudNetworkUser(string hwid, IPEndPoint remoteEndPoint) : base(hwid)
        {
            _remoteEndPoint = remoteEndPoint;
        }
    }
}
