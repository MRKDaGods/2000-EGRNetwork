using MRK.Networking.Internal.Utils;

namespace MRK.Networking.CloudActions
{
    public class CloudRequestField
    {
        private readonly Action<object, NetDataWriter> _serialize;
        private byte[] _dynamicSerialization;

        public string Key { get; init; }
        public object Value { get; init; }

        public CloudRequestField(string key, object val, Action<object, NetDataWriter> serialize, params byte[] dynamicSerialization)
        {
            Key = key;
            Value = val;
            _serialize = serialize;
            _dynamicSerialization = dynamicSerialization;
        }

        public void Serialize(object val, NetDataWriter writer)
        {
            if (_dynamicSerialization != null)
            {
                writer.PutBytesWithLength(_dynamicSerialization);
            }
            else
            {
                writer.Put(0);
            }

            _serialize(val, writer);
        }
    }

    public class CloudRequestFieldString : CloudRequestField
    {
        public CloudRequestFieldString(string key, string val) : base(key, val, Serialize, DynamicSerialization.ReadString)
        {
        }

        private static new void Serialize(object val, NetDataWriter writer)
        {
            writer.Put((string)val);
        }
    }
}
