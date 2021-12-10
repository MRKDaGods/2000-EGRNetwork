namespace MRK.Services.Accounts
{
    public class LoginOperation : AccountServiceOperation<bool>
    {
        public string Token
        {
            get; init;
        }

        public static new LoginOperation Failed
        {
            get
            {
                return new LoginOperation(true);
            }
        }

        public LoginOperation(bool finished, bool result = false, string token = null) : base(finished, result)
        {
            Token = token;
        }

        public static new LoginOperation Reason(bool result, ValidationReason reason)
        {
            return new LoginOperation(true, result)
            {
                Extra = reason
            };
        }
    }
}
