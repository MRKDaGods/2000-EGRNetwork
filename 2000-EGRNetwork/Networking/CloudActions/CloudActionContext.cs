using MRK.Networking.Internal.Utils;
using MRK.Threading;
using System;
using System.Collections.Generic;

namespace MRK.Networking.CloudActions
{
    public class CloudActionContext : InterlockedAccess
    {
        private const int MaximumSendCount = 10;

        private readonly CloudNetwork _cloudNetwork;
        private readonly CloudNetworkUser _networkUser;
        private readonly NetDataWriter _responseDataWriter;
        private readonly CloudActionHeader _inHeader;
        private readonly CloudActionHeader _outHeader;
        private string _actionToken;
        private bool _valid;
        private Dictionary<string, CloudRequestField> _requestFields;
        private readonly Dictionary<string, CloudResponseField> _responseFields;
        private bool _replied;
        private int _sendCount;
        private int _miniTokenOffset;

        public CloudNetwork CloudNetwork
        {
            get { return _cloudNetwork; }
        }

        public CloudNetworkUser NetworkUser
        {
            get { return _networkUser; }
        }

        public NetDataReader Data
        {
            get; init;
        }

        public CloudResponse Response
        {
            get { return _outHeader.Response; }
            set { _outHeader.Response = value; }
        }

        public string ActionToken
        {
            get { return _actionToken; }
        }

        public NetDataWriter DataWriter
        {
            get { return _responseDataWriter; }
        }

        public bool Valid
        {
            get { return _valid; }
        }

        public bool Sendable
        {
            get { return _sendCount < MaximumSendCount; }
        }

        public CloudActionHeader RequestHeader
        {
            get { return _inHeader; }
        }

        public CloudActionContext(CloudNetwork cloudNetwork, CloudNetworkUser networkUser, NetDataReader data, string transportToken, bool outOnly = false)
        {
            _cloudNetwork = cloudNetwork;
            _networkUser = networkUser;
            Data = data;
            _actionToken = transportToken;

            _responseDataWriter = new NetDataWriter();
            _inHeader = new CloudActionHeader(CloudNetwork.CloudAPIVersion, string.Empty);
            _outHeader = new CloudActionHeader(CloudNetwork.CloudAPIVersion, transportToken);
            _responseFields = new Dictionary<string, CloudResponseField>();
            _sendCount = 0;

            if (!outOnly)
            {
                DeserializeRequest();
            }
            else
            {
                _valid = true;
            }
        }

        private void WriteHeaderToStream(byte evtType = 0x1)
        {
            _miniTokenOffset = _responseDataWriter.Length + 1; // ... + trackedEvt
            _outHeader.TrackedEventType = evtType; //data=0x1
            _outHeader.ResponseFieldsLength = _responseFields.Count;
            _outHeader.Serialize(_responseDataWriter);

            if (_responseFields.Count > 0)
            {
                foreach (var pair in _responseFields)
                {
                    _responseDataWriter.Put(pair.Key);
                    pair.Value.Serialize(pair.Value.Value, _responseDataWriter);
                }
            }
        }

        public void Reply(string body)
        {
            if (_replied)
            {
                Logger.LogError("Cannot reply more than once to the same action");
                return;
            }

            _replied = true;

            WriteHeaderToStream();
            _responseDataWriter.Put(body);
            Send();
        }

        public void Reply()
        {
            if (_replied)
            {
                Logger.LogError("Cannot reply more than once to the same action");
                return;
            }

            _replied = true;

            WriteHeaderToStream();
            Send();
        }

        /// <summary>
        /// Reply without a minitoken
        /// </summary>
        public void ReplyScalar()
        {
            if (_replied)
            {
                Logger.LogError("Cannot reply more than once to the same action");
                return;
            }

            _replied = true;

            _outHeader.MiniActionToken = _actionToken; //scalar
            WriteHeaderToStream(0x2); //scalar
            Send();
        }

        public void Fail(string reason)
        {
            Response = CloudResponse.Failure;
            SetFailInfo(reason);
            Reply();
        }

        private void Send()
        {
            if (!_valid)
            {
                Logger.LogError("Cannot send an invalid cloud request");
                return;
            }

            _cloudNetwork.CloudSend(this);
            _sendCount++;
        }

        public void Retry(string miniToken)
        {
            //any more checks?
            if (Sendable)
            {
                //re-write minitoken
                int oldPos = _responseDataWriter.SetPosition(_miniTokenOffset);
                _responseDataWriter.Put(miniToken);
                _responseDataWriter.SetPosition(oldPos);

                Send();
            }
        }

        private void DeserializeRequest()
        {
            try
            {
                _inHeader.Deserialize(Data);
                Logger.LogInfo($"Fields length = {_inHeader.RequestFieldsLength}");
                _outHeader.MiniActionToken = _inHeader.MiniActionToken;

                _actionToken = _inHeader.CloudActionToken;
                _outHeader.CloudActionToken = _actionToken;

                //request fields
                DeserializeRequestFields();

                _valid = true;
            }
            catch (Exception ex)
            {
                Logger.LogError(ex.ToString());
                _valid = false;
            }
        }

        private void DeserializeRequestFields()
        {
            if (_inHeader == null) throw new ArgumentNullException();
            if (_inHeader.RequestFieldsLength == 0) return;

            if (_requestFields == null)
            {
                _requestFields = new Dictionary<string, CloudRequestField>();
            }
            else
            {
                _requestFields.Clear();
            }

            for (int i = 0; i < _inHeader.RequestFieldsLength; i++)
            {
                string fieldKey = Data.GetString();
                byte[] dynamicSerialization = Data.GetBytesWithLength();

                CloudRequestField requestField = new CloudRequestField(fieldKey, dynamicSerialization);
                if (!requestField.Resolve(Data)) throw new Exception();

                _requestFields[fieldKey] = requestField;
            }
        }

        public bool GetRequestField<T>(string key, out T value)
        {
            value = default;
            if (_requestFields == null) return false;

            CloudRequestField field;
            if (!_requestFields.TryGetValue(key, out field)) return false;

            value = (T)field.Value;
            return true;
        }

        private static bool ValidateField(CloudResponseField responseField)
        {
            return responseField != null
                && !string.IsNullOrEmpty(responseField.Key)
                && responseField.Value != null
                && responseField.Serialize != null;
        }

        public void AddField(CloudResponseField responseField)
        {
            if (ValidateField(responseField))
            {
                _responseFields.Add(responseField.Key, responseField);
            }
        }

        public void SetFailInfo(string info)
        {
            _responseFields["FailInfo"] = new CloudResponseFieldString("FailInfo", info);
        }
    }
}