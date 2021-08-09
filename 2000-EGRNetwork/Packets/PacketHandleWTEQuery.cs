using MRK.WTE;
using System.Collections.Generic;
using static MRK.EGRLogger;

namespace MRK.Networking.Packets {
    [PacketHandler(PacketType.WTEQUERY)]
    public class PacketHandleWTEQuery {
        static void Handle(EGRNetwork network, EGRSessionUser sessionUser, PacketDataStream stream, int buffer) {
            if (!EGRSessionUser.IsValidUser(sessionUser))
                return;

            byte people = stream.ReadByte();
            int price = stream.ReadInt32();
            string cuisine = stream.ReadString();

            HashSet<Place> queryRes = network.WTE.Query(people, price, cuisine);

            network.SendPacket(sessionUser.Peer, buffer, PacketType.WTEQUERY, DeliveryMethod.ReliableOrdered, (x) => {
                x.WriteInt32(queryRes.Count);
                foreach (Place p in queryRes) {
                    x.WriteString(p.Name);
                }
            });
        }
    }
}
