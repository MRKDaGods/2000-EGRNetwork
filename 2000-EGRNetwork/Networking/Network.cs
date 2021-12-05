using MRK.Networking.Internal;
using MRK.Networking.Internal.Utils;
using MRK.Networking.Packets;
using MRK.Security;
using MRK.Threading;
using System.Collections.Generic;

namespace MRK.Networking
{
    public enum EGRStandardResponse : byte
    {
        NONE = 0x0,
        TIMED_OUT,
        FAILED,
        SUCCESS
    }

    public class Network
    {
        private const int WorkerCount = 4;

        delegate void OnPacketReceivedDelegate(NetPeer peer, PacketType packet, PacketDataStream stream, int req);
        public delegate void DataWriteDelegate(PacketDataStream stream);

        protected readonly NetManager _netManager;
        protected readonly EventBasedNetListener _eventListener;
        private readonly int _port;
        private readonly string _key;
        private readonly string _name;
        private readonly Dictionary<string, NetPeer> _connectedPeers;
        private readonly Reference<bool> _running;
        private System.Threading.Thread _internalNetThread;
        private readonly bool _internalThread;
        private readonly int _internalThreadInterval;

        protected readonly ThreadPool _threadPool;

        public string Name
        {
            get { return _name; }
        }

        public int Port
        {
            get { return _port; }
        }

        public string Key
        {
            get { return _key; }
        }

        public Network(string name, int port, string key, bool internalThread = false, int internalThreadInterval = 100)
        {
            _name = name;
            _port = port;
            _key = key;
            _internalThread = internalThread;
            _internalThreadInterval = internalThreadInterval;

            _eventListener = new EventBasedNetListener();
            _eventListener.PeerConnectedEvent += OnPeerConnected;
            _eventListener.PeerDisconnectedEvent += OnPeerDisconnected;
            _eventListener.ConnectionRequestEvent += OnConnectionRequest;
            _eventListener.NetworkReceiveEvent += OnReceive;

            _netManager = new NetManager(_eventListener);

#if SIMULATE_NET_CONDITIONS
            _netManager.SimulationPacketLossChance = 50;
            _netManager.SimulatePacketLoss = true;
            _netManager.SimulateLatency = true;
            _netManager.SimulationMinLatency = 100;
            _netManager.SimulationMaxLatency = 3000;
#endif

            _threadPool = new ThreadPool(15, 10);

            _connectedPeers = new Dictionary<string, NetPeer>();
            _running = new Reference<bool>();
        }

        #region Network event handlers

        private void OnConnectionRequest(ConnectionRequest request)
        {
            OnNetworkConnectRequest(request);
        }

        private void OnPeerConnected(NetPeer peer)
        {
            OnNetworkPeerConnect(peer);
        }

        private void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo)
        {
            OnNetworkPeerDisconnect(peer, disconnectInfo);
        }

        private void OnReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod method)
        {
            OnNetworkReceive(peer, reader, channel, method);
            /*
             try
                {
                    PacketNature nature = PacketNature.Out;
                    PacketType type = (PacketType)reader.GetUShort();

                    int bufferedReq = reader.GetInt();

                    PacketDataStream dataStream = new PacketDataStream(reader.GetRemainingBytes(), nature);
                    dataStream.Prepare();

                    _onPacketReceived?.Invoke(peer, type, dataStream, bufferedReq);

                    dataStream.Clean();
                }
                catch
                {
                }
             */
        }

        #endregion

        #region Network overridable event handlers

        protected virtual void OnNetworkConnectRequest(ConnectionRequest request)
        {
            QueueConnectionApproval(request);
        }

        protected virtual void OnNetworkApproval(NetworkUser networkUser, NetDataReader data)
        {
            if (networkUser.IsServerUser)
            {
                ((ServerNetworkUser)networkUser).AddConnectedNetwork(this);
            }
        }

        protected virtual void OnNetworkPeerConnect(NetPeer peer)
        {
        }

        protected virtual void OnNetworkPeerDisconnect(NetPeer peer, DisconnectInfo disconnectInfo)
        {
        }

        protected virtual void OnNetworkReceive(NetPeer peer, NetPacketReader reader, byte channel, DeliveryMethod method)
        {
        }

        #endregion

        private void QueueConnectionApproval(ConnectionRequest request)
        {
            _threadPool.Run(() => ApproveConnection(request));
        }

        private bool ApproveConnection(ConnectionRequest request)
        {
            try
            {
                string key = request.Data.GetString();
                if (key == _key)
                {
                    //xor hwid with key[0] ^ key[^1]
                    string hwid = request.Data.GetString();
                    hwid = Xor.Single(hwid, (char)(key[0] ^ key[^1]));
                    if (hwid.StartsWith("egr"))
                    {
                        NetPeer peer = request.Accept();
                        NetworkUser networkUser = EGR.Server.GetOrAddUser(hwid);
                        _connectedPeers[hwid] = peer;

                        OnNetworkApproval(networkUser, request.Data);
                        return true;
                    }
                }
            }
            catch
            {
            }

            request.RejectForce();
            return false;
        }

        public bool Start()
        {
            if (_netManager.IsRunning && _running.Value)
                return false;

            _running.Value = true;

            if (_internalThread)
            {
                _internalNetThread = new(NetThread);
                _internalNetThread.Start();
            }

            return _netManager.Start(_port);
        }

        private void NetThread()
        {
            while (_running.Value)
            {
                _netManager.PollEvents();
                System.Threading.Thread.Sleep(_internalThreadInterval);
            }
        }

        public void UpdateNetwork()
        {
            if (_internalThread)
            {
                return;
            }

            _netManager.PollEvents();
        }

        public void SendPacket(NetPeer peer, int buf, PacketType packet, DeliveryMethod deliveryMethod, DataWriteDelegate writeDelegate)
        {
            PacketDataStream dataStream = new PacketDataStream(null, PacketNature.In);
            dataStream.Prepare();

            dataStream.WriteByte((byte)PacketNature.In);
            dataStream.WriteUInt16((ushort)packet);
            dataStream.WriteInt32(buf);

            writeDelegate(dataStream);

            peer.Send(dataStream.Data, deliveryMethod);

            dataStream.Clean();
        }

        public void SendStandardResponsePacket(NetPeer peer, int buf, EGRStandardResponse response)
        {
            SendPacket(peer, buf, PacketType.STDRSP, DeliveryMethod.ReliableOrdered, (stream) => {
                stream.WriteByte((byte)response);
            });
        }

        public void Stop()
        {
            if (!_running.Value)
            {
                return;
            }

            lock (_running)
            {
                _running.Value = false;
            }

            if (_internalNetThread != null)
            {
                _internalNetThread.Join(_internalThreadInterval * 2);
                _internalNetThread = null;
            }

            _netManager.Stop();
        }

        public void CloseConnection(NetworkUser networkUser, byte[] data = null)
        {
            NetPeer peer;
            if (_connectedPeers.TryGetValue(networkUser.HWID, out peer))
            {
                _connectedPeers.Remove(networkUser.HWID);

                if (networkUser.IsServerUser)
                {
                    EGR.Server.RemoveUser(this, (ServerNetworkUser)networkUser);
                }

                peer.Disconnect(data);
            }
        }
    }
}