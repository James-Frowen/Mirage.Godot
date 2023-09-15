using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Mirage.CodeGen;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace Mirage.Weaver
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            try
            {
                Console.WriteLine("Mirage Weaver start");

                // This is to prevent an array out of bounds exception
                // if you're double clicking or running it without any arguments.
                if (args.Length == 0)
                {
                    // Environment.ExitCode = 1;
                    Console.Error.WriteLine("ERROR: Mirage CodeGen cannot be run without any arguments!");
                    Console.WriteLine("Usage: Mirage.CodeGen.exe <path to application DLL>");
                    Console.WriteLine("Example: Mirage.CodeGen.exe D:\\Dev\\CoolApp\\MyMirageApp.dll");
                    Environment.Exit(1);
                }

                var dllPath = args[0];
                var data = File.ReadAllBytes(dllPath);
                var asm = Assembly.Load(data);

                // TODO: use proper Assembly paths 
                var references = asm.GetReferencedAssemblies().Select(a => Path.Combine(Path.GetDirectoryName(dllPath), a.Name)).ToArray();
                var compiledAssembly = new CompiledAssembly(dllPath, references, new string[0]);
                var weaverLogger = new WeaverLogger(false);
                var weaver = new Weaver(weaverLogger);
                var result = weaver.Process(compiledAssembly);

                Write(result, dllPath, compiledAssembly.PdbPath);

                var exitCode = CheckDiagnostics(weaverLogger);
                Environment.ExitCode = 0;
            }
            catch (Exception e)
            {
                Environment.ExitCode = 1;
                Console.Error.WriteLine(e);
                return;
            }
        }

        private static int CheckDiagnostics(WeaverLogger weaverLogger)
        {
            var diagnostics = weaverLogger.GetDiagnostics();
            var exitCode = 0;
            foreach (var message in diagnostics)
            {
                var data = message.MessageData;
                var type = message.DiagnosticType;
                Console.WriteLine($"[{type}]: {data}");

                if (type == DiagnosticType.Error)
                    exitCode = 1;
            }
            return exitCode;
        }

        private static void Write(Result result, string dllPath, string pdbPath)
        {
            var inMemory = result.ILPostProcessResult.InMemoryAssembly;

            var pe = inMemory.PeData;
            var pdb = inMemory.PdbData;

            File.WriteAllBytes(dllPath, pe.ToArray());
            File.WriteAllBytes(pdbPath, pdb.ToArray());
        }
    }

    public class CompiledAssembly : ICompiledAssembly
    {
        public CompiledAssembly(string dllPath, string[] references, string[] defines)
        {
            Name = Path.GetFileName(dllPath);
            PdbPath = $"{Path.GetDirectoryName(dllPath)}/{Path.GetFileNameWithoutExtension(dllPath)}.pdb";
            var peData = File.ReadAllBytes(dllPath);
            var pdbData = File.ReadAllBytes(PdbPath);
            InMemoryAssembly = new InMemoryAssembly(peData, pdbData);
            References = references;
            Defines = defines;
        }

        public InMemoryAssembly InMemoryAssembly { get; }

        public string Name { get; }
        public string PdbPath { get; }
        public string[] References { get; }
        public string[] Defines { get; }
    }
}


