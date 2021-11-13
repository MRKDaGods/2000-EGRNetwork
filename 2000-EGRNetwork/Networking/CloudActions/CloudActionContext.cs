using MRK.Networking.Internal.Utils;

namespace MRK.Networking.CloudActions
{
    public class CloudActionContext
    {
        private readonly CloudNetwork _cloudNetwork;
        private readonly CloudNetworkUser _networkUser;
        private readonly NetDataWriter _netDataWriter;
        private readonly CloudActionHeader _outHeader;
        private readonly string _actionToken;

        public CloudNetwork CloudNetwork
        {
            get { return _cloudNetwork; }
        }

        public CloudNetworkUser NetworkUser
        {
            get { return _networkUser; }
        }

        public NetDataReader Data
        {
            get; private set;
        }

        public CloudResponse Response
        {
            get { return _outHeader.Response; }
            set {  _outHeader.Response = value; }
        }

        public string ActionToken
        {
            get { return _actionToken; }
        }

        public NetDataWriter DataWriter
        {
            get { return _netDataWriter; }
        }

        public CloudActionContext(CloudNetwork cloudNetwork, CloudNetworkUser networkUser, NetDataReader data, string actionToken)
        {
            _cloudNetwork = cloudNetwork;
            _networkUser = networkUser;
            Data = data;
            _actionToken = actionToken;

            _netDataWriter = new NetDataWriter();
            _outHeader = new CloudActionHeader(CloudNetwork.CloudAPIVersion, actionToken);
        }

        private void WriteHeaderToStream()
        {
            _outHeader.Serialize(_netDataWriter);
        }

        public void Reply(string body)
        {
            WriteHeaderToStream();
            _netDataWriter.Put(body);
        }

        private void Send()
        {
            _cloudNetwork.CloudSend(this);
        }
    }
}