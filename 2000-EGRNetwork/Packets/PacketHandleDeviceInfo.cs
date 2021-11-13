using static MRK.Logger;

namespace MRK.Networking.Packets {
    [PacketHandler(PacketType.DEVINFO)]
    public class PacketHandleDeviceInfo {
        static void Handle(Network network, NetworkUser sessionUser, PacketDataStream stream, int buffer) {
            string hwid = stream.ReadString();

            sessionUser.AssignDeviceInfo(hwid);

            LogInfo($"[{sessionUser.Peer.Id}] Assigned device info, hwid={hwid}");
        }
    }
}
