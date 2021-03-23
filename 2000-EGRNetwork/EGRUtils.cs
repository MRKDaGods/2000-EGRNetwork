using System;

namespace MRK.Networking {
    public class EGRUtils {
        static string ms_Charset = "abcdefghijklmnopqrstuvwxyzABDCEFGHIJKLMNOPQRSTUVWXYZ1234567890";

        public static string GetRandomString(int len) {
            Random random = new Random();
            string str = "";

            for (int i = 0; i < len; i++)
                str += ms_Charset[random.Next(0, ms_Charset.Length)];

            return str;
        }
    }
}
