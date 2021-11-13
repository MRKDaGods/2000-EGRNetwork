namespace MRK.Networking.Packets {
    [PacketHandler(PacketType.TILEFETCHCANCEL)]
    public class PacketHandleCancelFetchTile : Behaviour {
        static void Handle(Network network, NetworkUser sessionUser, PacketDataStream stream, int buffer) {
            if (!NetworkUser.IsValidUser(sessionUser, false))
                return;

            if (sessionUser.CDNInfo == null)
                return;

            NetworkUser proxyCdnUser = sessionUser.GetProxyUser(sessionUser.CDNInfo.CDN.Network);
            if (proxyCdnUser.TilePipe == null)
                return;

            string tileset = stream.ReadString();
            int hash = stream.ReadInt32();
            bool low = stream.ReadBool();
            proxyCdnUser.TilePipe.CancelRequest(tileset, hash, low);
            Logger.LogInfo("Cancelled request");
        }
    }
}