using System;

namespace MRK.Networking.Packets {
    [AttributeUsage(AttributeTargets.Class)]
    public class PacketHandlerAttribute : Attribute {
        public PacketType PacketType { get; private set; }

        public PacketHandlerAttribute(PacketType type) {
            PacketType = type;
        }
    }
}
