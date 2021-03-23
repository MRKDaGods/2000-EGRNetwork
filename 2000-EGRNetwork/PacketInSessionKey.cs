namespace MRK.Networking.Packets {
    public class PacketInSessionKey : Packet {
        public string Key { get; private set; }

        static PacketInSessionKey() {
            RegisterIn(PacketType.XKEY, typeof(PacketInSessionKey));
        }

        public PacketInSessionKey() : base(PacketNature.In, PacketType.XKEY) {
        }

        public override void Read(PacketDataStream stream) {
            Key = stream.ReadString();
        }
    }
}