using System.Text.RegularExpressions;

namespace MRK.Services.Accounts
{
    public class Validation
    {
        private const int EmailMinLength = 7;
        private const int EmailMaxLength = 254;
        private const int NameMinLength = 2;
        private const int NameMaxLength = 35;

        private static readonly string EmailRegex;

        static Validation()
        {
            EmailRegex = @"^((([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]" +
                @"|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+(\.([a-z]|\d|[!#\$%&'\*\+\-\/=\?\^_`{\|}~]" +
                @"|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])+)*)|((\x22)((((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(([\x01-\x08\x0b\x0c\x0e-\x1f\x7f]" +
                @"|\x21|[\x23-\x5b]|[\x5d-\x7e]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(\\([\x01-\x09\x0b\x0c\x0d-\x7f]" +
                @"|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF]))))*(((\x20|\x09)*(\x0d\x0a))?(\x20|\x09)+)?(\x22)))@((([a-z]|\d" +
                @"|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~|" +
                @"[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|\d|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.)+(([a-z]" +
                @"|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])|(([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])([a-z]|\d|-|\.|_|~" +
                @"|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])*([a-z]|[\u00A0-\uD7FF\uF900-\uFDCF\uFDF0-\uFFEF])))\.?$";
        }

        public static bool ValidateString(string str)
        {
            return !string.IsNullOrEmpty(str) && !string.IsNullOrWhiteSpace(str);
        }

        public static bool ValidateEmail(string email)
        {
            if (!ValidateString(email)) return false;

            return email.Length >= EmailMinLength
                && email.Length <= EmailMaxLength
                && Regex.IsMatch(email, EmailRegex, RegexOptions.IgnoreCase);
        }

        public static bool ValidateName(string name)
        {
            if (!ValidateString(name)) return false;

            //TODO: add more sname validation

            return name.Length >= NameMinLength
                && name.Length <= NameMaxLength;
        }

        public static bool ValidateAccountReason(ref Account account, out ValidationReason reason)
        {
            reason = ValidationReason.None;

            if (!ValidateEmail(account.Email))
            {
                reason |= ValidationReason.InvalidEmail;
            }

            if (!ValidateName(account.FirstName))
            {
                reason |= ValidationReason.InvalidFirstName;
            }

            if (!ValidateName(account.LastName))
            {
                reason |= ValidationReason.InvalidLastName;
            }

            return reason == ValidationReason.None;
        }

        public static bool ValidateAccount(ref Account account)
        {
            return ValidateAccountReason(ref account, out _);
        }
    }
}
