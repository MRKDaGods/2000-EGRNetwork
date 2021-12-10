namespace MRK.Services.Accounts
{
    public class AccountServiceOperation<T> : ServiceOperation<T, ValidationReason>
    {
        public static new AccountServiceOperation<T> Failed
        {
            get
            {
                return new AccountServiceOperation<T>(false);
            }
        }

        public AccountServiceOperation(bool finished, T result = default) : base(finished, result)
        {
        }

        public static AccountServiceOperation<T> Reason(T result, ValidationReason reason)
        {
            return new AccountServiceOperation<T>(true, result)
            {
                Extra = reason
            };
        }
    }
}
