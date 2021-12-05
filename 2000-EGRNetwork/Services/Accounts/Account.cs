using System;

namespace MRK.Services.Accounts
{
    public struct Account
    {
        public Guid UID;
        public string Email;
        public string PwdHash;
        public string FirstName;
        public string LastName;
        public int Gender;
    }
}
