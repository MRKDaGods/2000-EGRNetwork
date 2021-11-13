using System;
using System.Net;
using static MRK.Logger;

namespace MRK.Networking.Packets {
    [PacketHandler(PacketType.QUERYDIRS)]
    public class PacketHandleQueryDirections {
        static readonly string[] ms_Profiles = new string[3] {
            "driving", "walking", "cycling"
        };

        static void Handle(Network network, NetworkUser sessionUser, PacketDataStream stream, int buffer) {
            double fromLat = stream.ReadDouble();
            double fromLng = stream.ReadDouble();
            double toLat = stream.ReadDouble();
            double toLng = stream.ReadDouble();
            byte profile = stream.ReadByte();

            if (profile >= ms_Profiles.Length)
                return;

            WebClient wc = new WebClient();
            try {
                string response = wc.DownloadString(
                    "https://api.mapbox.com/directions/v5/mapbox/" +
                    ms_Profiles[profile] +
                    $"/{fromLng},{fromLat};{toLng},{toLat}" +
                    "?alternatives=true" +
                    "&geometries=geojson" +
                    "&steps=true" +
                    "&access_token=" +
                    "pk.eyJ1IjoiMjAwMGVneXB0IiwiYSI6ImNrbHI5dnlwZTBuNTgyb2xsOTRvdnQyN2QifQ.fOW4YjVUAE5fjwtL9Etajg");

                network.SendPacket(sessionUser.Peer, buffer, PacketType.STDJSONRSP, DeliveryMethod.ReliableOrdered, x => {
                    x.WriteString(response);
                });
            }
            catch {
            }
        }
    }
}
