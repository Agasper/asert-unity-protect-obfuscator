using System;
using System.Collections.Generic;
using System.Text;
using dnlib.DotNet;
using dnlib.DotNet.Emit;
using log4net;

namespace AsertNet.Protection.Constants
{
    public class ConstantProtection
    {
        public bool HideIntegers { get; set; }
        public bool HideStrings { get; set; }
        public bool EncryptStrings { get; set; }

        internal static readonly ILog log = LogManager.GetLogger(typeof(ConstantProtection));

        ModuleDefMD module;
        MethodDef intMaskMethod;
        MethodDef strMaskMethod;
        int intKey;
        byte strKey;
        Random rnd;
        List<string> usedNames;

        public ConstantProtection(ModuleDefMD module)
        {
            this.usedNames = new List<string>();
            this.module = module;
            this.rnd = new Random();
        }

        void InjectMasker()
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetEntryAssembly();
            string applicationPath = System.IO.Path.GetDirectoryName(assembly.Location);
            ModuleDefMD typeModule = ModuleDefMD.Load(System.IO.Path.Combine(applicationPath, "AsertInject.dll"));
            TypeDef maskClass = typeModule.ResolveTypeDef(MDToken.ToRID(typeof(O).MetadataToken));
            typeModule.Types.Remove(maskClass);
            module.Types.Add(maskClass);

            intMaskMethod = maskClass.FindMethod("_");
            intKey = rnd.Next();
            intMaskMethod.Body.Instructions[2].OpCode = OpCodes.Ldc_I4;
            intMaskMethod.Body.Instructions[2].Operand = intKey;

            strMaskMethod = maskClass.FindMethod("_d");
            strKey = (byte)rnd.Next(2, 255);
            strMaskMethod.Body.Instructions[3].OpCode = OpCodes.Ldc_I4;
            strMaskMethod.Body.Instructions[3].Operand = (int)strKey;

            //var mm = maskClass.FindMethod("_d");
            //Console.WriteLine(mm);
            //Console.WriteLine(mm.HasBody);
            //foreach (var i in mm.Body.Instructions)
            //    Console.WriteLine(i);
            //throw new Exception("Stop");

            log.InfoFormat("Keys generated. Str: {0}, Int: {1}", strKey, intKey);
        }

        public void Protect()
        {
            log.Info("Injecting masking class...");
            InjectMasker();
            log.Info("Protecting modules...");
            foreach (TypeDef type in module.Types)
            {
                ProtectType(type);
            }
        }

        void ProtectType(TypeDef type)
        {
            foreach (MethodDef method in type.Methods)
            {
                ProcessMethod(method);
            }

            if (type.HasNestedTypes)
                foreach (TypeDef nested in type.NestedTypes)
                    ProtectType(nested);
        }

        bool CanProtect(MethodDef method)
        {
            if (method == intMaskMethod)
                return false;
            if (!method.HasBody)
                return false;
            if (method.DeclaringType.IsGlobalModuleType)
                return false;
            return true;
        }

        void ProcessMethod(MethodDef method)
        {
            if (!CanProtect(method))
                return;
            log.DebugFormat("Protecting constants in method {0}.{1}()", method.DeclaringType.FullName, method.Name);

            if (HideIntegers)
            {
                var instr = method.Body.Instructions;
                for (int i = 0; i < instr.Count; i++)
                {
                    if (instr[i].IsLdcI4())
                    {
                        ProtectIntegers(method, i);
                        i += 10;
                    }
                }
            }

            if (HideStrings || EncryptStrings)
                StringHidding(method);
        }

        void ProtectIntegers(MethodDef method, int i)
        {
            var instr = method.Body.Instructions;
            if (!instr[i].IsLdcI4()) 
                return;

            int value = instr[i].GetLdcI4Value();
            method.Body.Instructions.Insert(i + 1, Instruction.Create(OpCodes.Call, intMaskMethod));
            method.Body.Instructions[i].OpCode = OpCodes.Ldc_I4;
            method.Body.Instructions[i].Operand = value ^ intKey;
        }

        string MaskString(string s)
        {
            if (!EncryptStrings)
                return s;
            return O._e(s, strKey);
        }

        void StringHidding(MethodDef method)
        {            
            var instr = method.Body.Instructions;

            for (int i = 0; i < instr.Count; i++)
            {
                if (instr[i].OpCode == OpCodes.Ldstr)
                {
                    var original_str_value = instr[i].Operand.ToString();
                    var modulector = method.Module.GlobalType;

                    FieldDefUser original_str_value_field = new FieldDefUser(GenerateNewName(),
                        new FieldSig(method.Module.CorLibTypes.String),
                        FieldAttributes.Public | FieldAttributes.Static);
                    modulector.Fields.Add(original_str_value_field);

                    var cctor = modulector.FindOrCreateStaticConstructor();

                    //cctor.Body.Instructions.Insert(cctor.Body.Instructions.Count - 1,
                    //   OpCodes.Ldstr.ToInstruction(MaskString(original_str_value)));
                    
                    //cctor.Body.Instructions.Insert(cctor.Body.Instructions.Count - 1,
                        //OpCodes.Stsfld.ToInstruction(original_str_value_field));

					cctor.Body.Instructions.Insert(0,
					                               OpCodes.Stsfld.ToInstruction(original_str_value_field));
                    cctor.Body.Instructions.Insert(0,
                       OpCodes.Ldstr.ToInstruction(MaskString(original_str_value)));


                    instr[i].OpCode = OpCodes.Ldsfld;
                    instr[i].Operand = original_str_value_field;

                    if (EncryptStrings)
                        instr.Insert(i + 1, Instruction.Create(OpCodes.Call, strMaskMethod));

                }
            }
        }

        public string GenerateNewName()
        {
            string str = Utils.RandomStringHex(rnd, 16);
            while (usedNames.Contains(str))
            {
                str = Utils.RandomStringHex(rnd, 16);
            }
            usedNames.Add(str);
            return str;
        }

    }
}
