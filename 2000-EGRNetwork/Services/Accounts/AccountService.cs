using System;

namespace MRK.Services.Accounts
{
    public class AccountService : Service
    {
        private const string PrimaryDatabaseName = "UserData.db";
        private const string AccountTable = "Account";
        private const string TokenTable = "Token";

        private readonly object _tokenLock;

        public AccountService()
        {
            _tokenLock = new object();
        }

        public override void Initialize()
        {
            SetupPrimaryDatabase(GetDatabasePath(PrimaryDatabaseName));

            //setup tables
            //Account
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

            //Token
            PrimaryDatabase.ExecuteNonQuery(
                $"CREATE TABLE IF NOT EXISTS \"{TokenTable}\"" +
                "(" +
                "\"UID\"       TEXT," +
                "\"HWID\"      TEXT," +
                "\"Token\"     TEXT," +
                "\"CreatedAt\" NUMERIC" +
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

        public LoginOperation Login(string email, string pwd, string hwid)
        {
            //invalid email
            if (!Validation.ValidateEmail(email))
            {
                return LoginOperation.Reason(false, ValidationReason.InvalidEmail);
            }

            if (!Validation.ValidateString(pwd))
            {
                return LoginOperation.Reason(false, ValidationReason.InvalidPassword);
            }

            //TODO: add proper hwid validation
            if (!Validation.ValidateString(hwid))
            {
                return LoginOperation.Reason(false, ValidationReason.InvalidHWID);
            }

            if (!EmailAssociatedWithAccount(email).Result)
            {
                return LoginOperation.Reason(false, ValidationReason.InvalidEmail);
            }

            try
            {
                using var reader = PrimaryDatabase.ExecuteReader(
                    $"SELECT * FROM {AccountTable} " +
                    "WHERE(" +
                    $"Email='{EscapeValue(email)}' " +
                    "AND " +
                    $"PwdHash='{EscapeValue(pwd)}'" +
                    ");"
                );

                //nope
                if (!reader.HasRows)
                {
                    return LoginOperation.Reason(false, ValidationReason.InvalidPassword);
                }

                reader.Read();
                string uid = reader.GetString(0);
                //token
                var opGenToken = GenerateToken(uid, hwid);
                if (!opGenToken.Finished)
                {
                    Logger.LogWarning($"Failed to generate token, uid={uid}");
                    return LoginOperation.Failed;
                }

                return new LoginOperation(true, true, opGenToken.Result);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Login, encountered an error, ex={ex}");
            }

            return LoginOperation.Failed;
        }

        public LoginOperation Login(string token, string hwid)
        {
            if (!Validation.ValidateString(token))
            {
                return LoginOperation.Reason(false, ValidationReason.InvalidToken);
            }

            if (!Validation.ValidateString(hwid))
            {
                return LoginOperation.Reason(false, ValidationReason.InvalidHWID);
            }

            try
            {
                using var reader = PrimaryDatabase.ExecuteReader(
                    $"SELECT * FROM {TokenTable} " +
                    "WHERE(" +
                    $"Token='{EscapeValue(token)}' " +
                    "AND " +
                    $"HWID='{EscapeValue(hwid)}'" +
                    ");"
                );

                //nope
                if (!reader.HasRows)
                {
                    return LoginOperation.Reason(false, ValidationReason.InvalidToken);
                }

                reader.Read();
                string uid = reader.GetString(0);
                return new LoginOperation(true, true, uid);
            }
            catch (Exception ex)
            {
                Logger.LogError($"Login, encountered an error, ex={ex}");
            }

            return LoginOperation.Failed;
        }

        private void DeleteTokenUnsafe(string uid, string hwid, string token)
        {
            PrimaryDatabase.ExecuteNonQuery(
                $"DELETE FROM {TokenTable} " +
                "WHERE(" +
                $"UID='{EscapeValue(uid)}' " +
                "AND " +
                $"HWID='{EscapeValue(hwid)}' " +
                "AND " +
                $"Token='{EscapeValue(token)}'" +
                ");"
            );
        }

        private AccountServiceOperation<string> GenerateToken(string uid, string hwid)
        {
            if (!Validation.ValidateString(uid))
            {
                return AccountServiceOperation<string>.Failed;
            }

            if (!Validation.ValidateString(hwid))
            {
                return AccountServiceOperation<string>.Failed;
            }

            try
            {
                lock (_tokenLock)
                {
                    //check whether hwid has previous token
                    using var reader = PrimaryDatabase.ExecuteReader(
                        $"SELECT * FROM {TokenTable} " +
                        "WHERE(" +
                        $"UID='{EscapeValue(uid)}' " +
                        "AND " +
                        $"HWID='{EscapeValue(hwid)}'" +
                        ");"
                    );

                    //exists, delete old one for security
                    if (reader.HasRows && reader.Read())
                    {
                        DeleteTokenUnsafe(uid, hwid, reader.GetString(2));
                    }

                    //generate token
                    Token token = Token.Generate();
                    PrimaryDatabase.ExecuteNonQuery(
                        $"INSERT INTO {TokenTable} (UID, HWID, Token, CreatedAt) " +
                        "VALUES(" +
                        $"'{EscapeValue(uid)}', " +
                        $"'{EscapeValue(hwid)}', " +
                        $"'{EscapeValue(token.Content)}', " +
                        $"'{token.Ticks}'" +
                        ");"
                    );

                    return new AccountServiceOperation<string>(true, token.Content);
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"GenerateToken, encountered an error, ex={ex}");
            }

            return AccountServiceOperation<string>.Failed;
        }
    }
}
