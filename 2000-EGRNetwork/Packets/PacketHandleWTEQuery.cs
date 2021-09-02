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

            EGRWTE wte = EGRMain.Instance.WTE;
            HashSet<Place> queryRes = wte.Query(people, price, cuisine);
            HashSet<WTEProxyPlace> proxyRes = wte.ProxifyQuery(queryRes);
            network.SendPacket(sessionUser.Peer, buffer, PacketType.WTEQUERY, DeliveryMethod.ReliableOrdered, (stream) => OnStreamWrite(stream, proxyRes));
        }

        static void OnStreamWrite(PacketDataStream stream, HashSet<WTEProxyPlace> proxyRes) {
            if (proxyRes == null) {
                stream.WriteInt32(0);
                return;
            }

            stream.WriteInt32(proxyRes.Count);
            foreach (WTEProxyPlace place in proxyRes) {
                stream.WriteString(place.Name);
                stream.WriteList(place.Tags, (tag, _stream) => {
                    _stream.WriteString(tag);
                });

                stream.WriteSingle(place.GeneralMinimum);
                stream.WriteSingle(place.GeneralMaximum);
            }
        }
    }
}
