using System.IO;

namespace MRK {
    public struct EGRCDNResourceHeader {
        public long CreationDate { get; set; }
        public long Lifetime { get; set; }
        public long Size { get; set; }
        public byte[] Signature { get; set; }
        public long TotalFileSize { get; set; }

        public static long PhysicalSize => 0x28; //long+long+long+16(md5) = 40 bytes

        public static bool TryReadHeader(BinaryReader reader, out EGRCDNResourceHeader header) {
            try {
                long creation = reader.ReadInt64();
                long lifetime = reader.ReadInt64();
                long size = reader.ReadInt64();
                byte[] signature = reader.ReadBytes(16);

                header = new EGRCDNResourceHeader {
                    CreationDate = creation,
                    Lifetime = lifetime,
                    Size = size,
                    Signature = signature,
                    TotalFileSize = reader.BaseStream.Length
                };

                return true;
            }
            catch {
                header = default;
                return false;
            }
        }

        public static void WriteHeader(BinaryWriter writer, EGRCDNResourceHeader header) {
            writer.Write(header.CreationDate);
            writer.Write(header.Lifetime);
            writer.Write(header.Size);
            writer.Write(header.Signature);
        }
    }
}
