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
                //LogError($"[{sessionUser.Peer.Id}] is not logged in, hwid={sessionUser.HWID}");
                //return;
            }

            ulong cid = stream.ReadUInt64();

            EGRPlace place = EGRMain.Instance.PlaceManager.GetPlace(cid);
            LogInfo($"[{sessionUser.Peer.Id}] fetchplc, {cid}");

            network.SendPacket(sessionUser.Peer, buffer, PacketType.PLCFETCH, DeliveryMethod.ReliableOrdered, (x) => {
                bool exists = place != null;
                x.WriteBool(exists);

                if (exists) {
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
            });
        }
    }
}
