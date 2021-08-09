using System.IO;

namespace MRK {
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
}
