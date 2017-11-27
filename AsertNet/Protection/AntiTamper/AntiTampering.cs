using System;
using System.IO;
using System.Security.Policy;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using log4net;
using UnityEngine;

namespace AsertNet.Protection.AntiTamper
{
    public class AntiTampering
    {
        internal static readonly ILog log = LogManager.GetLogger(typeof(AntiTampering));


        public static string Sign(string resultAssemblyFilename)
        {
            log.Info("Signing module...");
            var aInfo = Signing.Signer.SignAssembly(resultAssemblyFilename);
            log.Info("Module signed with token " + aInfo.HexToken);
            return aInfo.HexToken;
        }


        public static void AddCallToUnity(ModuleDefMD unityModule, string hash)
        {
            log.Info("Adding hash to Unity...");

            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetEntryAssembly();
            string applicationPath = System.IO.Path.GetDirectoryName(assembly.Location);
            ModuleDefMD typeModule = ModuleDefMD.Load(System.IO.Path.Combine(applicationPath, "AsertInject.dll"));
            TypeDef tamperClass = typeModule.ResolveTypeDef(MDToken.ToRID(typeof(UnityCertificate).MetadataToken));
            MethodDef checkerMethod = tamperClass.FindMethod("GetHash");

            typeModule.Types.Remove(tamperClass);
            unityModule.Types.Add(tamperClass);

            checkerMethod.Body.Instructions[1].Operand = hash;

            //foreach (var i in checkerMethod.Body.Instructions)
            //    Console.WriteLine(i);
        }

        public static void AddCallToModule(ModuleDefMD module)
        {
            log.Info("Adding hash checking to the assembly...");
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetEntryAssembly();
            string applicationPath = System.IO.Path.GetDirectoryName(assembly.Location);
            ModuleDefMD typeModule = ModuleDefMD.Load(System.IO.Path.Combine(applicationPath, "AsertInject.dll"));
            TypeDef tamperClass = typeModule.ResolveTypeDef(MDToken.ToRID(typeof(AsertSigning).MetadataToken));

            typeModule.Types.Remove(tamperClass);
            module.Types.Add(tamperClass);

            MethodDef cctor = module.GlobalType.FindOrCreateStaticConstructor();

            //foreach (var p in cctor.Body.Instructions)
                //Console.WriteLine(p);

            cctor.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, tamperClass.FindMethod("Expose")));


            //var t = Type.GetType("UnityEngine.UnityCertificate, UnityEngine.CoreModule, Version=0.0.0.0, Culture=neutral, PublicKeyToken=null");
            //Console.WriteLine(t);
            //Console.WriteLine(t.GetMethod("GetHash", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public));
            //var methodInfo = t.GetMethod("GetHash", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
            //string h__ = methodInfo.Invoke(null, null).ToString();

            //throw new Exception("asd");




            //foreach (TypeDef type in module.Types)
            //{
            //    if (type.IsGlobalModuleType) 
            //        continue;
                
            //    foreach (MethodDef method in type.Methods)
            //    {
            //        if (!method.HasBody) 
            //            continue;
            //        if (method.IsConstructor)
            //        {
            //            method.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Nop));
            //            method.Body.Instructions.Insert(0, Instruction.Create(OpCodes.Call, init));
            //        }
            //    }
            //}
        }
    }
}
