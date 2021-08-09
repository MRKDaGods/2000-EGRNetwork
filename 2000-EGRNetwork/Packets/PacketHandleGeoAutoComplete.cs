using System;
using System.Net;
using static MRK.EGRLogger;

namespace MRK.Networking.Packets {
    [PacketHandler(PacketType.GEOAUTOCOMPLETE)]
    public class PacketHandleGeoAutoComplete {
        static void Handle(EGRNetwork network, EGRSessionUser sessionUser, PacketDataStream stream, int buffer) {
            string query = stream.ReadString();
            double lat = stream.ReadDouble();
            double lng = stream.ReadDouble();

            if (!IsQueryValid(ref query))
                return;

            string escapedQuery = Uri.EscapeDataString(query);

            WebClient wc = new WebClient();
            string response = wc.DownloadString(
                "https://api.mapbox.com/geocoding/v5/mapbox.places/" +
                escapedQuery +
                ".json?proximity=" +
                $"{lat},{lng}" +
                "&limit=10" +
                "&autocomplete=true" +
                "&fuzzyMatch=true" +
                "&access_token=" + 
                "pk.eyJ1IjoiMjAwMGVneXB0IiwiYSI6ImNrbHI5dnlwZTBuNTgyb2xsOTRvdnQyN2QifQ.fOW4YjVUAE5fjwtL9Etajg");

            network.SendPacket(sessionUser.Peer, buffer, PacketType.GEOAUTOCOMPLETE, DeliveryMethod.ReliableOrdered, x => {
                x.WriteString(response);
            });
        }

        static bool IsQueryValid(ref string query) {
            if (query.Length > 256)
                return false;

            if (query.Split(' ').Length > 20)
                return false;

            query = query.Replace(';', ' ');
            return true;
        }
    }
}
