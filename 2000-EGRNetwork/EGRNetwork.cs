using MRK.Networking.Packets;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

using static MRK.EGRLogger;
using static System.Console;

namespace MRK.Networking {
    public enum EGRStandardResponse : byte {
        NONE = 0x0,
        TIMED_OUT,
        FAILED,
        SUCCESS
    }

    public class EGRNetwork {
        delegate void OnPacketReceivedDelegate(NetPeer peer, PacketType packet, PacketDataStream stream, int req);
        public delegate void DataWriteDelegate(PacketDataStream stream);

        readonly NetManager m_Network;
        readonly EventBasedNetListener m_Listener;
        readonly int m_Port;
        readonly string m_Key;
        OnPacketReceivedDelegate m_OnPacketReceived;
        readonly Dictionary<NetPeer, EGRSessionUser> m_Users;
        readonly Dictionary<PacketType, List<MethodInfo>> m_PacketHandlers;

        public static EGRNetwork Instance { get; private set; }
        public EGRAccountManager AccountManager { get; private set; }
        public EGRPlaceManager PlaceManager { get; private set; }

        public EGRNetwork(int port, string key) {
            Instance = this;

            m_Key = key;
            m_Users = new Dictionary<NetPeer, EGRSessionUser>();
            m_OnPacketReceived = INTERNAL_OnPacketReceived;

            m_Listener = new EventBasedNetListener();
            m_Listener.PeerConnectedEvent += OnPeerConnected;
            m_Listener.PeerDisconnectedEvent += OnPeerDisconnected;
            m_Listener.ConnectionRequestEvent += OnConnectionRequest;
            m_Listener.NetworkReceiveEvent += OnReceive;
            m_Network = new NetManager(m_Listener);

            m_Port = port;

            m_PacketHandlers = new Dictionary<PacketType, List<MethodInfo>>();
            for (PacketType type = PacketType.None + 1; type < PacketType.MAX; type++) {
                List<MethodInfo> handlers = new List<MethodInfo>();
                foreach (Type t in Assembly.GetExecutingAssembly().GetTypes()) {
                    PacketHandlerAttribute attr = t.GetCustomAttribute<PacketHandlerAttribute>();
                    if (attr == null)
                        continue;

                    if (attr.PacketType == type)
                        handlers.Add(t.GetMethod("Handle", BindingFlags.NonPublic | BindingFlags.Static));
                }

                m_PacketHandlers[type] = handlers;
            }

            (AccountManager = new EGRAccountManager()).Initialize(@"E:\EGRNetworkAlpha");
            (PlaceManager = new EGRPlaceManager()).Initialize(@"E:\mrkwinrt\vsprojects\MRKGoogleSkimmer\MRKGoogleSkimmer\bin\Debug\Data", @"E:\EGRNetworkAlpha");

            var x = PlaceManager.GetPlaces(30.02d, 30d, 31d, 31d, 0);
        }

        void OnConnectionRequest(ConnectionRequest request) {
            request.AcceptIfKey(m_Key);
        }

        void OnPeerConnected(NetPeer peer) {
            LogInfo($"[{peer.Id}] Peer connected, ep={peer.EndPoint}");

            EGRSessionUser sUser = GetSessionUser(peer);
            if (sUser != null) {
                LogError($"[{peer.Id}] Peer is already connected");
                return;
            }

            sUser = new EGRSessionUser(peer);
            //assign xor key
            string xorKey = EGRUtils.GetRandomString(24);
            sUser.AssignXorKey(xorKey);

            m_Users[peer] = sUser;
        }

        void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) {
            EGRSessionUser sUser = GetSessionUser(peer);
            if (sUser == null) {
                LogError($"[{peer.Id}] Cannot find peer");
                return;
            }

            m_Users.Remove(sUser.Peer);

            LogInfo($"[{peer.Id}] disconnected");
        }

        EGRSessionUser GetSessionUser(NetPeer peer) {
            EGRSessionUser suser;
            if (m_Users.TryGetValue(peer, out suser))
                return suser;

            return null;
        }

        void OnReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod method) {
            new Thread(() => {
                try {
                    PacketNature nature = PacketNature.Out;//(PacketNature)reader.GetByte();
                    PacketType type = (PacketType)reader.GetUShort();

                    int bufferedReq = reader.GetInt();

                    PacketDataStream dataStream = new PacketDataStream(reader.GetRemainingBytes(), nature);
                    dataStream.Prepare();

                    m_OnPacketReceived?.Invoke(peer, type, dataStream, bufferedReq);

                    WriteLine($"[{peer.Id}] packet, n={nature}, t={type}, SZ={dataStream.Data.Length} bytes");

                    dataStream.Clean();
                }
                catch {
                }
            }).Start();
        }

        public bool Start() {
            if (m_Network.IsRunning)
                return false;

            return m_Network.Start(m_Port);
        }

        public void UpdateNetwork() {
            m_Network.PollEvents();
        }

        public void SendPacket(NetPeer peer, int buf, PacketType packet, DeliveryMethod deliveryMethod, DataWriteDelegate writeDelegate) {
            PacketDataStream dataStream = new PacketDataStream(null, PacketNature.In);
            dataStream.Prepare();

            dataStream.WriteByte((byte)PacketNature.In);
            dataStream.WriteUInt16((ushort)packet);
            dataStream.WriteInt32(buf);

            writeDelegate(dataStream);

            peer.Send(dataStream.Data, deliveryMethod);

            dataStream.Clean();
        }

        public void SendStandardResponsePacket(NetPeer peer, int buf, EGRStandardResponse response) {
            SendPacket(peer, buf, PacketType.STDRSP, DeliveryMethod.ReliableOrdered, (stream) => {
                stream.WriteByte((byte)response);
            });
        }

        void INTERNAL_OnPacketReceived(NetPeer peer, PacketType packet, PacketDataStream stream, int buf) {
            //process our packets
            foreach (MethodInfo handler in m_PacketHandlers[packet]) {
                handler.Invoke(null, new object[] { this, GetSessionUser(peer), stream, buf });
            }
        }
    }
}