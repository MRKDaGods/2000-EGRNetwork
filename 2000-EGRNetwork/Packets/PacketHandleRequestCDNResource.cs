using static MRK.EGRLogger;

namespace MRK.Networking.Packets {
    [PacketHandler(PacketType.CDNRESOURCE)]
    public class PacketHandleRequestCDNResource : EGRBase {
        static void Handle(EGRNetwork network, EGRSessionUser sessionUser, PacketDataStream stream, int buffer) {
            //if (!EGRSessionUser.IsValidUser(sessionUser)) {
            //    return;
            //}

            string resourceStr = stream.ReadString();
            byte[] sig = stream.ReadBytes(16);

            EGRCDNResource resource;
            bool success = Client.CDNNetwork.ResourceManager.QueryResource(resourceStr, sig, out resource);

            LogInfo($"Requesting resource '{resourceStr}' res={success}");

            network.SendPacket(sessionUser.Peer, buffer, PacketType.CDNRESOURCE, 
                DeliveryMethod.ReliableOrdered, (stream) => OnStreamWrite(stream, success, resource));
        }

        static void OnStreamWrite(PacketDataStream stream, bool success, EGRCDNResource resource) {
            stream.WriteByte((byte)(success ? EGRStandardResponse.SUCCESS : EGRStandardResponse.FAILED));
            
            if (success) {
                stream.WriteInt32((int)resource.Header.Size);
                stream.WriteBytes(resource.Data);
            }
        }
    }
}
