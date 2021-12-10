using MRK.Networking.CloudActions;
using MRK.Services.Accounts;

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
            string method;
            if (!context.GetRequestField("method", out method))
            {
                context.Fail("Method not specified");
                return;
            }

            bool opSuccess = false;
            switch (method)
            {
                case "manual":
                    ManualLogin(context, ref opSuccess);
                    break;

                case "auto":
                    AutomaticLogin(context, ref opSuccess);
                    break;

                default:
                    context.Fail("Unknown method");
                    return;
            }

            if (opSuccess)
            {
                context.Response = CloudResponse.Success;
                context.Reply();
            }
        }

        private static void ManualLogin(CloudActionContext context, ref bool opSuccess)
        {
            string username, password, hwid;
            if (!context.GetRequestField("user", out username)
                || !context.GetRequestField("pwd", out password)
                || !context.GetRequestField("hwid", out hwid))
            {
                context.Fail("Missing field");
                return;
            }

            AccountService accountService = EGR.Server.GetService<AccountService>();
            var op = accountService.Login(username, password, hwid);

            if (!op.Finished)
            {
                context.Fail("Operation failed");
                return;
            }

            if (!op.Result)
            {
                context.Fail(op.Extra.ToString());
                return;
            }

            context.AddField(new CloudResponseFieldString("token", op.Token));
            opSuccess = true;
        }

        private static void AutomaticLogin(CloudActionContext context, ref bool opSuccess)
        {
            string token, hwid;
            if (!context.GetRequestField("token", out token)
                || !context.GetRequestField("hwid", out hwid))
            {
                context.Fail("Missing field");
                return;
            }

            AccountService accountService = EGR.Server.GetService<AccountService>();
            var op = accountService.Login(token, hwid);

            if (!op.Finished)
            {
                context.Fail("Operation failed");
                return;
            }

            if (!op.Result)
            {
                context.Fail(op.Extra.ToString());
                return;
            }

            context.AddField(new CloudResponseFieldString("uid", op.Token)); //uid is stored in op.Token
            opSuccess = true;
        }
    }
}