using MRK.Networking.CloudActions;
using Microsoft.Data.Sqlite;

namespace MRK.Networking.CloudAPI.V1.Authentication
{
    public class Login : CloudAction
    {
        public override string Path
        {
            get
            {
                return "/2000/v1/auth/login";
            }
        }

        public override void Execute(CloudActionContext context)
        {
            string username, password, hwid;
            if (!context.GetRequestField("user", out username) 
                || !context.GetRequestField("pwd", out password) 
                || !context.GetRequestField("hwid", out hwid))
            {
                context.Response = CloudResponse.Failure;
                context.SetFailInfo("Missing field");
                context.Reply();
                return;
            }

            context.Response = CloudResponse.Success;
            context.Reply();
        }
    }
}