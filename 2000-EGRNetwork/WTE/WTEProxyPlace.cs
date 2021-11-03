using System.Collections.Generic;

namespace MRK.WTE {
    public struct WTEProxyPlace {
        public string Name;
        public ulong CID;
        public float GeneralMinimum;
        public float GeneralMaximum;
        public List<string> Tags;
        public string IconResource;
        public byte[] IconResourceSig;
    }
}
