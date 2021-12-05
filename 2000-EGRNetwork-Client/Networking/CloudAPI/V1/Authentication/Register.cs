using MRK.Networking.CloudActions;

namespace MRK.Networking.CloudAPI.V1.Authentication
{
    public class Register : CloudAction
    {
        private string _email;
        private string _pwdHash;
        private string _firstName;
        private string _lastName;

        public override string Path
        {
            get { return "/2000/v1/auth/register"; }
        }

        public Register(string email, string pwdHash, string firstName, string lastName, CloudActionContext context) : base(context)
        {
            _email = email;
            _pwdHash = pwdHash;
            _firstName = firstName;
            _lastName = lastName;
        }

        protected override void OnRequestSend()
        {
            Context.AddField(new CloudRequestFieldString("email", _email));
            Context.AddField(new CloudRequestFieldString("pwd", _pwdHash));
            Context.AddField(new CloudRequestFieldString("firstName", _firstName));
            Context.AddField(new CloudRequestFieldString("lastName", _lastName));
        }
    }
}