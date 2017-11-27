using System;
using System.Linq;
using System.Text;

namespace AsertNet.Protection
{
    public class Utils
    {

        public static readonly char[] unicodeCharset = new char[] { }
                .Concat(Enumerable.Range(0x200b, 5).Select(ord => (char)ord))
                .Concat(Enumerable.Range(0x2029, 6).Select(ord => (char)ord))
                .Concat(Enumerable.Range(0x206a, 6).Select(ord => (char)ord))
                .Except(new[] { '\u2029' })
                .ToArray();

        public static readonly string hexCharset = "abcdef1234567890";


        public static string RandomStringUnicode(Random rnd, int length)
        {
            StringBuilder str = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                char c = unicodeCharset[rnd.Next(0, unicodeCharset.Length)];
                str.Append(c);
            }
            return str.ToString();
        }

        public static string RandomStringHex(Random rnd, int length)
        {
            StringBuilder str = new StringBuilder(length);
            for (int i = 0; i < length; i++)
            {
                char c = hexCharset[rnd.Next(0, hexCharset.Length)];
                str.Append(c);
            }
            return str.ToString();
        }


    }
}
