using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MRK.WTE {
    public struct WTEProxyPlace {
        public string Name;
        public float GeneralMinimum;
        public float GeneralMaximum;
        public List<string> Tags;
        public string IconResource;
        public byte[] IconResourceSig;
    }
}
