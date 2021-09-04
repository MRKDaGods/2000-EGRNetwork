using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MRK.Networking {
    public class EGRUtils {
        static string ms_Charset = "abcdefghijklmnopqrstuvwxyzABDCEFGHIJKLMNOPQRSTUVWXYZ1234567890";

        public static string GetRandomString(int len) {
            Random random = new();
            string str = "";

            for (int i = 0; i < len; i++)
                str += ms_Charset[random.Next(0, ms_Charset.Length)];

            return str;
        }

        public static string FixInvalidString(string str) {
            return string.Join("_", str.Split(Path.GetInvalidFileNameChars()));
        }

        public static ulong GetRandomID() {
            return new Random().NextULong();
        }

        public static string CalculateRawHash(byte[] inputBytes) {
            using (MD5 md5 = MD5.Create()) {
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                    sb.Append(hashBytes[i].ToString("X2"));

                return sb.ToString();
            }
        }
    }
}
