namespace MRK.Networking.Packets {
    public class PacketOutSessionKey : Packet {
        public string HWID { get; private set; }

        static PacketOutSessionKey() {
            RegisterOut(PacketType.XKEY, typeof(PacketOutSessionKey));
        }

        public PacketOutSessionKey() : base(PacketNature.Out, PacketType.XKEY) {
        }

        public override void Write(PacketDataStream stream) {
            stream.WriteString(HWID);
        }
    }
}