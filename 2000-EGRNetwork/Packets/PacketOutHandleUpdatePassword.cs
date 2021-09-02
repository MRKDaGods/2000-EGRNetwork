using static MRK.EGRLogger;

namespace MRK.Networking.Packets {
    [PacketHandler(PacketType.UPDACCPWD)]
    public class PacketHandleUpdatePassword{
        static void Handle(EGRNetwork network, EGRSessionUser sessionUser, PacketDataStream stream, int buffer) {
            if (string.IsNullOrEmpty(sessionUser.HWID)) {
                LogError($"[{sessionUser.Peer.Id}] does not have a valid hwid, hwid={sessionUser.HWID}");
                return;
            }

            if (sessionUser.Account == null) {
                LogError($"[{sessionUser.Peer.Id}] is not logged in, hwid={sessionUser.HWID}");
                return;
            }

            string token = stream.ReadString();
            string pass = stream.ReadString();
            bool logoutAll = stream.ReadBool();

            bool success = EGRMain.Instance.AccountManager.UpdatePassword(token, pass, logoutAll, sessionUser);
            LogInfo($"[{sessionUser.Peer.Id}] update acc pwd, pwd={pass}, hwid={sessionUser.HWID}, result={success}");

            network.SendStandardResponsePacket(sessionUser.Peer, buffer, success ? EGRStandardResponse.SUCCESS : EGRStandardResponse.FAILED);
        }
    }
}
