using static MRK.EGRLogger;

namespace MRK.Networking.Packets {
    [PacketHandler(PacketType.UPDACC)]
    public class PacketHandleUpdateAccountInfo {
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
            string name = stream.ReadString();
            string email = stream.ReadString();
            sbyte gender = stream.ReadSByte();

            bool success = EGRMain.Instance.AccountManager.UpdateAccountInfo(token, name, email, gender, sessionUser);
            LogInfo($"[{sessionUser.Peer.Id}] update acc info, em={email}, name={name}, gender={gender}, hwid={sessionUser.HWID}, result={success}");

            network.SendStandardResponsePacket(sessionUser.Peer, buffer, success ? EGRStandardResponse.SUCCESS : EGRStandardResponse.FAILED);
        }
    }
}
