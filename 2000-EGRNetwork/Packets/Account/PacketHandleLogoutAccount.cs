using static MRK.Logger;

namespace MRK.Networking.Packets {
    [PacketHandler(PacketType.LGNOUT)]
    public class PacketHandleLogoutAccount {
        static void Handle(Network network, NetworkUser sessionUser, PacketDataStream stream, int buffer) {
            if (string.IsNullOrEmpty(sessionUser.HWID)) {
                LogError($"[{sessionUser.Peer.Id}] does not have a valid hwid, hwid={sessionUser.HWID}");
                return;
            }

            if (sessionUser.Account == null) {
                LogError($"[{sessionUser.Peer.Id}] is not logged in, hwid={sessionUser.HWID}");
                return;
            }

            sessionUser.AssignAccount(null);
            sessionUser.Token = null;

            LogInfo($"[{sessionUser.Peer.Id}] logout acc, hwid={sessionUser.HWID}");
        }
    }
}
