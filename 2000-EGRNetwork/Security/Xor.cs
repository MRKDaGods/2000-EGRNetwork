using System.Linq;

namespace MRK.Security
{
    public class Xor
    {
        public static string Single(string input, char c)
        {
            return string.Concat(input.Select(ch => (char)(ch ^ c)));
        }

        public static void SingleNonAlloc(byte[] input, char c)
        {
            for (int i = 0; i < input.Length; i++)
            {
                input[i] = (byte)(input[i] ^ (byte)c);
            }
        }

        public static string Multiple(string input, byte[] key)
        {
            return string.Concat(input.Select(ch => {
                foreach (byte b in key)
                {
                    ch ^= (char)b;
                }

                return ch;
            }));
        }
    }
}
