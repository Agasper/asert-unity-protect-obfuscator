using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using dnlib.DotNet;
using log4net;

namespace AsertNet.Protection.Renaming
{
    public class Renamer
    {
        public bool StrongRenaming { get; set; }
        public bool RenameTypes { get; set; }
        public bool RenameMethods { get; set; }
        public bool RenameMethodParams { get; set; }
        public bool RenameFields { get; set; }
        public bool RenameProps { get; set; }
        public bool RenameEvents { get; set; }

        internal static readonly ILog log = LogManager.GetLogger(typeof(Renamer));

        
        static readonly string[] lockedMethods = new string[] { 
            "Awake",
            "Start",
            "Update",
            "LateUpdate",
            "FixedUpdate",
            "Reset",
            "Reset",
            "OnMouseEnter",
            "OnMouseOver",
            "OnMouseExit",
            "OnMouseDown",
            "OnMouseUp",
            "OnMouseDrag",
            "OnTriggerEnter",
            "OnTriggerExit",
            "OnTriggerStay",
            "OnCollisionEnter",
            "OnCollisionExit",
            "OnCollisionStay",
            "OnControllerColliderHit",
            "OnJointBreak",
            "OnParticleCollision",
            "OnBecameVisible",
            "OnBecameInvisible",
            "OnLevelWasLoaded",
            "OnEnable",
            "OnDisable",
            "OnPreCull",
            "OnPreRender",
            "OnPostRender",
            "OnRenderObject",
            "OnWillRenderObject",
            "OnGUI",
            "OnRenderImage",
            "OnDrawGizmosSelected",
            "OnDrawGizmos",
            "OnApplicationPause",
            "OnApplicationQuit",
            "OnPlayerConnected",
            "OnServerInitialized",
            "OnConnectedToServer",
            "OnPlayerDisconnected",
            "OnDisconnectedFromServer",
            "OnFailedToConnect",
            "OnFailedToConnectToMasterServer",
            "OnNetworkInstantiate",
            "OnSerializeNetworkView",

            "GetEnumerator",
        };

        ModuleDefMD module;
        List<string> usedNames;
        Random rnd;

        public Renamer(ModuleDefMD module)
        {
            this.StrongRenaming = true;
            this.rnd = new Random();
            this.usedNames = new List<string>();
            this.module = module;
        }

        public void RenameModule()
        {
            log.Info("Renaming things...");
            foreach (TypeDef type in module.Types)
            {
                RenameType(type);
            }   
        }

        void RenameType(TypeDef type)
        {
            if (type.FullName == "AsertSigning")
                return;

            string oldName = type.FullName;

            if (CanRenameType(type))
            {
                string newName = GenerateNewName();
                log.DebugFormat("Renaming class {0} to {1}", type.Name, newName);
                type.Name = newName;
            }
     
            foreach(var method in type.Methods)
            {
                string oldMethodName = method.Name;
                RenameMethod(type, oldName, method);
                if (RenameMethodParams)
                    RenameMethodSignatures(oldName, oldMethodName, method);
            }

            foreach (var field in type.Fields)
                RenameField(oldName, field);

            foreach (var prop in type.Properties)
                RenameProperty(oldName, prop);

            foreach (var ev in type.Events)
                RenameEvent(oldName, ev);


            if (type.HasNestedTypes)
                foreach (TypeDef nested in type.NestedTypes)
                    RenameType(nested);
        }

        void RenameMethod(TypeDef type, string realTypeName, MethodDef method)
        {
            if (!CanRenameMethod(realTypeName, method))
                return;

            string newName = GenerateNewName();
            log.DebugFormat("Renaming method {0}.{1}() to {2}", realTypeName, method.Name, newName);
            method.Name = newName;
        }

        void RenameMethodSignatures(string realTypeName, string realMethodName, MethodDef method)
        {
            foreach (var p in method.Parameters)
            {
                string newName = GenerateNewName();
                log.DebugFormat("Renaming method parameter {0}.{1}({2}) to {3}", realTypeName, realMethodName, p.Name, newName);
                p.Name = newName;
            }
        }

        void RenameProperty(string realTypeName, PropertyDef prop)
        {
            if (!CanRenameProperty(realTypeName, prop))
                return;
            string newName = GenerateNewName();
            log.DebugFormat("Renaming property {0}.{1} to {2}", realTypeName, prop.Name, newName);
            prop.Name = newName;
        }

        void RenameEvent(string realTypeName, EventDef ev)
        {
            if (!CanRenameEvent(realTypeName, ev))
                return;
            string newName = GenerateNewName();
            log.DebugFormat("Renaming event {0}.{1} to {2}", realTypeName, ev.Name, newName);
            ev.Name = newName;
        }

