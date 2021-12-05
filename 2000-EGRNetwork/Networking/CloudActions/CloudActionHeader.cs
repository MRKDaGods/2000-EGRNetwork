﻿using MRK.Networking.Internal.Utils;

namespace MRK.Networking.CloudActions
{
    public class CloudActionHeader : INetSerializable
    {
        private int _responseFieldsLength;

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

        public byte TrackedEventType
        {
            get; set;
        }

        public string MiniActionToken
        {
            get; set;
        }

        public int RequestFieldsLength
        {
            get; private set;
        }

        public int ResponseFieldsLength
        {
            set { _responseFieldsLength = value; }
        }

        public CloudActionHeader(int cloudAPIVersion, string actionToken)
        {
            CloudAPIVersion = cloudAPIVersion;
            CloudActionToken = actionToken;
        }

        public void Deserialize(NetDataReader reader)
        {
            MiniActionToken = reader.GetString();
            CloudAPIVersion = reader.GetInt();
            CloudActionToken = reader.GetString();
            RequestFieldsLength = reader.GetInt();
        }

        public void Serialize(NetDataWriter writer)
        {
            writer.Put(TrackedEventType); //tracked transport
            writer.Put(MiniActionToken); //tracked transport

            writer.Put(CloudAPIVersion); //header
            writer.Put(CloudActionToken); //header
            writer.Put((byte)Response); //header
            writer.Put(_responseFieldsLength); //header
        }
    }
}
