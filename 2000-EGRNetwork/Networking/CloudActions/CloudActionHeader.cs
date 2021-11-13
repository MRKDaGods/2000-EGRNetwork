using MRK.Networking.Internal.Utils;

namespace MRK.Networking.CloudActions
{
    public class CloudActionHeader : INetSerializable
    {
        public int CloudAPIVersion
        {
            get; set;
        }

        public string CloudActionToken
        {
            get; set;
        }

        public CloudResponse Response
        {
            get; set;
        }

        public CloudActionHeader(int cloudAPIVersion, string actionToken)
        {
            CloudAPIVersion = cloudAPIVersion;
            CloudActionToken = actionToken;
        }

        public void Deserialize(NetDataReader reader)
        {
            CloudAPIVersion = reader.GetInt();
            CloudActionToken = reader.GetString();
            Response = (CloudResponse)reader.GetByte();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(CloudAPIVersion);
            writer.Put(CloudActionToken);
            writer.Put((byte)Response);
        }
    }
}
