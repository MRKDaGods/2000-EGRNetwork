using System.Net;
using System;
using System.Collections.Generic;

using static System.Console;
using MRK.Networking.Packets;

namespace MRK.Networking {
    public class EGRNetworkOLD {
        delegate void OnPacketReceivedDelegate(NetPeer peer, PacketType packet, PacketDataStream stream, int req);
        delegate void DataWriteDelegate(PacketDataStream stream);

        NetManager m_Network;
        EventBasedNetListener m_Listener;
        int m_Port;
        string m_Key;
        OnPacketReceivedDelegate m_OnPacketReceived;
        Dictionary<string, EGRUserInfo> m_Users;

        string m_SkeyOfLast;

        public EGRNetworkOLD(int port, string key) {
            m_Key = key;
            m_Users = new Dictionary<string, EGRUserInfo>();
            m_OnPacketReceived = INTERNAL_OnPacketReceived;

            m_Listener = new EventBasedNetListener();
            m_Listener.PeerConnectedEvent += OnPeerConnected;
            m_Listener.PeerDisconnectedEvent += OnPeerDisconnected;
            m_Listener.ConnectionRequestEvent += OnConnectionRequest;
            m_Listener.NetworkReceiveEvent += OnReceive;
            m_Network = new NetManager(m_Listener);

            m_Port = port;
        }

        void OnConnectionRequest(ConnectionRequest request) {
            request.AcceptIfKey(m_Key);
        }

        void OnPeerConnected(NetPeer peer) {
            WriteLine($"Peer connected, ep={peer.EndPoint}, id={peer.Id}");
        }

        void OnPeerDisconnected(NetPeer peer, DisconnectInfo disconnectInfo) {
            EGRUserInfo userInfo = FindUserFromPeer(peer);
            if (userInfo != null) {
                m_Users.Remove(userInfo.SessionKey);
            }
        }

        void OnReceive(NetPeer peer, NetPacketReader reader, DeliveryMethod method) {
            try {
                PacketNature nature = PacketNature.Out;//(PacketNature)reader.GetByte();
                PacketType type = (PacketType)reader.GetUShort();

                string sessionKey = reader.GetString();
                m_SkeyOfLast = sessionKey;

                int bufferedReq = reader.GetInt();

                /*Packet packet = Packet.CreatePacketInstance(nature, type);
                if (packet == null) {
                    WriteLine($"Cannot create packet, n={nature}, t={type}");
                    return;
                }*/

                PacketDataStream dataStream = new PacketDataStream(reader.GetRemainingBytes(), nature);
                dataStream.Prepare();

                m_OnPacketReceived?.Invoke(peer, type, dataStream, bufferedReq);

                WriteLine($"[{peer.EndPoint}, {sessionKey}] packet, n={nature}, t={type}, SZ={dataStream.Data.Length} bytes");

                dataStream.Clean();
            }
            catch { 
            }
        }

        public bool Start() {
            if (m_Network.IsRunning)
                return false;

            return m_Network.Start(m_Port);
        }

        public void UpdateNetwork() {
            m_Network.PollEvents();
        }

        void SendPacket(NetPeer peer, int buf, Packet packet, DeliveryMethod deliveryMethod, DataWriteDelegate writeDelegate) {
            //Packet must be in
            if (packet.PacketNature != PacketNature.In)
                return;

            PacketDataStream dataStream = new PacketDataStream(null, PacketNature.In);
            dataStream.Prepare();

            dataStream.WriteByte((byte)PacketNature.In);
            dataStream.WriteUInt16((ushort)packet.PacketType);
            dataStream.WriteInt32(buf);

            writeDelegate(dataStream);

            peer.Send(dataStream.Data, deliveryMethod);

            dataStream.Clean();
        }

        EGRUserInfo FindUserFromHWID(string hwid) {
            foreach (EGRUserInfo userInfo in m_Users.Values) {
                if (userInfo.HWID == hwid)
                    return userInfo;
            }

            return null;
        }

