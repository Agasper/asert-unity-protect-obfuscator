using System;
using System.IO;
using AsertNet.Protection.AntiTamper;
using AsertNet.Protection.Constants;
using AsertNet.Protection.Renaming;
using dnlib.DotNet;
using log4net;


namespace AsertNet.Protection
{
    public class Asert
    {
        public bool StrongRenaming { get; set; }
        public bool RenameTypes { get; set; }
        public bool RenameMethods { get; set; }
        public bool RenameMethodParams { get; set; }
        public bool RenameFields { get; set; }
        public bool RenameProps { get; set; }
        public bool RenameEvents { get; set; }

        public bool HideIntegers { get; set; }
        public bool HideStrings { get; set; }
        public bool EncryptStrings { get; set; }

		internal static readonly ILog log = LogManager.GetLogger(typeof(Asert));
        ModuleDefMD module;

        public Asert(string filename)
        {
            log.Info("Reading file " + filename);
            module = ModuleDefMD.Load(filename);
        }

        public void PerformRenaming()
        {
            Renamer r = new Renamer(module)
            {
                RenameTypes = this.RenameTypes,
                RenameMethods = this.RenameMethods,
                RenameMethodParams = this.RenameMethodParams,
                RenameFields = this.RenameFields,
                RenameProps = this.RenameProps,
                RenameEvents = this.RenameEvents,
                StrongRenaming = this.StrongRenaming,
            };
            r.RenameModule();
        }

        public void ProtectConstants()
        {
            ConstantProtection cp = new ConstantProtection(module)
            {
                HideStrings = this.HideStrings,
                HideIntegers = this.HideIntegers,
                EncryptStrings = this.EncryptStrings,
            };
            cp.Protect();
        }

        public void AntiTamperingProtect()
        {
            AntiTampering.AddCallToModule(module);
        }

        public string AntiTamperingSign(string resultAssembly)
        {
            return AntiTampering.Sign(resultAssembly);
        }

        public void AntiTamperingInjectUnity(string hash, string unityLibFilename)
        {
            log.Info("Reading file " + unityLibFilename);
            var unityModule = ModuleDefMD.Load(unityLibFilename);
            AntiTampering.AddCallToUnity(unityModule, hash);
            unityModule.Write(unityLibFilename);
            log.Info("Unity lib updated");
        }

        public void Save(string filename)
        {
            module.Write(filename);
            log.Info("File Saved to " + filename);
        }

    }
}
