namespace MRK {
    public struct EGRNetworkConfigRecord {
        readonly string m_Value;

        public string String => m_Value;
        public int Int => AsInt();
        public float Float => AsFloat();

        public EGRNetworkConfigRecord(string val) {
            m_Value = val;
        }

        int AsInt() {
            int i;
            int.TryParse(m_Value, out i);
            return i;
        }

        float AsFloat() {
            float f;
            float.TryParse(m_Value, out f);
            return f;
        }
    }
}
