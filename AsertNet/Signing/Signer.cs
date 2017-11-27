using System;
using System.IO;
using System.Reflection;
using System.Security.Cryptography;
using Mono.Cecil;

namespace AsertNet.Signing
{
    public static class Signer
    {
        public static AssemblyInfo SignAssembly(string assemblyPath)
        {
            if (string.IsNullOrEmpty(assemblyPath))
                throw new ArgumentNullException(nameof(assemblyPath));

            if (!File.Exists(assemblyPath))
                throw new FileNotFoundException("Could not find provided assembly file.", assemblyPath);

            bool writeSymbols = File.Exists(Path.ChangeExtension(assemblyPath, ".pdb"));

            // Get the assembly info and go from there.
            AssemblyInfo info = AssemblyInfo.GetAssemblyInfo(assemblyPath);

            // Don't sign assemblies with a strong-name signature.
            if (info.IsSigned)
                return info;

            AssemblyDefinition.ReadAssembly(assemblyPath, AssemblyInfo.GetReadParameters(assemblyPath))
                  .Write(assemblyPath, new WriterParameters() { StrongNameKeyPair = GenerateStrongNameKeyPair(), WriteSymbols = writeSymbols });

            return AssemblyInfo.GetAssemblyInfo(assemblyPath);
        }

        public static StrongNameKeyPair GenerateStrongNameKeyPair()
        {
            using (var provider = new RSACryptoServiceProvider(1024, new CspParameters() { KeyNumber = 2 }))
            {
                return new StrongNameKeyPair(provider.ExportCspBlob(!provider.PublicOnly));
            }
        }

    }
}
