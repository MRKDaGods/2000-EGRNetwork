namespace MRK {
    public class EGRCDNResource {
        public EGRCDNResourceHeader Header { get; private set; }
        public byte[] Data { get; private set; }

        public EGRCDNResource(EGRCDNResourceHeader header, byte[] data) {
            Header = header;
            Data = data;
        }
    }
}
