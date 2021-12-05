using System;

namespace MRK.Services.Accounts
{
    public class AccountService : Service
    {
        private const string PrimaryDatabaseName = "UserData.db";
        private const string AccountTable = "Account";

        public override void Initialize()
        {
            SetupPrimaryDatabase(GetDatabasePath(PrimaryDatabaseName));

            //setup tables
            PrimaryDatabase.ExecuteNonQuery(
                $"CREATE TABLE IF NOT EXISTS \"{AccountTable}\"" +
                "(" +
                "\"UID\"	   TEXT UNIQUE," +
                "\"Email\"     TEXT," +
                "\"PwdHash\"   TEXT," +
                "\"FirstName\" TEXT," +
                "\"LastName\"  TEXT," +
                "\"Gender\"    INTEGER" +
                ");"
            );
        }

        public AccountServiceOperation<bool> AccountExists(Guid uid)
        {
            try
            {
                using var reader = PrimaryDatabase.ExecuteReader(
                    $"SELECT * FROM {AccountTable} " +
                    "WHERE " +
                    $"UID='{EscapeValue(uid.ToString())}'"
                );

                return new AccountServiceOperation<bool>(true, reader.HasRows);
            }
            catch (Exception ex)
            {
                Logger.LogError($"AccountExists encountered an error, ex={ex}");
            }

            return AccountServiceOperation<bool>.Failed;
        }

        public AccountServiceOperation<bool> EmailAssociatedWithAccount(string email)
        {
            try
            {
                using var reader = PrimaryDatabase.ExecuteReader(
                    $"SELECT * FROM {AccountTable} " +
                    "WHERE " +
                    $"Email='{EscapeValue(email)}'"
                );

                return new AccountServiceOperation<bool>(true, reader.HasRows);
            }
            catch (Exception ex)
            {
                Logger.LogError($"EmailAssociatedWithAccount encountered an error, ex={ex}");
            }

            return AccountServiceOperation<bool>.Failed;
        }

        public AccountServiceOperation<bool> CreateAccount(Account account)
        {
            //trim fields before calling

            var opEmailExists = EmailAssociatedWithAccount(account.Email);
            if (!opEmailExists.Finished)
            {
                //op failed
                return AccountServiceOperation<bool>.Failed;
            }

            if (opEmailExists.Result)
            {
                //email exists
                return AccountServiceOperation<bool>.Reason(false, ValidationReason.ExistsEmail);
            }

            ValidationReason reason;
            if (!Validation.ValidateAccountReason(ref account, out reason))
            {
                //invalid
                return AccountServiceOperation<bool>.Reason(false, reason);
            }

            account.UID = Guid.NewGuid();

            try
            {
                PrimaryDatabase.ExecuteNonQuery(
                    $"INSERT INTO {AccountTable} (UID, Email, PwdHash, FirstName, LastName, Gender) " +
                    "VALUES(" +
                    $"'{account.UID}', " +
                    $"'{EscapeValue(account.Email)}', " +
                    $"'{account.PwdHash}', " +
                    $"'{EscapeValue(account.FirstName)}', " +
                    $"'{EscapeValue(account.LastName)}', " +
                    $"'{account.Gender}'" +
                    $");"
                );

                return new AccountServiceOperation<bool>(true, true);
            }
            catch (Exception ex)
            {
                Logger.LogError($"CreateAccount, encountered an error, ex={ex}");
            }

            return AccountServiceOperation<bool>.Failed;
        }
    }
}
