using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace MRK {
    public abstract class EGRFileSysIO<T> {
        protected string m_Root;
        ReaderWriterLockSlim m_Lock;

        public EGRFileSysIO(string dir) {
            m_Root = dir;
            m_Lock = new ReaderWriterLockSlim();
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
        }

        protected abstract string GetDataFile(T obj);

        protected abstract void Write(T obj, BinaryWriter w);

        protected abstract T Read(BinaryReader r);

        public void Write(T obj) {
            try {
                m_Lock.EnterWriteLock();

                string dir = $"{m_Root}\\{GetDataFile(obj)}";
                string owner = Path.GetDirectoryName(dir);
                if (!Directory.Exists(owner))
                    CreateRecursiveDir(owner);

                using (FileStream fstream = new FileStream(dir, FileMode.Create))
                using (BinaryWriter writer = new BinaryWriter(fstream)) {
                    Write(obj, writer);
                    writer.Close();
                }
            }
            catch {
            }
            finally {
                m_Lock.ExitWriteLock();
            }
        }

        public T Read(string dir) {
            dir = $"{m_Root}\\{dir}";
            if (!File.Exists(dir))
                return default;

            T obj = default;

            try {
                m_Lock.EnterReadLock();

                using (FileStream fstream = new FileStream(dir, FileMode.Open))
                using (BinaryReader reader = new BinaryReader(fstream)) {
                    obj = Read(reader);
                    reader.Close();
                }
            }
            catch {
                //hmm?
            }
            finally {
                m_Lock.ExitReadLock();
            }

            return obj;
        }

        public void Delete(T obj) {
            try {
                m_Lock.EnterWriteLock();

                string path = GetDataFile(obj);
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch {
            }
            finally {
                m_Lock.ExitWriteLock();
            }
        }
    }

    public class EGRFileSysIOAccount : EGRFileSysIO<EGRAccount> {
        public EGRFileSysIOAccount(string root) : base(root) {
        }

        protected override string GetDataFile(EGRAccount obj) {
            return $"{obj.UUID}\\egr0";
        }

        protected override void Write(EGRAccount obj, BinaryWriter w) {
            w.Write(obj.FullName);
            w.Write(obj.Email);
            w.Write(obj.Password);
            w.Write(obj.Gender);
            w.Write(obj.HWID);
            w.Write(obj.UUID);
        }

        protected override EGRAccount Read(BinaryReader r) {
            string name = r.ReadString();
            string email = r.ReadString();
            string pwd = r.ReadString();
            sbyte gender = r.ReadSByte();
            string hwid = r.ReadString();

            return new EGRAccount(name, email, pwd, gender, hwid);
        }
    }

    public class EGRFileSysIOToken : EGRFileSysIO<EGRToken> {
        public EGRFileSysIOToken(string root) : base(root) {
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
    }
}
