namespace MRK.Services.Accounts
{
    public enum ValidationReason
    {
        None,

        InvalidEmail = 1 << 0,
        InvalidFirstName = 1 << 1,
        InvalidLastName = 1 << 2,
        InvalidPassword = 1 << 3,
        InvalidHWID = 1 << 4,
        InvalidToken = 1 << 5,
        InvalidMax = InvalidToken,

        ExistsEmail = InvalidMax << 1
    }
}
