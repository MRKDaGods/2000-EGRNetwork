using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using MRK.Networking;

namespace MRK {
    public class EGRAccountManager {
        string m_RootPath;

        string m_AccountsPath => $"{m_RootPath}\\Accounts";
        string m_TokensPath => $"{m_RootPath}\\Tokens";

        public void Initialize(string root) {
            m_RootPath = root;

            if (!Directory.Exists(root)) {
                CreateRecursiveDir(root);
                CreateRecursiveDir(m_AccountsPath);
                CreateRecursiveDir(m_TokensPath);
            }
        }

        public bool RegisterAccount(string name, string email, string phash, string hwid) {
            EGRAccount acc = new EGRAccount(name, email, phash, -1, hwid);
            if (AccountExists(acc))
                return false;

            //write acc info
            Directory.CreateDirectory(GetAccountPath(acc));
            WriteAccountInfo(acc);

            return true;
        }

        public bool LoginAccount(string email, string phash, EGRSessionUser user, out EGRAccount acc) {
            acc = null;

            EGRAccount tmp = new EGRAccount("x y", email, phash, -1, "");
            if (!AccountExists(tmp))
                return false;

            tmp = ReadAccountInfo(tmp.UUID);
            if (tmp.Password != phash)
                return false;

            //add hwid check?

            EGRToken token = GenerateNewToken(tmp.UUID, user.HWID);
            if (token == null)
                return false;

            user.Token = token;
            acc = tmp;

            return true;
        }

        public bool LoginAccount(string token, EGRSessionUser user, out EGRAccount acc) {
            acc = null;

            EGRToken tk = GetToken(user.HWID, token);
            if (tk == null)
                return false;

            user.Token = tk;
            acc = ReadAccountInfo(tk.UUID);

            return true;
        }

        public bool LoginAccount(EGRSessionUser user, out EGRAccount acc) {
            acc = null;

            EGRAccount tmp = new EGRAccount("E G R", $"{user.HWID}@egr.com", EGRAccount.CalculateHash(user.HWID), -1, "");
            if (!AccountExists(tmp)) {
                //create account
                if (!RegisterAccount(tmp.FullName, tmp.Email, tmp.Password, user.HWID))
                    return false;
            }

            EGRToken token = GenerateNewToken(tmp.UUID, user.HWID);
            if (token == null)
                return false;

            user.Token = token;
            acc = tmp;

            return true;
        }

        EGRAccount ReadAccountInfo(string uuid) {
            using (FileStream fstream = new FileStream($"{GetAccountPath(uuid)}\\egr0", FileMode.Open))
            using (BinaryReader reader = new BinaryReader(fstream)) {
                string name = reader.ReadString();
                string email = reader.ReadString();
                string pwd = reader.ReadString();
                sbyte gender = reader.ReadSByte();
                string hwid = reader.ReadString();
                reader.Close();

                return new EGRAccount(name, email, pwd, gender, hwid);
            }
        }

        public bool UpdateAccountInfo(string token, string name, string email, sbyte gender, EGRSessionUser sessionUser) {
            //so sessionUser.Account should be our acc, lets compare tokens?
            if (sessionUser.Token.Token != token)
                return false;

            //Validate?

            EGRAccount acc = new EGRAccount(name, email, sessionUser.Account.Password, gender, sessionUser.HWID);
            WriteAccountInfo(acc);
            sessionUser.AssignAccount(acc);
            return true;
        }

        public EGRToken GetToken(string hwid, string token) {
            string path = $"{m_TokensPath}\\{hwid}";
            if (!Directory.Exists(path))
                return null;

            string fpath = $"{path}\\{EGRAccount.CalculateHash(token)}";
            if (!File.Exists(fpath))
                return null;

            using (FileStream fstream = new FileStream(fpath, FileMode.Open))
            using (BinaryReader reader = new BinaryReader(fstream)) {
                long creationTime = reader.ReadInt64();
                string tk = reader.ReadString();
                string uuid = reader.ReadString();
                string _hwid = reader.ReadString();

                return new EGRToken {
                    CreationTime = new DateTime(creationTime),
                    Token = tk,
                    UUID = uuid,
                    HWID = _hwid
                };
            }
        }

        public EGRToken GenerateNewToken(string uuid, string hwid) {
            string path = $"{m_TokensPath}\\{hwid}";
            CreateRecursiveDir(path);

            string tk = EGRUtils.GetRandomString(200);

            string fpath = $"{path}\\{EGRAccount.CalculateHash(tk)}";
            /* if (File.Exists(fpath)) {
                File.Delete(fpath);
            } */

            using (FileStream fstream = new FileStream(fpath, FileMode.Create))
            using (BinaryWriter writer = new BinaryWriter(fstream)) {
                EGRToken token = new EGRToken {
                    CreationTime = DateTime.Now,
                    Token = tk,
                    UUID = uuid,
                    HWID = hwid
                };

                writer.Write(token.CreationTime.Ticks);
                writer.Write(token.Token);
                writer.Write(token.UUID);
                writer.Write(token.HWID);

                return token;
            }
        }

        string GetAccountPath(string uuid) {
            return $"{m_AccountsPath}\\{uuid}";
        }

        string GetAccountPath(EGRAccount acc) {
            return GetAccountPath(acc.UUID);
        }

        bool AccountExists(EGRAccount acc) {
            return Directory.Exists(GetAccountPath(acc));
        }

        //UNSAFE
        void WriteAccountInfo(EGRAccount acc) {
            using (FileStream fstream = new FileStream($"{GetAccountPath(acc)}\\egr0", FileMode.Create))
            using (BinaryWriter writer = new BinaryWriter(fstream)) {
                writer.Write(acc.FullName);
                writer.Write(acc.Email);
                writer.Write(acc.Password);
                writer.Write(acc.Gender);
                writer.Write(acc.HWID);
                writer.Write(acc.UUID);

                writer.Close();
            }
        }

        void CreateRecursiveDir(string dir) {
            int start = 0;
            while (start < dir.Length) {
                int sepIdx = dir.IndexOf('\\', start);
                if (sepIdx == -1)
                    sepIdx = dir.Length - 1;

                string _dir = dir.Substring(0, sepIdx + 1);
                if (!Directory.Exists(_dir))
                    Directory.CreateDirectory(_dir);

                start = sepIdx + 1;
            }

            Console.WriteLine($"Creating dir {dir}");
        }
    }
}
