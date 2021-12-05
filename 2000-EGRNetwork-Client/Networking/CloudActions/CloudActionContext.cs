using MRK.Networking.Internal.Utils;
using MRK.Networking.CloudActions.Transport;

namespace MRK.Networking.CloudActions
{
    public class CloudActionContext
    {
        private readonly NetDataWriter _requestDataWriter;
        private readonly CloudActionHeader _requestHeader;
        private readonly Dictionary<string, CloudRequestField> _requestFields;
        private readonly CloudActionHeader _responseHeader;
        private Dictionary<string, CloudResponseField> _responseFields;
        private NetDataReader? _responseDataReader;
        private readonly TrackedTransport _transport;
        private bool _serialized;
        private int _miniTokenOffset;

        public event Action? ResponseReceived;

        public string CloudActionToken
        {
            set { _requestHeader.CloudActionToken = value; }
        }

        public CloudResponse Response
        {
            get { return _responseHeader.Response; }
            set { _responseHeader.Response = value; }
        }

        public NetDataWriter RequestData
        {
            get { return _requestDataWriter; }
        }

        public CloudActionHeader RequestHeader
        {
            get { return _requestHeader; }
        }

        public NetDataReader? ResponseData
        {
            get { return _responseDataReader; }
        }

        public TrackedTransport Transport
        {
            get { return _transport; }
        }

        public CloudAction? Action
        {
            get; set;
        }

        public string FailInfo
        {
            get { string fail; return GetResponseField("FailInfo", out fail) ? fail : string.Empty; }
        }

        public CloudActionContext(TrackedTransport transport, int cloudAPIVersion)
        {
            _requestDataWriter = new NetDataWriter();
            _requestHeader = new CloudActionHeader(cloudAPIVersion, string.Empty);
            _requestFields = new Dictionary<string, CloudRequestField>();
            _responseHeader = new CloudActionHeader(cloudAPIVersion, string.Empty);

            _transport = transport;
        }

        private static bool ValidateField(CloudRequestField requestField)
        {
            return requestField != null 
                && !string.IsNullOrEmpty(requestField.Key) 
                && requestField.Value != null 
                && requestField.Serialize != null;
        }

        public void AddField(CloudRequestField requestField)
        {
            if (ValidateField(requestField))
            {
                _requestFields.Add(requestField.Key, requestField);
            }
        }

        public void Serialize()
        {
            if (Action == null)
            {
                Logger.LogError("Cannot serialize context with null action");
                return;
            }

            if (_serialized)
            {
                Logger.LogError("Serialize has already been called");
                return;
            }

            _serialized = true;

            //write action path
            _requestDataWriter.Put(Action.Path);

            //allocate 10 bytes for 
            _miniTokenOffset = _requestDataWriter.Length;
            _requestDataWriter.Put(new string(' ', TrackedTransport.MiniActionTokenLength));

            _requestHeader.RequestFieldsLength = _requestFields.Count;
            _requestHeader.Serialize(_requestDataWriter);

            //serialize our request fields
            if (_requestFields.Count > 0)
            {
                foreach (var pair in _requestFields)
                {
                    _requestDataWriter.Put(pair.Key);
                    pair.Value.Serialize(pair.Value.Value, _requestDataWriter);
                }
            }
        }

        public void WriteMiniActionTokenToSerializedBuffer(string? miniActionToken)
        {
            if (string.IsNullOrEmpty(miniActionToken) || miniActionToken.Length != TrackedTransport.MiniActionTokenLength)
            {
                Logger.LogError("Invalid mini action token");
                return;
            }

            _requestDataWriter.SetPosition(_miniTokenOffset);
            _requestDataWriter.Put(miniActionToken);
            Logger.LogInfo("Written miniacttoken to serialized buffer");
        }

        private void DeserializeResponseFields()
        {
            if (_responseDataReader == null) throw new ArgumentException();
            if (_responseHeader.ResponseFieldsLength == 0) return;

            if (_responseFields == null)
            {
                _responseFields = new Dictionary<string, CloudResponseField>();
            }
            else
            {
                _responseFields.Clear();
            }

            for (int i = 0; i < _responseHeader.ResponseFieldsLength; i++)
            {
                string fieldKey = _responseDataReader.GetString();
                byte[] dynamicSerialization = _responseDataReader.GetBytesWithLength();

                CloudResponseField responseField = new CloudResponseField(fieldKey, dynamicSerialization);
                if (!responseField.Resolve(_responseDataReader)) throw new Exception();

                _responseFields[fieldKey] = responseField;
            }
        }

        public void SetResponse(NetDataReader reader)
        {
            _responseDataReader = reader;

            try
            {
                _responseHeader.Deserialize(reader);
                DeserializeResponseFields();
            }
            catch
            {
                Logger.LogError("Unable to set response");
                Response = CloudResponse.Failure;
            }

            if (ResponseReceived != null)
            {
                ResponseReceived();
            }
        }

        public bool GetResponseField<T>(string key, out T value)
        {
            value = default;
            if (_responseFields == null) return false;

            CloudResponseField field;
            if (!_responseFields.TryGetValue(key, out field)) return false;

            value = (T)field.Value;
            return true;
        }
    }
}
