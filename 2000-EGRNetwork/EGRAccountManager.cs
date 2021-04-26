using MRK.Networking;
using System;
using System.IO;
using System.Threading;

namespace MRK {
    public class EGRAccountManager {
        string m_RootPath;
        ReaderWriterLock m_RWLock;
        EGRFileSysIOAccount m_IOAccount;
        EGRFileSysIOToken m_IOToken;

        string m_AccountsPath => $"{m_RootPath}\\Accounts";
        string m_TokensPath => $"{m_RootPath}\\Tokens";

        public EGRAccountManager() {
            m_RWLock = new ReaderWriterLock();
        }

        public void Initialize(string root) {
            m_RootPath = root;

            m_IOAccount = new EGRFileSysIOAccount(m_AccountsPath);
            m_IOToken = new EGRFileSysIOToken(m_TokensPath);

            /* if (!Directory.Exists(root)) {
                CreateRecursiveDir(root);
                CreateRecursiveDir(m_AccountsPath);
                CreateRecursiveDir(m_TokensPath);
            } */
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

            EGRToken tk = GetToken(user.HWID, token);
            if (tk == null)
                return false;

            user.Token = tk;
            acc = ReadAccountInfo(tk.UUID);

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

            tmp = ReadAccountInfo(tmp.UUID);

            user.Token = token;
            acc = tmp;

            return true;
        }

        EGRAccount ReadAccountInfo(string uuid) {
            try {
                m_RWLock.AcquireReaderLock(10000);

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
            finally {
                m_RWLock.ReleaseReaderLock();
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

            try {
                m_RWLock.AcquireReaderLock(10000);

                using (FileStream fstream = new FileStream(fpath, FileMode.Open))
                using (BinaryReader reader = new BinaryReader(fstream)) {
                    long creationTime = reader.ReadInt64();
                    string tk = reader.ReadString();
                    string uuid = reader.ReadString();
                    string _hwid = reader.ReadString();

                    reader.Close();

                    return new EGRToken {
                        CreationTime = new DateTime(creationTime),
                        Token = tk,
                        UUID = uuid,
                        HWID = _hwid
                    };
                }
            }
            finally {
                m_RWLock.ReleaseReaderLock();
            }
        }

        void DeleteHWIDTokenIfExists(string hwid, string uuid) {
            string path = $"{m_TokensPath}\\{hwid}";
            if (!Directory.Exists(path))
                return;
            try {
                m_RWLock.AcquireReaderLock(10000);

                foreach (string filename in Directory.EnumerateFiles(path)) {
                    using (FileStream fstream = new FileStream(filename, FileMode.Open))
                    using (BinaryReader reader = new BinaryReader(fstream)) {
                        long creationTime = reader.ReadInt64();
                        string tk = reader.ReadString();
                        string _uuid = reader.ReadString();

                        reader.Close();

                        if (_uuid == uuid)
                            DeleteToken(hwid, tk);
                    }
                }
            }
            finally {
                m_RWLock.ReleaseReaderLock();
            }
        }

        void DeleteToken(string hwid, string token) {
            string path = $"{m_TokensPath}\\{hwid}";
            if (!Directory.Exists(path))
                return;

            string fpath = $"{path}\\{EGRAccount.CalculateHash(token)}";
            if (!File.Exists(fpath))
                return;

            File.Delete(fpath);
        }

        public EGRToken GenerateNewToken(string uuid, string hwid) {
            string path = $"{m_TokensPath}\\{hwid}";
            CreateRecursiveDir(path);

            string tk = EGRUtils.GetRandomString(200);

            string fpath = $"{path}\\{EGRAccount.CalculateHash(tk)}";
            /* if (File.Exists(fpath)) {
                File.Delete(fpath);
            } */

            try {
                m_RWLock.AcquireWriterLock(10000);

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
            finally {
                m_RWLock.ReleaseWriterLock();
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
            try {
                m_RWLock.AcquireWriterLock(10000);

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
            finally {
                m_RWLock.ReleaseWriterLock();
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
