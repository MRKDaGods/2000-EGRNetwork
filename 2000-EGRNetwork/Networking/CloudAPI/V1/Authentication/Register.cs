using MRK.Networking.CloudActions;
using MRK.Services.Accounts;

namespace MRK.Networking.CloudAPI.V1.Authentication
{
    public class Register : CloudAction
    {
        public override string Path
        {
            get
            {
                return "/2000/v1/auth/register";
            }
        }

        public override void Execute(CloudActionContext context)
        {
            string email, pwdHash, firstName, lastName;
            if (!context.GetRequestField("email", out email)
                || !context.GetRequestField("pwd", out pwdHash)
                || !context.GetRequestField("firstName", out firstName)
                || !context.GetRequestField("lastName", out lastName))
            {
                context.Response = CloudResponse.Failure;
                context.SetFailInfo("Missing field");
                context.Reply();
                return;
            }

            Account account = new Account
            {
                Email = email,
                PwdHash = pwdHash,
                FirstName = firstName.Trim(),
                LastName = lastName.Trim(),
                Gender = 0
            };

            AccountService accountService = EGR.Server.GetService<AccountService>();
            var op = accountService.CreateAccount(account);

            if (!op.Finished)
            {
                context.Response = CloudResponse.Failure;
                context.SetFailInfo("Op failed");
                context.Reply();
                return;
            }

            if (!op.Result)
            {
                context.Response = CloudResponse.Failure;
                context.SetFailInfo($"{op.Extra}");
                context.Reply();
                return;
            }

            context.Response = CloudResponse.Success;
            context.Reply();
        }
    }
}