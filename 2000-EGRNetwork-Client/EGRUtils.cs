using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace MRK
{
    public class EGRUtils
    {
        static string ms_Charset = "abcdefghijklmnopqrstuvwxyzABDCEFGHIJKLMNOPQRSTUVWXYZ1234567890";

        public static string GetRandomString(int len)
        {
            Random random = new();
            string str = "";

            for (int i = 0; i < len; i++)
                str += ms_Charset[random.Next(0, ms_Charset.Length)];

            return str;
        }

        public static string FixInvalidString(string str)
        {
            return string.Join("_", str.Split(Path.GetInvalidFileNameChars()));
        }


        public static string CalculateRawHash(byte[] inputBytes)
        {
            using (MD5 md5 = MD5.Create())
            {
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                    sb.Append(hashBytes[i].ToString("X2"));

                return sb.ToString();
            }
        }

        public static void Iterator<T>(List<T> list, Action<T, Reference<bool>> iter)
        {
            if (iter == null)
                return;

            Reference<bool> exit = new();
            foreach (T item in list)
            {
                iter(item, exit);

                if (exit.Value)
                    break;
            }
        }
    }
}
