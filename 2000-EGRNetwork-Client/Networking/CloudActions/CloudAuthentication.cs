using MRK.Networking.Internal.Utils;
using MRK.Security;
using System.Text;

namespace MRK.Networking.CloudActions
{
    public class CloudAuthentication
    {
        private readonly string _cloudKey;
        private readonly string _hwid;

        public CloudAuthentication(string cloudKey, string hwid)
        {
            _cloudKey = cloudKey;
            _hwid = hwid;
        }

        public void AuthenticateDataStream(ref NetDataWriter data)
        {
            NetDataWriter secureWriter = new NetDataWriter();
            secureWriter.Put(_cloudKey);

            //xor it with key[0] ^ key[^1]
            string rawHwid = $"egr{_hwid}";
            string hwid = Xor.Single(rawHwid, (char)(_cloudKey[0] ^ _cloudKey[^1]));
            secureWriter.Put(hwid);

            string token = EGRUtils.GetRandomString(32);
            token = token.Insert(0, "egr");

            byte[] addr = Encoding.UTF8.GetBytes(rawHwid);
            //xor addr with hwid.length
            Xor.SingleNonAlloc(addr, (char)rawHwid.Length);

            //xor with hwid[0] ^ hwid[^1]
            Xor.SingleNonAlloc(addr, (char)(rawHwid[0] ^ rawHwid[^1]));
            string outToken = Xor.Multiple(token, addr);
            secureWriter.Put(outToken);

            secureWriter.Put(data.Data);
            data = secureWriter;
        }
    }
}
