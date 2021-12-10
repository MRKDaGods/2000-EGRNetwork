using MRK.Networking.CloudActions;

namespace MRK.Networking.CloudAPI.V1.Authentication
{
    public class Login : CloudAction
    {
        private string _username;
        private string _password;
        private string _hwid;
        private string _token;
        private readonly string _method;
        private readonly Action _serializeData;

        public override string Path
        {
            get { return "/2000/v1/auth/login"; }
        }

        public string Token
        {
            get; private set;
        }

        public string UID
        {
            get; private set;
        }

        public Login(string username, string password, string hwid, CloudActionContext context) : base(context)
        {
            _username = username;
            _password = password;
            _hwid = hwid;

            _method = "manual";
            _serializeData = SerializeManual;
        }

        public Login(string token, string hwid, CloudActionContext context) : base(context)
        {
            _token = token;
            _hwid = hwid;

            _method = "auto";
            _serializeData = SerializeAuto;
        }

        private void SerializeManual()
        {
            Context.AddField(new CloudRequestFieldString("user", _username));
            Context.AddField(new CloudRequestFieldString("pwd", _password));
        }

        private void SerializeAuto()
        {
            Context.AddField(new CloudRequestFieldString("token", _token));
        }

        protected override void OnRequestSend()
        {
            Context.AddField(new CloudRequestFieldString("method", _method));
            Context.AddField(new CloudRequestFieldString("hwid", _hwid));

            //serialize extra data depending on method
            _serializeData();
        }

        protected override void OnResponseReceived()
        {
            if (Response == CloudResponse.Success)
            {
                if (_method == "manual")
                {
                    string token;
                    Context.GetResponseField("token", out token);
                    Token = token;
                }
                else
                {
                    string uid;
                    Context.GetResponseField("uid", out uid);
                    UID = uid;
                }
            }
        }
    }
}
