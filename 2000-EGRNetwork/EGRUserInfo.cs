namespace MRK.Networking {
    public class EGRUserInfo {
        public string HWID { get; private set; }
        public string SessionKey { get; set; }
        public NetPeer Peer { get; private set; }

        public bool IsAuthenticated { get; set; }
        public string Username { get; set; }
        public string Nickname { get; set; }
        public string PasswordHash { get; set; }

        public int Cash { get; set; }
        public int Gold { get; set; }
        public int Exp { get; set; }

        public string DataKey { get; set; }

        public EGRUserInfo(string hwid, NetPeer peer) {
            HWID = hwid;
            Peer = peer;

            DataKey = "";

            Cash = 200009;
            Gold = 50693;
            Exp = 427891;
        }
    }
}
