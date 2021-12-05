namespace MRK.Services.Accounts
{
    public enum ValidationReason
    {
        None,
        InvalidEmail = 1 << 0,
        InvalidFirstName = 1 << 1,
        InvalidLastName = 1 << 2,
        ExistsEmail = 1 << 3
    }
}
