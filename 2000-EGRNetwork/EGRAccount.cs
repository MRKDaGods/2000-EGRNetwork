using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MRK {
    public class EGRToken {
        public DateTime CreationTime;
        public string Token;
        public string UUID;
        public string HWID;
    }

    public class EGRAccount {
        const string HASH_SALT = "LrtLpJL4DeGxG5Atjza46OHEiyasOOXtbROGiSbP";

        public string FirstName { get; private set; }
        public string LastName { get; private set; }
        public string FullName => $"{FirstName} {LastName}";
        public string Email { get; private set; }
        public string Password { get; private set; }
        public string HWID { get; private set; }
        public string UUID { get; private set; }

        public EGRAccount(string name, string email, string phash, string hwid) {
            string[] _names = name.Split(' ');
            FirstName = _names[0];

            string[] otherNames = new string[_names.Length - 1];
            Array.Copy(_names, 1, otherNames, 0, otherNames.Length);
            LastName = string.Join(" ", otherNames);

            Email = email;
            Password = phash;
            HWID = hwid;

            AssignUUID();
        }

        void AssignUUID() {
            UUID = CalculateHash(Email);
        }

        public static string CalculateHash(string input) {
            using (MD5 md5 = MD5.Create()) {
                byte[] inputBytes = Encoding.ASCII.GetBytes(input + HASH_SALT);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                    sb.Append(hashBytes[i].ToString("X2"));

                return sb.ToString();
            }
        }
    }
}