        EGRUserInfo FindUserFromPeer(NetPeer peer) {
            foreach (EGRUserInfo userInfo in m_Users.Values) {
                if (userInfo.Peer == peer)
                    return userInfo;
            }

            return null;
        }

        EGRUserInfo FindUserFromDataKey(string dataKey) {
            foreach (EGRUserInfo userInfo in m_Users.Values) {
                if (userInfo.DataKey == dataKey)
                    return userInfo;
            }

            return null;
        }

        EGRUserInfo FindUserFromSessionKey(string skey) {
            foreach (EGRUserInfo userInfo in m_Users.Values) {
                if (userInfo.SessionKey == skey)
                    return userInfo;
            }

            return null;
        }

        void INTERNAL_OnPacketReceived(NetPeer peer, PacketType packet, PacketDataStream stream, int buf) {
            OnPacketReceivedDelegate onrec = null;

            onrec?.Invoke(peer, packet, stream, buf);
        }

        void HandleInSessionKey(NetPeer peer, PacketType packet, PacketDataStream stream, int buf) {
            //send session key
            string hwid = stream.ReadString();
            WriteLine($"Received HWID: {hwid}");

            EGRUserInfo userInfo = FindUserFromHWID(hwid);
            if (userInfo != null)
                m_Users.Remove(userInfo.SessionKey);
            else
                userInfo = new EGRUserInfo(hwid, peer);

            string sessionKey = EGRUtils.GetRandomString(10); //skey len
            userInfo.SessionKey = sessionKey;
            m_Users[sessionKey] = userInfo;

            SendPacket(peer, buf, new PacketInSessionKey(), DeliveryMethod.ReliableOrdered, x => {
                x.WriteString(sessionKey);
            });
        }

        Packet CreateStationaryPacket(PacketType type) {
            return new Packet(PacketNature.In, type);
        }

        string GetRandomDataKey() {
            return new Random().Next(int.MinValue, int.MaxValue).ToString();
        }

        void HandleInAuthData(NetPeer peer, PacketType packet, PacketDataStream stream, int buf) {
            string username = stream.ReadString();
            string phash = stream.ReadString();

            if (username == "mrk") {
                EGRUserInfo _userInf = FindUserFromSessionKey(m_SkeyOfLast); //no reg for now
                _userInf.Username = username;
                _userInf.Nickname = username.ToUpper(); //all test stuff
                _userInf.PasswordHash = phash;
                _userInf.IsAuthenticated = true;
                _userInf.DataKey = GetRandomDataKey();

                SendPacket(peer, buf, CreateStationaryPacket(PacketType.XKEY), DeliveryMethod.ReliableOrdered, x => {
                    x.WriteBool(true); //success
                    x.WriteString(_userInf.DataKey); //datakey
                });
            }
        }

        void HandleInFetchData(NetPeer peer, PacketType packet, PacketDataStream stream, int buf) {
            string dataKey = stream.ReadString(); //read the data key

            EGRUserInfo _userInf = FindUserFromDataKey(dataKey);
            if (_userInf == null)
                return;

            SendPacket(peer, buf, CreateStationaryPacket(PacketType.XKEY), DeliveryMethod.ReliableOrdered, x => {
                x.WriteString(_userInf.Username);
                x.WriteString(_userInf.Nickname);
                x.WriteInt32(_userInf.Cash);
                x.WriteInt32(_userInf.Gold);
                x.WriteInt32(_userInf.Exp);
            });
        }

        void HandleInChatMessage(NetPeer peer, PacketType packet, PacketDataStream stream, int buf) {
            string username = stream.ReadString();
            string nickname = stream.ReadString();
            string content = stream.ReadString();

            foreach (NetPeer _peer in m_Network.ConnectedPeerList) {
                SendPacket(_peer, buf, CreateStationaryPacket(PacketType.XKEY), DeliveryMethod.ReliableOrdered, x => {
                    x.WriteString(username);
                    x.WriteString(nickname);
                    x.WriteString(content);
                });
            }
        }
    }
}