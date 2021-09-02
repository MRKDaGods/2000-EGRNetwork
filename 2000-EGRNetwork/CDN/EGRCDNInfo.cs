namespace MRK.Networking {
    public class EGRCDNInfo {
        public int Port;
        public string Key;

        public override string ToString() {
            return $"CDNINFO port={Port} Key={Key}";
        }
    }
}
