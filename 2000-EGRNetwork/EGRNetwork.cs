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

    public class EGRDownloadRequest {
        public ulong ID;
        public NetPeer Peer;
        public byte[][] Data;
        public int Progress;
        public DateTime RequestTime;
        public bool Accepted;
    }

    public class EGRNetwork {
        const int WORKER_COUNT = 4;

        delegate void OnPacketReceivedDelegate(NetPeer peer, PacketType packet, PacketDataStream stream, int req);
        public delegate void DataWriteDelegate(PacketDataStream stream);

        readonly NetManager m_Network;
        readonly EventBasedNetListener m_Listener;
        readonly int m_Port;
        readonly string m_Key;
        OnPacketReceivedDelegate m_OnPacketReceived;
        readonly Dictionary<NetPeer, EGRSessionUser> m_Users;
        readonly Dictionary<PacketType, List<MethodInfo>> m_PacketHandlers;
        readonly Dictionary<NetPeer, List<EGRDownloadRequest>> m_ActiveDownloads;
        readonly MRKWorker[] m_Workers;
        int m_LastWorkerIdx;

        public static EGRNetwork Instance { get; private set; }
        public EGRAccountManager AccountManager { get; private set; }
        public EGRPlaceManager PlaceManager { get; private set; }
        public EGRTileManager TileManager { get; private set; }

        public EGRNetwork(int port, string key, params string[] paths) {
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

            (AccountManager = new EGRAccountManager()).Initialize(paths[0]);
            (PlaceManager = new EGRPlaceManager()).Initialize(paths[1], paths[0]);
            (TileManager = new EGRTileManager()).Initialize(paths[0]);

            m_ActiveDownloads = new Dictionary<NetPeer, List<EGRDownloadRequest>>();

            m_Workers = new MRKWorker[WORKER_COUNT];
            for (int i = 0; i < WORKER_COUNT; i++) {
                m_Workers[i] = new MRKWorker();
                m_Workers[i].StartWorker();
            }
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
            int wIdx = m_LastWorkerIdx;
            m_Workers[m_LastWorkerIdx++].Queue(() => {
                try {
                    PacketNature nature = PacketNature.Out;//(PacketNature)reader.GetByte();
                    PacketType type = (PacketType)reader.GetUShort();

                    int bufferedReq = reader.GetInt();

                    PacketDataStream dataStream = new PacketDataStream(reader.GetRemainingBytes(), nature);
                    dataStream.Prepare();

                    m_OnPacketReceived?.Invoke(peer, type, dataStream, bufferedReq);

                    WriteLine($"[{peer.Id}] packet, n={nature}, t={type}, SZ={dataStream.Data.Length} bytes, w={wIdx}");

                    dataStream.Clean();
                }
                catch {
                }
            });

            if (m_LastWorkerIdx == WORKER_COUNT)
                m_LastWorkerIdx = 0;
        }

        public bool Start() {
            if (m_Network.IsRunning)
                return false;

            return m_Network.Start(m_Port);
        }

        public void UpdateNetwork() {
            m_Network.PollEvents();

            lock (m_ActiveDownloads) {
                foreach (var pair in m_ActiveDownloads) {
                    List<EGRDownloadRequest> removing = new List<EGRDownloadRequest>();
                    foreach (EGRDownloadRequest request in pair.Value) {
                        if (!request.Accepted) {
                            if ((DateTime.Now - request.RequestTime).TotalSeconds >= 5d) {
                                removing.Add(request);
                                continue;
                            }
                        }

                        bool incomplete = request.Progress < request.Data.Length;
                        SendPacket(pair.Key, -1, PacketType.DWNLD, DeliveryMethod.ReliableUnordered, x => {
                            x.WriteUInt64(request.ID);
                            x.WriteInt32(request.Progress);
                            x.WriteBool(incomplete);

                            if (incomplete) {
                                byte[] section = request.Data[request.Progress];
                                x.WriteInt32(section.Length);
                                foreach (byte b in section)
                                    x.WriteByte(b);
                            }
                        });

                        if (!incomplete) {
                            //done!
                            removing.Add(request);
                        }
                    }

                    foreach (EGRDownloadRequest req in removing)
                        m_ActiveDownloads[pair.Key].Remove(req);
                }
            }
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

            //manual packet handling
            switch (packet) {

                case PacketType.DWNLDREQ:
                    ProcessInboundDownloadRequest(peer, stream.ReadUInt64(), stream.ReadBool());
                    break;

                case PacketType.DWNLD:
                    ProcessInboundDownloadConfirmation(peer, stream.ReadUInt64());
                    break;

            }

            foreach (MethodInfo handler in m_PacketHandlers[packet]) {
                handler.Invoke(null, new object[] { this, GetSessionUser(peer), stream, buf });
            }
        }

        public EGRDownloadRequest CreateDownloadRequest(NetPeer peer, byte[] data) {
            EGRDownloadRequest request = new EGRDownloadRequest {
                Peer = peer,
                ID = EGRUtils.GetRandomID()
            };

            //60000bytes is our limit
            int sections = (int)Math.Ceiling(data.Length / 20000m);
            byte[][] byteSectors = new byte[sections][];
            for (int i = 0; i < sections; i++) {
                byteSectors[i] = new byte[i == sections - 1 ? data.Length - i * 20000 : 20000];
                for (int j = 0; j < byteSectors[i].Length; j++) {
                    byteSectors[i][j] = data[i * 20000 + j];
                }
            }

            request.Data = byteSectors;
            return request;
        }

        public void StartDownload(EGRDownloadRequest request) {
            //send req info to peer
            if (request.Peer == null)
                return;

            SendPacket(request.Peer, -1, PacketType.DWNLDREQ, DeliveryMethod.ReliableOrdered, x => {
                x.WriteUInt64(request.ID);
                x.WriteInt32(request.Data.Length);
            });

            request.RequestTime = DateTime.Now;

            lock (m_ActiveDownloads) {
                if (!m_ActiveDownloads.ContainsKey(request.Peer)) {
                    m_ActiveDownloads[request.Peer] = new List<EGRDownloadRequest>();
                }

                m_ActiveDownloads[request.Peer].Add(request);
            }
        }

        void ProcessInboundDownloadRequest(NetPeer peer, ulong id, bool result) {
            LogInfo($"download {id} req with res={result}");

            lock (m_ActiveDownloads) {
                if (!result) {
                    if (m_ActiveDownloads.ContainsKey(peer))
                        m_ActiveDownloads[peer].RemoveAll(x => x.ID == id);
                    return;
                }

                m_ActiveDownloads[peer].Find(x => x.ID == id).Accepted = true;
            }
        }

        void ProcessInboundDownloadConfirmation(NetPeer peer, ulong id) {
            EGRDownloadRequest request = m_ActiveDownloads[peer].Find(x => x.ID == id);
            if (request != null) {
                LogInfo($"download {request.Progress} confirm");
                request.Progress++;
            }
        }
    }
}