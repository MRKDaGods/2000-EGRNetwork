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

        public AccountServiceOperation(bool success, T result = default) : base(success, result)
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
