using MRK.Networking.Internal.Utils;

namespace MRK.Networking.CloudActions
{
    public class CloudActionHeader : INetSerializable
    {
        private int _requestFieldsLength;

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

        public int RequestFieldsLength
        {
            set { _requestFieldsLength = value; }
        }

        public int ResponseFieldsLength
        {
            get; private set;
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
            ResponseFieldsLength = reader.GetInt();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(CloudAPIVersion);
            writer.Put(CloudActionToken);
            writer.Put(_requestFieldsLength);
        }
    }
}
