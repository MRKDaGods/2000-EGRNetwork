using MRK.Networking;
using System;
using System.IO;
using System.Threading;

namespace MRK {
    public class EGRAccountManager {
        string m_RootPath;
        EGRFileSysIOAccount m_IOAccount;
        EGRFileSysIOToken m_IOToken;

        string m_AccountsPath => $"{m_RootPath}\\Accounts";
        string m_TokensPath => $"{m_RootPath}\\Tokens";

        public void Initialize(string root) {
            m_RootPath = root;
            if (!Directory.Exists(root))
                Directory.CreateDirectory(root);

            m_IOAccount = new EGRFileSysIOAccount(m_AccountsPath);
            m_IOToken = new EGRFileSysIOToken(m_TokensPath);
        }

        public bool RegisterAccount(string name, string email, string phash, string hwid) {
            EGRAccount acc = new EGRAccount(name, email, phash, -1, hwid);
            if (AccountExists(acc))
                return false;

            m_IOAccount.Write(acc);
            return true;
        }

        public bool LoginAccount(string email, string phash, EGRSessionUser user, out EGRAccount acc) {
            acc = null;

            EGRAccount tmp = new EGRAccount("x y", email, phash, -1, "");
            if (!AccountExists(tmp))
                return false;

            tmp = m_IOAccount.Read(tmp.UUID);
            if (tmp.Password != phash)
                return false;

            //add hwid check?
            DeleteHWIDTokenIfExists(user.HWID, tmp.UUID);

            EGRToken token = GenerateNewToken(tmp.UUID, user.HWID);
            if (token == null)
                return false;

            user.Token = token;
            acc = tmp;

            return true;
        }

        public bool LoginAccount(string token, EGRSessionUser user, out EGRAccount acc) {
            acc = null;

            EGRToken tk = m_IOToken.Read(user.HWID, token);
            if (tk == null)
                return false;

            user.Token = tk;
            acc = m_IOAccount.Read(tk.UUID);
            return true;
        }

        public bool LoginAccountDev(string name, string model, EGRSessionUser user, out EGRAccount acc) {
            acc = null;

            EGRAccount tmp = new EGRAccount($"{name} {model}", $"{user.HWID}@egr.com", EGRAccount.CalculateHash(user.HWID), -1, "");
            if (!AccountExists(tmp)) {
                //create account
                if (!RegisterAccount(tmp.FullName, tmp.Email, tmp.Password, user.HWID))
                    return false;
            }

            DeleteHWIDTokenIfExists(user.HWID, tmp.UUID);

            EGRToken token = GenerateNewToken(tmp.UUID, user.HWID);
            if (token == null)
                return false;

            tmp = m_IOAccount.Read(tmp.UUID);

            user.Token = token;
            acc = tmp;
            return true;
        }

        public bool UpdateAccountInfo(string token, string name, string email, sbyte gender, EGRSessionUser sessionUser) {
            //so sessionUser.Account should be our acc, lets compare tokens?
            if (sessionUser.Token.Token != token)
                return false;

            //Validate?

            EGRAccount acc = new EGRAccount(name, email, sessionUser.Account.Password, gender, sessionUser.HWID);
            m_IOAccount.Write(acc);
            sessionUser.AssignAccount(acc);
            return true;
        }

        void DeleteHWIDTokenIfExists(string hwid, string uuid) {
            foreach (string filename in m_IOToken.GetFiles($"\\{hwid}")) {
                EGRToken token = m_IOToken.Read($"{hwid}\\{Path.GetFileName(filename)}");
                if (token == null)
                    continue;

                if (token.UUID == uuid)
                    m_IOToken.Delete(token);
            }
        }

        public EGRToken GenerateNewToken(string uuid, string hwid) {
            EGRToken token = new EGRToken {
                CreationTime = DateTime.Now,
                Token = EGRUtils.GetRandomString(200),
                UUID = uuid,
                HWID = hwid
            };

            m_IOToken.Write(token);
            return token;
        }

        bool AccountExists(EGRAccount acc) {
            return m_IOAccount.Exists(acc);
        }
    }
}
