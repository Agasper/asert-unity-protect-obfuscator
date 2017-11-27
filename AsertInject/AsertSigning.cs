using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using System.Security.Policy;

public static class AsertSigning
{
    public static void Expose()
    {
        var hash = GetHash();
        var t = GetClass();
        var methodInfo = GetMethod(t);
        string h__ = methodInfo.Invoke(null, null).ToString();
        if (h__ != hash)
            throw new Exception();
    }

    public static string GetHash()
    {
        var a = System.Reflection.Assembly.GetExecutingAssembly();
        return BitConverter.ToString(a.GetName().GetPublicKeyToken()).Replace("-", string.Empty);
    }

    public static Type GetClass()
    {
        return Type.GetType("UnityEngine.UnityCertificate, UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
    }

    public static MethodInfo GetMethod(Type t)
    {
        return t.GetMethod("GetHash", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
    }

}
