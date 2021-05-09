using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace MRK {
    public abstract class EGRFileSysIO<T> {
        protected string m_Root;
        protected ReaderWriterLockSlim m_Lock;
        readonly static List<string> ms_EmptyBuffer;

        static EGRFileSysIO() {
            ms_EmptyBuffer = new List<string>();
        }

        public EGRFileSysIO(string dir) {
            m_Root = dir;
            if (!Directory.Exists(dir))
                CreateRecursiveDir(dir);

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

                string path = $"{m_Root}\\{GetDataFile(obj)}";
                if (File.Exists(path))
                    File.Delete(path);
            }
            catch {
            }
            finally {
                m_Lock.ExitWriteLock();
            }
        }

        //MUST ADD \ for subdir
        public IEnumerable<string> GetFiles(string sub = "") {
            string dir = m_Root + sub;
            if (!Directory.Exists(dir))
                return ms_EmptyBuffer;

            return Directory.EnumerateFiles(m_Root + sub);
        }

        public bool Exists(T obj) {
            return File.Exists($"{m_Root}\\{GetDataFile(obj)}");
        }

        public bool DirExists(string sub) {
            return Directory.Exists($"{m_Root}\\{sub}");
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

        public new EGRAccount Read(string uuid) {
            return base.Read($"{uuid}\\egr0");
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

        public EGRToken Read(string hwid, string token) {
            return Read($"{hwid}\\{EGRAccount.CalculateHash(token)}");
        }
    }

    public class EGRFileSysIOPlace : EGRFileSysIO<EGRPlace> {
        public EGRFileSysIOPlace(string root) : base(root) {
        }

        protected override string GetDataFile(EGRPlace obj) {
            return $"{obj.CID}\\egr0";
        }

        protected override void Write(EGRPlace obj, BinaryWriter w) {
            w.Write(obj.Name);
            w.Write(obj.Type);
            w.Write(obj.CID);
            w.Write(obj.Address);
            w.Write(obj.Latitude);
            w.Write(obj.Longitude);

            w.Write(obj.Ex.Length);
            foreach (string ex in obj.Ex)
                w.Write(ex);

            w.Write(obj.Chain);

            w.Write(obj.Types.Length);
            foreach (EGRPlaceType type in obj.Types)
                w.Write((ushort)type);
        }

        protected override EGRPlace Read(BinaryReader r) {
            string name = r.ReadString();
            string type = r.ReadString();
            string _cid = r.ReadString();
            string addr = r.ReadString();
            double lat = r.ReadDouble();
            double lng = r.ReadDouble();

            int exLen = r.ReadInt32();
            string[] ex = new string[exLen];
            for (int i = 0; i < exLen; i++)
                ex[i] = r.ReadString();

            ulong chain = r.EOF() ? 0UL : r.ReadUInt64();

            EGRPlace place = new EGRPlace(name, type, _cid, addr, lat, lng, ex, chain);
            if (!r.EOF()) {
                int typeLen = r.ReadInt32();
                EGRPlaceType[] types = new EGRPlaceType[typeLen];
                for (int i = 0; i < typeLen; i++)
                    types[i] = (EGRPlaceType)r.ReadUInt16();

                place.SetTypes(types);
            }

            return place;
        }

        public new EGRPlace Read(string cid) {
            return base.Read($"{cid}\\egr0");
        }
    }

    public class EGRFileSysIOPlaceChain : EGRFileSysIO<EGRPlaceChain> {
        string m_ChainIndexPath => $"{m_Root}\\Index";
        
        public EGRFileSysIOPlaceChain(string root) : base(root) {
            if (!Directory.Exists(m_ChainIndexPath))
                Directory.CreateDirectory(m_ChainIndexPath);
        }

        protected override string GetDataFile(EGRPlaceChain obj) {
            return $"{obj.ID}\\egr0";
        }

        protected override EGRPlaceChain Read(BinaryReader r) {
            ulong id = r.ReadUInt64();
            string name = r.ReadString();

            int mlen = r.ReadInt32();
            string[] matches = new string[mlen];
            for (int i = 0; i < mlen; i++)
                matches[i] = r.ReadString();

            return new EGRPlaceChain(id, name, matches);
        }

        public new EGRPlaceChain Read(string name) {
            return base.Read($"{GetChainIDFromName(name)}\\egr0");
        }

        protected override void Write(EGRPlaceChain obj, BinaryWriter w) {
            w.Write(obj.ID);
            w.Write(obj.Name);

            w.Write(obj.Matches.Length);
            foreach (string match in obj.Matches)
                w.Write(match);
        }

        public void IndexChain(EGRPlaceChain chain) {
            try {
                m_Lock.EnterWriteLock();

                File.WriteAllText($"{m_ChainIndexPath}\\{chain.Name}", $"{chain.ID}");
            }
            finally {
                m_Lock.ExitWriteLock();
            }
        }

        public ulong GetChainIDFromName(string name) {
            string path = $"{m_ChainIndexPath}\\{name}";
            if (!File.Exists(path))
                return 0;

            return ulong.Parse(File.ReadAllText(path));
        }
    }

    public class EGRFileSysIOTileID : EGRFileSysIO<EGRTileID> {
        public EGRFileSysIOTileID(string root) : base(root) {
        }

        protected override string GetDataFile(EGRTileID obj) {
            return $"{obj.Z}\\{obj.X}\\{obj.Y}\\tile.png";
        }

        protected override EGRTileID Read(BinaryReader r) {
            byte[] data = new byte[r.BaseStream.Length];
            for (long i = 0; i < r.BaseStream.Length; i++)
                data[i] = r.ReadByte();

            return new EGRTileID {
                Data = data
            };
        }

        protected override void Write(EGRTileID obj, BinaryWriter w) {
            foreach (byte b in obj.Data)
                w.Write(b);
        }
    }
}
