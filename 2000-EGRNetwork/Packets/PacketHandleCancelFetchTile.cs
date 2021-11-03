namespace MRK.Networking.Packets {
    [PacketHandler(PacketType.TILEFETCHCANCEL)]
    public class PacketHandleCancelFetchTile : MRKBehaviour {
        static void Handle(EGRNetwork network, EGRSessionUser sessionUser, PacketDataStream stream, int buffer) {
            if (!EGRSessionUser.IsValidUser(sessionUser, false))
                return;

            if (sessionUser.CDNInfo == null)
                return;

            EGRSessionUser proxyCdnUser = sessionUser.GetProxyUser(sessionUser.CDNInfo.CDN.Network);
            if (proxyCdnUser.TilePipe == null)
                return;

            string tileset = stream.ReadString();
            int hash = stream.ReadInt32();
            bool low = stream.ReadBool();
            proxyCdnUser.TilePipe.CancelRequest(tileset, hash, low);
            EGRLogger.LogInfo("Cancelled request");
        }
    }
}