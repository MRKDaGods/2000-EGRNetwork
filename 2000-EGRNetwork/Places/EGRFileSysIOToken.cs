using System;
using System.Collections.Generic;
using System.IO;

namespace MRK {
    public class EGRFileSysIOToken : EGRFileSysIO<EGRToken> {
        string m_TokenIndexPath => $"{m_Root}\\Index";

        public EGRFileSysIOToken(string root) : base(root) {
            if (!Directory.Exists(m_TokenIndexPath))
                Directory.CreateDirectory(m_TokenIndexPath);
        }

        protected override string GetDataFile(EGRToken obj) {
            return $"{obj.HWID}\\{EGRAccount.CalculateHash(obj.Token)}";
        }

        protected override void Write(EGRToken obj, BinaryWriter w) {
            w.Write(obj.CreationTime.Ticks);
            w.Write(obj.Token);
            w.Write(obj.UUID);
            w.Write(obj.HWID);
        }

        protected override EGRToken Read(BinaryReader r) {
            long creationTime = r.ReadInt64();
            string tk = r.ReadString();
            string uuid = r.ReadString();
            string _hwid = r.ReadString();

            return new EGRToken {
                CreationTime = new DateTime(creationTime),
                Token = tk,
                UUID = uuid,
                HWID = _hwid
            };
        }

        public EGRToken Read(string hwid, string token) {
            return Read($"{hwid}\\{EGRAccount.CalculateHash(token)}");
        }

        public void IndexToken(EGRToken token) {
            string dir = $"{m_TokenIndexPath}\\{token.UUID}";
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            try {
                m_Lock.EnterWriteLock();

                File.WriteAllText($"{dir}\\{token.HWID}", $"{token.Token}");
            }
            finally {
                m_Lock.ExitWriteLock();
            }
        }

        protected override void DeleteIndex(EGRToken obj) {
            string dir = $"{m_TokenIndexPath}\\{obj.UUID}\\{obj.HWID}";
            if (File.Exists(dir))
                File.Delete(dir);
        }

        public void ChangeIndexOwner(string oldUuid, string newUuid) {
            Directory.Move($"{m_TokenIndexPath}\\{oldUuid}", $"{m_TokenIndexPath}\\{newUuid}");
        }

        public List<EGRToken> GetTokensForUUID(string uuid) {
            List<EGRToken> tokens = new List<EGRToken>();

            try {
                m_Lock.EnterReadLock();

                foreach (string fname in Directory.EnumerateFiles($"{m_TokenIndexPath}\\{uuid}")) {
                    string hwid = Path.GetFileName(fname);
                    string token = File.ReadAllText(fname);

                    tokens.Add(new EGRToken {
                        HWID = hwid,
                        Token = token,
                        UUID = uuid
                    });
                }
            }
            finally {
                m_Lock.ExitReadLock();
            }

            return tokens;
        }
    }
}
