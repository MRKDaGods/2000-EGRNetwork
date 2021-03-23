using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MRK.Networking.Packets;
using static MRK.EGRLogger;

namespace MRK.Networking {
    public class EGRSessionUser {
        public NetPeer Peer { get; private set; }
        public string XorKey { get; private set; }
        public string HWID { get; private set; }
        public EGRAccount Account { get; private set; }
        public EGRToken Token { get; set; }

        public EGRSessionUser(NetPeer peer) {
            Peer = peer;
        }

        public void AssignXorKey(string key) {
            XorKey = key;

            EGRNetwork.Instance.SendPacket(Peer, -1, PacketType.XKEY, DeliveryMethod.ReliableOrdered, (writer) => {
                writer.WriteString(key);
            });

            LogInfo($"[{Peer.Id}] Assigned xor key to {key}");
        }

        //only hwid for now?
        public void AssignDeviceInfo(string hwid) {
            HWID = hwid;
        }

        public void AssignAccount(EGRAccount acc) {
            Account = acc;

            LogInfo($"[{Peer.Id}] Assigned account, token={Token.Token}");
        }
    }
}
