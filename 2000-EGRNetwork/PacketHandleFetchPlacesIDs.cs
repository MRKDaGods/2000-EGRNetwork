using System.Collections.Generic;
using static MRK.EGRLogger;

namespace MRK.Networking.Packets {
    [PacketHandler(PacketType.PLCIDFETCH)]
    public class PacketHandleFetchPlacesIDs {
        static void Handle(EGRNetwork network, EGRSessionUser sessionUser, PacketDataStream stream, int buffer) {
            if (string.IsNullOrEmpty(sessionUser.HWID)) {
                LogError($"[{sessionUser.Peer.Id}] does not have a valid hwid, hwid={sessionUser.HWID}");
                return;
            }

            if (sessionUser.Account == null) {
                LogError($"[{sessionUser.Peer.Id}] is not logged in, hwid={sessionUser.HWID}");
                return;
            }

            ulong ctx = stream.ReadUInt64();
            double minLat = stream.ReadDouble();
            double minLng = stream.ReadDouble();
            double maxLat = stream.ReadDouble();
            double maxLng = stream.ReadDouble();
            int zoomLvl = stream.ReadInt32();

            List<EGRPlace> places = network.PlaceManager.GetPlaces(minLat, minLng, maxLat, maxLng, zoomLvl);
            LogInfo($"[{sessionUser.Peer.Id}] fetchidplcs, ctx={ctx} min({minLat}, {minLng}), max({maxLat}, {maxLng}), z={zoomLvl}, found {places.Count} places");

            network.SendPacket(sessionUser.Peer, buffer, PacketType.PLCIDFETCH, DeliveryMethod.ReliableOrdered, (x) => {
                x.WriteUInt64(ctx);
                x.WriteInt32(places.Count);

                foreach (EGRPlace place in places) {
                    x.WriteUInt64(place.CIDNum);
                }
            });
        }
    }
}
