using MRK.Networking.CloudActions;

namespace MRK.Networking.CloudAPI.V1.Authentication
{
    public class Login : CloudAction
    {
        private string _username;
        private string _password;
        private string _hwid;

        public override string Path
        {
            get { return "/2000/v1/auth/login"; }
        }

        public Login(string username, string password, string hwid, CloudActionContext context) : base(context)
        {
            _username = username;
            _password = password;
            _hwid = hwid;
        }

        protected override void OnRequestSend()
        {
            Context.AddField(new CloudRequestFieldString("user", _username));
            Context.AddField(new CloudRequestFieldString("pwd", _password));
            Context.AddField(new CloudRequestFieldString("hwid", _hwid));
        }
    }
}
