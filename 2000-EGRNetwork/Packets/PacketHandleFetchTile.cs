using static MRK.EGRLogger;

namespace MRK.Networking.Packets {
    [PacketHandler(PacketType.TILEFETCH)]
    public class PacketHandleFetchTile : MRKBehaviour {
        static void Handle(EGRNetwork network, EGRSessionUser sessionUser, PacketDataStream stream, int buffer) {
            if (!EGRSessionUser.IsValidUser(sessionUser, false)) {
                return;
            }

            if (sessionUser.TilePipe == null) {
                LogInfo("TILEPIPE NULL");
                return;
            }

            string tileset = stream.ReadString();
            int z = stream.ReadInt32();
            int x = stream.ReadInt32();
            int y = stream.ReadInt32();
            bool low = stream.ReadBool();

            EGRTileID tileID = new() {
                Z = z,
                X = x,
                Y = y
            };

            sessionUser.TilePipe.QueueTile(new EGRUserTileRequest {
                Tileset = tileset,
                TileID = tileID,
                Hash = tileID.GetHashCode(),
                Buffer = buffer,
                Low = low,
                Cancelled = false
            });

            /* Client.TileManager.GetTile(tileset, tileID, low, (tile) => {
                network.SendPacket(sessionUser.Peer, buffer, PacketType.TILEFETCH, DeliveryMethod.ReliableUnordered, (stream) => {
                    bool success = tile != null;
                    stream.WriteBool(success);
                    stream.Write<EGRTileID>(tileID);

                    if (success) {
                        stream.WriteInt32(tile.Data.Length); //dataSz
                        stream.WriteBytes(tile.Data); //data
                    }
                });
            }); */
        }
    }
}
