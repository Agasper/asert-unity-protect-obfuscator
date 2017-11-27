using System;
using System.IO;
using AsertNet.Protection;
using log4net;
using log4net.Config;

namespace AsertNet
{
    class MainClass
    {
        internal static readonly ILog log = LogManager.GetLogger(typeof(MainClass));
        public static void Main(string[] args)
        {
            System.Reflection.Assembly assembly = System.Reflection.Assembly.GetEntryAssembly();
            string ApplicationPath = System.IO.Path.GetDirectoryName(assembly.Location);
            XmlConfigurator.Configure(new FileInfo(Path.Combine(ApplicationPath, "logging.xml")));

            log.Info("Asert Unity Build Protection");
            log.Info("Copyright (c) 2017 - Alexander Melkozerov");
            log.Info("-------------------------------------------------");

            Arguments parsedArgs = new Arguments(args);

            if (!parsedArgs.ContainsArg("filename"))
            {
                PrintUsage();
                return;
            }
            string filename = parsedArgs["filename"];


            if (parsedArgs.ContainsArg("antitamper") && !parsedArgs.ContainsArg("unitylib"))
            {
                log.Error("You're going to use anti tampering protection, but forgot to specify location of the unityengine.dll with --unitylib argument");
                return;
            }

            Asert asert = new Asert(filename)
            {
                StrongRenaming = !parsedArgs.ContainsArg("softrenaming"),
                RenameEvents = parsedArgs.ContainsArg("renameevents") || parsedArgs.ContainsArg("renameall"),
                RenameProps = parsedArgs.ContainsArg("renameprops") || parsedArgs.ContainsArg("renameall"),
                RenameFields = parsedArgs.ContainsArg("renamefields") || parsedArgs.ContainsArg("renameall"),
                RenameTypes = parsedArgs.ContainsArg("renametypes") || parsedArgs.ContainsArg("renameall"),
                RenameMethodParams = parsedArgs.ContainsArg("renamemethodparams") || parsedArgs.ContainsArg("renameall"),
                RenameMethods = parsedArgs.ContainsArg("renamemethods") || parsedArgs.ContainsArg("renameall"),

                HideStrings = parsedArgs.ContainsArg("hidestrings"),
                HideIntegers = parsedArgs.ContainsArg("hideintegers"),
                EncryptStrings = parsedArgs.ContainsArg("encryptstrings"),

            };

			if (parsedArgs.ContainsArg("antitamper"))
				asert.AntiTamperingProtect();
            asert.ProtectConstants();
            asert.PerformRenaming();
            asert.Save(filename);
            if (parsedArgs.ContainsArg("antitamper"))
            {
                asert.AntiTamperingInjectUnity(asert.AntiTamperingSign(filename), parsedArgs["unitylib"]);
            }
        }

        static void PrintUsage()
        {
            log.Info("Usage: AsertNet.exe --filename=\"<filename>\"");
            log.Info("Additional parameters:");
            log.Info("--renameall - Perform renaming all things");
            log.Info("--renamemethods - Perform renaming methods");
            log.Info("--renamemethodparams - Perform renaming method parameters");
            log.Info("--renametypes - Perform renaming types");
            log.Info("--renamefields - Perform renaming fields");
            log.Info("--renameprops - Perform renaming props");
            log.Info("--renameevents - Perform renaming events");
            log.Info("--softrenaming - Renaming to hex symbols instead of unreadable UTF-8");
            log.Info("");
            log.Info("--hideintegers - Replacing every integer constant with masked value");
            log.Info("--hidestrings - Move all the string to the special module");
            log.Info("--encryptstrings - Xor all the string (if used will auto enable --hidestrings)");
            log.Info("");
            log.Info("--antitamper - Inject checksum verification");
            log.Info("--unitylib - Location of UnityEngine.dll (required for anti tampering)");
        }

    }
}