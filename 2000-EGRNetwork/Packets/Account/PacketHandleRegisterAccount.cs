using static MRK.Logger;

namespace MRK.Networking.Packets {
    [PacketHandler(PacketType.REGACC)]
    public class PacketHandleRegisterAccount {
        static void Handle(Network network, NetworkUser sessionUser, PacketDataStream stream, int buffer) {
            if (string.IsNullOrEmpty(sessionUser.HWID)) {
                LogError($"[{sessionUser.Peer.Id}] does not have a valid hwid, hwid={sessionUser.HWID}");
                return;
            }

            string name = stream.ReadString();
            string email = stream.ReadString();
            string password = stream.ReadString();

            //do validation, etc

            bool success = EGR.Instance.AccountManager.RegisterAccount(name, email, password, sessionUser.HWID);
            LogInfo($"[{sessionUser.Peer.Id}] registered acc, n={name}, em={email}, pwd={password}, hwid={sessionUser.HWID}, result={success}");

            network.SendStandardResponsePacket(sessionUser.Peer, buffer, success ? EGRStandardResponse.SUCCESS : EGRStandardResponse.FAILED);
        }
    }
}
