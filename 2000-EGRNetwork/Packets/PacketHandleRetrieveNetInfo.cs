using MRK.Networking.Internal;
using static MRK.Logger;

namespace MRK.Networking.Packets {
    [PacketHandler(PacketType.NETINFO)]
    public class PacketHandleRetrieveNetInfo {
        static void Handle(Network network, NetworkUser sessionUser, PacketDataStream stream, int buffer) {
            if (!NetworkUser.IsValidUser(sessionUser)) {
                return;
            }

            sessionUser.AllocateCDN(); //alloc cdn if needed
            network.SendPacket(sessionUser.Peer, buffer, PacketType.NETINFO,
                DeliveryMethod.ReliableOrdered, (stream) => OnStreamWrite(stream, sessionUser));

            LogInfo($"[{sessionUser.Account.FullName}] Sent CDN ({sessionUser.CDNInfo})");
        }

        static void OnStreamWrite(PacketDataStream stream, NetworkUser user) {
            if (user.CDNInfo != null) {
                stream.WriteInt32(user.CDNInfo.Port);
                stream.WriteString(user.CDNInfo.Key);
            }
        }
    }
}
