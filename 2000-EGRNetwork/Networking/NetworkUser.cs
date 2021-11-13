namespace MRK.Networking
{
    public class NetworkUser
    {
        public string HWID
        {
            get; init;
        }

        public virtual bool IsServerUser
        {
            get { return true; }
        }

        public NetworkUser(string hwid)
        {
            HWID = hwid;
        }
    }
}
