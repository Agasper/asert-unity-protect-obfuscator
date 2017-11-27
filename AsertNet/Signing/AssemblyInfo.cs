using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Mono.Cecil;

namespace AsertNet.Signing
{

    [Serializable]
    public sealed class AssemblyInfo
    {
        public AssemblyInfo()
        {
            FilePath = string.Empty;
        }

        public string FilePath { get; internal set; }
        public byte[] Token { get; internal set; }
        public bool IsSigned { get; internal set; }

        public string HexToken 
        {
            get
            {
                return BitConverter.ToString(Token).Replace("-", string.Empty);
            }
        }


        public static AssemblyInfo GetAssemblyInfo(string assemblyPath)
        {
            if (string.IsNullOrEmpty(assemblyPath))
                throw new ArgumentNullException(nameof(assemblyPath));

            if (!File.Exists(assemblyPath))
                throw new FileNotFoundException("Could not find provided assembly file.", assemblyPath);

            var a = AssemblyDefinition.ReadAssembly(assemblyPath, GetReadParameters(assemblyPath));

            return new AssemblyInfo()
            {
                FilePath = Path.GetFullPath(assemblyPath),
                IsSigned = (a.MainModule.Attributes & ModuleAttributes.StrongNameSigned) != 0,
                Token = a.Name.PublicKeyToken
            };
        }

        public static ReaderParameters GetReadParameters(string assemblyPath)
        {
            var resolver = new DefaultAssemblyResolver();

            if (!string.IsNullOrEmpty(assemblyPath) && File.Exists(assemblyPath))
                resolver.AddSearchDirectory(Path.GetDirectoryName(assemblyPath));

            ReaderParameters readParams = null;

            try
            {
                readParams = new ReaderParameters() { AssemblyResolver = resolver, ReadSymbols = File.Exists(Path.ChangeExtension(assemblyPath, ".pdb")) };
            }
            catch (InvalidOperationException)
            {
                readParams = new ReaderParameters() { AssemblyResolver = resolver };
            }

            return readParams;
        }
    }

}