        void RenameField(string realTypeName, FieldDef field)
        {
            if (!CanRenameField(realTypeName, field))
                return;

            string newName = GenerateNewName();
            log.DebugFormat("Renaming field {0}.{1} to {2}", realTypeName, field.Name, newName);
            //log.Debug("FLD: " + realTypeName + "." + field.Name + "   ["+ field.FieldType +"]" + " >>> " + field.FieldType.IsGenericInstanceType);
            field.Name = newName;
        }


        bool CanRenameType(TypeDef type)
        {
            if (!RenameTypes)
                return false;
            if (type.IsRuntimeSpecialName || type.IsSpecialName)
                return false;
            if (type.IsImport)
                return false;
            if (type.IsGlobalModuleType)
                return false;
            if (IsTypeFromUnity(type))
                return false;
            if (type.CustomAttributes.Any(a => a.TypeFullName.EndsWith("DoNotRename", StringComparison.InvariantCulture)))
                return false;

            return true;
        }

        bool IsTypeFromUnity(TypeDef type)
        {
            var bType = type.BaseType as ITypeDefOrRef;
            while (bType != null)
            {
                if (bType.FullName.Contains("UnityEngine"))
                    return true;
                bType = bType.GetBaseType() as ITypeDefOrRef;
            }

            return false;
        }

        bool CanRenameMethod(string realTypeName, MethodDef method)
        {
            if (!RenameMethods)
                return false;
            if (IsTypeFromUnity(method.DeclaringType))
                return false;
            if (method.IsRuntimeSpecialName || method.IsRuntime)
                return false;
            if (method.IsConstructor)
                return false;
            if (method.IsVirtual || method.IsAbstract || method.HasOverrides) //Not sure if both ruins
                return false;
            if (realTypeName.IndexOf("<>__AnonType", StringComparison.InvariantCulture) == 0) //Anonimous method make it hurt
                return false;
            if (method.DeclaringType.HasGenericParameters) //Generics too
                return false;
            if (method.DeclaringType.IsForwarder)
                return false;
            if (lockedMethods.Any(m => m == method.Name))
                return false;
            if (method.CustomAttributes.Any(a => a.TypeFullName.EndsWith("DoNotRename", StringComparison.InvariantCulture)))
                return false;
            //if (IsMethodContainsReflection(method))
            //    return false;
            return true;
        }

        bool IsMethodContainsReflection(MethodDef method)
        {
            if (!method.HasBody)
                return false;
            var mbody = method.Body.Instructions;
            foreach (var instruction in mbody)
            {
                if (instruction.Operand != null && instruction.Operand.ToString().ToLower().Contains("reflection"))
                {
                    return true;
                }
            }
            return false;
        }

        bool CanRenameField(string realTypeName, FieldDef field)
        {
            if (!RenameFields)
                return false;
            if (realTypeName.IndexOf("<>__AnonType", StringComparison.InvariantCulture) == 0) //Anonimous method make it hurt
                return false;
            if (field.IsRuntimeSpecialName || field.IsSpecialName)
                return false;
            if (field.DeclaringType.HasGenericParameters)
                return false;
            if (field.DeclaringType.IsSerializable && !field.IsNotSerialized)
                return false;
            if (field.IsPinvokeImpl)
                return false;
            if (field.IsLiteral && field.DeclaringType.IsEnum)
                return false;
            if (field.CustomAttributes.Any(a => a.TypeFullName.EndsWith("DoNotRename", StringComparison.InvariantCulture)))
                return false;
            return true;
        }

        bool CanRenameProperty(string realTypeName, PropertyDef prop)
        {
            if (!RenameProps)
                return false;
            if (prop.IsRuntimeSpecialName)
                return false;
            if (realTypeName.IndexOf("<>__AnonType", StringComparison.InvariantCulture) == 0) //Anonimous method make it hurt
                return false;
            if (prop.CustomAttributes.Any(a => a.TypeFullName.EndsWith("DoNotRename", StringComparison.InvariantCulture)))
                return false;
            return true;
        }

        bool CanRenameEvent(string realTypeName, EventDef ev)
        {
            if (!RenameEvents)
                return false;
            if (ev.IsRuntimeSpecialName || ev.IsSpecialName)
                return false;
            if (ev.CustomAttributes.Any(a => a.TypeFullName.EndsWith("DoNotRename", StringComparison.InvariantCulture)))
                return false;
            return true;
        }

        public string GenerateNewName(bool unicode = false)
        {
            Func<string> getName = () =>
            {
                if (StrongRenaming)
                    return Utils.RandomStringUnicode(rnd, 8);
                else
                    return Utils.RandomStringHex(rnd, 8);
            };

            string str = getName();
            while (usedNames.Contains(str))
            {
                str = getName();
            }
            usedNames.Add(str);
            return str;
        }
    }
}
