using static MRK.EGRLogger;

namespace MRK.Networking.Packets {
    [PacketHandler(PacketType.DEVINFO)]
    public class PacketHandleDeviceInfo {
        static void Handle(EGRNetwork network, EGRSessionUser sessionUser, PacketDataStream stream, int buffer) {
            string hwid = stream.ReadString();

            sessionUser.AssignDeviceInfo(hwid);

            LogInfo($"[{sessionUser.Peer.Id}] Assigned device info, hwid={hwid}");
        }
    }
}
