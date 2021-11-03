using MRK.Networking.Packets;
using System.Collections.Generic;
using System.Linq;
using static MRK.EGRLogger;

namespace MRK.Networking {
    public class EGRSessionUser {
        public NetPeer Peer { get; private set; }
        public EGRNetwork Network { get; private set; }
        public string XorKey { get; private set; }
        public string HWID { get; private set; }
        public EGRAccount Account { get; private set; }
        public EGRToken Token { get; set; }
        public HashSet<string> SentCIDs { get; private set; }
        public EGRCDNInfo CDNInfo { get; private set; }
        public EGRUserTilePipe TilePipe { get; private set; }

        public EGRSessionUser(NetPeer peer, EGRNetwork network, bool enableTilePipe = false) {
            Peer = peer;
            Network = network;
            SentCIDs = new HashSet<string>();

            if (enableTilePipe)
                TilePipe = new(this);
        }

        ~EGRSessionUser() {
            if (TilePipe != null)
                TilePipe.Stop();
        }

        public void AssignXorKey(string key) {
            XorKey = key;

            Network.SendPacket(Peer, -1, PacketType.XKEY, DeliveryMethod.ReliableOrdered, (writer) => {
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

        public static bool IsValidUser(EGRSessionUser user, bool requireAccount = true) {
            return user != null && !string.IsNullOrEmpty(user.HWID) && (!requireAccount || user.Account != null);
        }

        public void AllocateCDN() {
            if (CDNInfo != null)
                return;

            CDNInfo = EGRMain.Instance.CDNNetwork.AllocateCDN();
        }

        public EGRSessionUser GetProxyUser(EGRNetwork other) {
            return other.Users.First(x => x.HWID == HWID);
        }
    }
}
