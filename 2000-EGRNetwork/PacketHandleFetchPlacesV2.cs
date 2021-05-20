using System;
using System.Collections.Generic;
using static MRK.EGRLogger;

namespace MRK.Networking.Packets {
    [PacketHandler(PacketType.PLCFETCHV2)]
    public class PacketHandleFetchPlacesV2 {
        static string Hash(List<EGRPlace> places) {
            if (places == null)
                return "";

            byte[] bytes = new byte[places.Count * 8]; //CID hash

            int idx = 0;
            foreach (EGRPlace place in places)
                Buffer.BlockCopy(BitConverter.GetBytes(place.CIDNum), 0, bytes, idx++ * 8, 8);

            return EGRUtils.CalculateRawHash(bytes);
        }

        static void Handle(EGRNetwork network, EGRSessionUser sessionUser, PacketDataStream stream, int buffer) {
            if (string.IsNullOrEmpty(sessionUser.HWID)) {
                LogError($"[{sessionUser.Peer.Id}] does not have a valid hwid, hwid={sessionUser.HWID}");
                return;
            }

            if (sessionUser.Account == null) {
                //LogError($"[{sessionUser.Peer.Id}] is not logged in, hwid={sessionUser.HWID}");
                //return;
            }

            int hash = stream.ReadInt32();
            string tileHash = stream.ReadString();
            double minLat = stream.ReadDouble();
            double minLng = stream.ReadDouble();
            double maxLat = stream.ReadDouble();
            double maxLng = stream.ReadDouble();
            int zoomLvl = stream.ReadInt32();

            List<EGRPlace> places = network.PlaceManager.GetPlaces(minLat, minLng, maxLat, maxLng, zoomLvl, sessionUser.SentCIDs);
            string realTileHash = Hash(places);
            if (realTileHash == tileHash) {
                LogInfo($"Client has matching hash {tileHash}");
                return;
            }

            LogInfo($"[{sessionUser.Peer.Id}] fetchplcV2, min({minLat}, {minLng}), max({maxLat}, {maxLng}), z={zoomLvl}, found {places.Count} places");

            network.SendPacket(sessionUser.Peer, buffer, PacketType.PLCFETCHV2, DeliveryMethod.ReliableOrdered, (x) => {
                x.WriteInt32(hash);
                x.WriteInt32(places.Count);

                foreach (EGRPlace place in places) {
                    sessionUser.SentCIDs.Add(place.CID);

                    x.WriteString(place.Name);
                    x.WriteString(place.Type);
                    x.WriteUInt64(place.CIDNum);
                    x.WriteString(place.Address);
                    x.WriteDouble(place.Latitude);
                    x.WriteDouble(place.Longitude);

                    x.WriteInt32(place.Ex.Length);
                    foreach (string ex in place.Ex)
                        x.WriteString(ex);

                    x.WriteUInt64(place.Chain);

                    x.WriteInt32(place.Types.Length);
                    foreach (EGRPlaceType type in place.Types)
                        x.WriteUInt16((ushort)type);
                }

                x.WriteString(realTileHash);
            });
        }
    }
}
