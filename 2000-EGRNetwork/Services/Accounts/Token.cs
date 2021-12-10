using System;

namespace MRK.Services.Accounts
{
    public struct Token
    {
        public string Content 
        { 
            get; set;
        }

        public long Ticks
        {
            get; set;
        }

        public Token(string content, long ticks)
        {
            Content = content;
            Ticks = ticks;
        }

        public static Token Generate()
        {
            //512 bit (64 bytes)
            long ticks = DateTime.UtcNow.Ticks;
            string b64Ticks = Convert.ToBase64String(BitConverter.GetBytes(ticks));
            return new Token($"{b64Ticks}{EGRUtils.GetRandomString(64 - b64Ticks.Length)}", ticks);
        }
    }
}
