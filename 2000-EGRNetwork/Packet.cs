﻿using System.Collections.Generic;
using System;

namespace MRK.Networking.Packets {
    public enum PacketNature : byte {
        None,
        In,
        Out
    }

    public enum PacketType : ushort {
        None = 0x00,

        //xor key
        XKEY = 0x01,
        //device info
        DEVINFO = 0x02,

        //register account
        REGACC = 0x10,
        //login
        LGNACC = 0x11,
        //login with token
        LGNACCTK = 0x12,
        //login with device
        LGNACCDEV = 0x13,
        //logout
        LGNOUT = 0x14,

        //standard response
        STDRSP = 0x20,


        MAX
    }

    public class Packet {
        readonly static Dictionary<PacketType, Type>[] ms_PacketTable;

        public PacketNature PacketNature { get; private set; }
        public PacketType PacketType { get; private set; }
        
        static Packet() {
            ms_PacketTable = new Dictionary<PacketType, Type>[2] {
                new Dictionary<PacketType, Type>(),
                new Dictionary<PacketType, Type>()
            };
        }

        public Packet(PacketNature nature, PacketType type) {
            PacketNature = nature;
            PacketType = type;
        }

        //For out packets
        public virtual void Write(PacketDataStream stream) {
        }

        public virtual void Read(PacketDataStream stream) {
        }

        protected static void RegisterIn(PacketType ptype, Type type) {
            ms_PacketTable[0][ptype] = type;
        }

        protected static void RegisterOut(PacketType ptype, Type type) {
            ms_PacketTable[1][ptype] = type;
        }

        public static Packet CreatePacketInstance(PacketNature nature, PacketType type) {
            Type _type = ms_PacketTable[((int)nature) - 1][type];
            if (_type == null)
                return null;

            return (Packet)Activator.CreateInstance(_type);
        }
    }
}