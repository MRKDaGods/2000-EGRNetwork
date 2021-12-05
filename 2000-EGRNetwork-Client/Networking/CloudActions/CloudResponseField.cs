using MRK.Networking.Internal.Utils;

namespace MRK.Networking.CloudActions
{
    public class CloudResponseField
    {
        private readonly byte[] _dynamicSerialization;
        private object _value;

        public string Key
        {
            get;
            init;
        }

        public object Value
        {
            get { return _value; }
        }

        public CloudResponseField(string key, byte[] dynamicSerialization)
        {
            Key = key;
            _dynamicSerialization = dynamicSerialization;
        }

        public bool Resolve(NetDataReader data)
        {
            if (_dynamicSerialization == null || _dynamicSerialization.Length == 0) return true;

            object value;
            if (!DynamicSerialization.Apply(data, _dynamicSerialization, out value)) return false;

            _value = value;
            return true;
        }
    }
}
