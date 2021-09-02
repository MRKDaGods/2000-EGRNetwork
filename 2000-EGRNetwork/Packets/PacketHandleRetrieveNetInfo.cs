using static MRK.EGRLogger;

namespace MRK.Networking.Packets {
    [PacketHandler(PacketType.NETINFO)]
    public class PacketHandleRetrieveNetInfo {
        static void Handle(EGRNetwork network, EGRSessionUser sessionUser, PacketDataStream stream, int buffer) {
            if (!EGRSessionUser.IsValidUser(sessionUser)) {
                return;
            }

            sessionUser.AllocateCDN(); //alloc cdn if needed
            network.SendPacket(sessionUser.Peer, buffer, PacketType.NETINFO,
                DeliveryMethod.ReliableOrdered, (stream) => OnStreamWrite(stream, sessionUser));

            LogInfo($"[{sessionUser.Account.FullName}] Sent CDN ({sessionUser.CDNInfo})");
        }

        static void OnStreamWrite(PacketDataStream stream, EGRSessionUser user) {
            if (user.CDNInfo != null) {
                stream.WriteInt32(user.CDNInfo.Port);
                stream.WriteString(user.CDNInfo.Key);
            }
        }
    }
}
