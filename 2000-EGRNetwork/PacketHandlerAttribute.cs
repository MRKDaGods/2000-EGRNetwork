using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRK.Networking.Packets {
    [AttributeUsage(AttributeTargets.Class)]
    public class PacketHandlerAttribute : Attribute {
        public PacketType PacketType { get; private set; }

        public PacketHandlerAttribute(PacketType type) {
            PacketType = type;
        }
    }
}
