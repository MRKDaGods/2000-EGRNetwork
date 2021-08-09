using System.IO;

namespace MRK {
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
}
