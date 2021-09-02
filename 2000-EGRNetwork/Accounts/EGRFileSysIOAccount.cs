using System.IO;

namespace MRK {
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
}
