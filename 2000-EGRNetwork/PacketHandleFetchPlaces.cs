using System.Collections.Generic;
using static MRK.EGRLogger;

namespace MRK.Networking.Packets {
    [PacketHandler(PacketType.PLCFETCH)]
    public class PacketHandleFetchPlaces{
        static void Handle(EGRNetwork network, EGRSessionUser sessionUser, PacketDataStream stream, int buffer) {
            if (string.IsNullOrEmpty(sessionUser.HWID)) {
                LogError($"[{sessionUser.Peer.Id}] does not have a valid hwid, hwid={sessionUser.HWID}");
                return;
            }

            if (sessionUser.Account == null) {
                LogError($"[{sessionUser.Peer.Id}] is not logged in, hwid={sessionUser.HWID}");
                return;
            }

            double minLat = stream.ReadDouble();
            double minLng = stream.ReadDouble();
            double maxLat = stream.ReadDouble();
            double maxLng = stream.ReadDouble();
            int zoomLvl = stream.ReadInt32();

            List<EGRPlace> places = network.PlaceManager.GetPlaces(minLat, minLng, maxLat, maxLng, zoomLvl);
            LogInfo($"[{sessionUser.Peer.Id}] fetchplcs, min({minLat}, {minLng}), max({maxLat}, {maxLng}), z={zoomLvl}, found {places.Count} places");

            network.SendPacket(sessionUser.Peer, buffer, PacketType.PLCFETCH, DeliveryMethod.ReliableOrdered, (x) => {
                x.WriteInt32(places.Count);

                foreach (EGRPlace place in places) {
                    x.WriteString(place.Name);
                    x.WriteString(place.Type);
                    x.WriteString(place.CID);
                    x.WriteString(place.Address);
                    x.WriteDouble(place.Latitude);
                    x.WriteDouble(place.Longitude);

                    x.WriteInt32(place.Ex.Length);
                    foreach (string ex in place.Ex)
                        x.WriteString(ex);
                }
            });
        }
    }
}
