using static MRK.EGRLogger;

namespace MRK.Networking.Packets {
    [PacketHandler(PacketType.TILEFETCH)]
    public class PacketHandleFetchTile {
        static void Handle(EGRNetwork network, EGRSessionUser sessionUser, PacketDataStream stream, int buffer) {
            if (string.IsNullOrEmpty(sessionUser.HWID)) {
                LogError($"[{sessionUser.Peer.Id}] does not have a valid hwid, hwid={sessionUser.HWID}");
                return;
            }

            //if (sessionUser.Account == null) {
            //    LogError($"[{sessionUser.Peer.Id}] is not logged in, hwid={sessionUser.HWID}");
            //    return;
            //}

            string tileset = stream.ReadString();
            int z = stream.ReadInt32();
            int x = stream.ReadInt32();
            int y = stream.ReadInt32();

            EGRTileID tileID = new EGRTileID { Z = z, X = x, Y = y };
            bool success = EGRMain.Instance.TileManager.GetTile(tileset, tileID);
            EGRDownloadRequest downloadRequest = success ? network.CreateDownloadRequest(sessionUser.Peer, tileID.Data) : null;
            network.SendPacket(sessionUser.Peer, buffer, PacketType.TILEFETCH, DeliveryMethod.ReliableOrdered, _x => {
                _x.WriteByte((byte)(success ? EGRStandardResponse.SUCCESS : EGRStandardResponse.FAILED));
                if (success) {
                    _x.WriteUInt64(downloadRequest.ID);
                }
            });

            if (downloadRequest != null)
                network.StartDownload(downloadRequest);
        }
    }
}
