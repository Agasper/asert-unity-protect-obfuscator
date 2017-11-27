using System;
using System.Linq;
using System.Text;

public static class O
{
    public static int _(int i)
    {
        return i ^ 0;
    }

    public static string _e(string t, byte kk)
    {
        byte[] k = new byte[256];
        int ll = k.Length;
        for (int j = 0; j < ll; j++)
            if (j == 0)
                k[j] ^= 127;
            else
                k[j] ^= kk;
        int i = 0;
        int l = k.Length;
        byte[] octets = Encoding.UTF8
                        .GetBytes(t)
                        .Select(b => (byte)(b ^ k[(++i) % l]))
                        .ToArray();
        return Convert.ToBase64String(octets);
    }

    public static string _d(string t)
    {
        byte kk = 196;
        byte[] k = new byte[256];
        int ll = k.Length;
        for (int j = 0; j < ll; j++)
            if (j == 0)
                k[j] ^= 127;
            else
                k[j] ^= kk;
        int l = k.Length;
        int i = 0;
        byte[] octets = Convert
                        .FromBase64String(t)
                        .Select(b => (byte)(b ^ k[(++i) % l]))
                        .ToArray();
        string plainText = Encoding.UTF8.GetString(octets);
        return plainText;
    }




}