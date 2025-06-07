using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Mirage.CodeGen;

namespace Mirage.Weaver
{
    internal class Program
    {
        private static void Main(string[] argsArray)
        {
            try
            {
                var args = new List<string>(argsArray);
                Console.WriteLine($"ONE-LINE ARGS={string.Join(' ', argsArray)}");

                Console.WriteLine("Mirage Weaver start");

                var force = args.Contains("-force");
                // remove any flags, first arg should be dll path
                args.RemoveAll(x => x.StartsWith("-"));

                // This is to prevent an array out of bounds exception
                // if you're double clicking or running it without any arguments.
                if (args.Count == 0)
                {
                    // Environment.ExitCode = 1;
                    Console.Error.WriteLine("ERROR: Mirage CodeGen cannot be run without any arguments!");
                    Console.WriteLine("Usage: Mirage.CodeGen.exe <path to application DLL> [hint paths...] [-force]");
                    Console.WriteLine("Example: Mirage.CodeGen.exe D:\\Dev\\CoolApp\\MyMirageApp.dll -force");
                    Environment.Exit(1);
                }

                var dllPath = args[0];
                Console.WriteLine($"Weaver target: {dllPath}");
                var hints = new List<string>();
                hints.Add(Path.GetDirectoryName(dllPath));
                for (var i = 1; i < args.Count; i++)
                {
                    hints.Add(args[i]);
                }

                foreach (var hint in hints)
                    Console.WriteLine($"Dll Hint path: {hint}");

                var data = File.ReadAllBytes(dllPath);
                var asm = Assembly.Load(data);

                // TODO: use proper Assembly paths
                // todo move this to PostProcessorAssemblyResolver
                var references = asm.GetReferencedAssemblies().Select(a => a.Name).ToArray();
                var shouldProcess = references.Contains("Mirage.Godot") || references.Contains("Mirage.Godot.dll");
                if (!force && !shouldProcess)
                {
                    Console.WriteLine($"Skipping weaver on {Path.GetFileName(dllPath)} because assembly does not reference Mirage.Core");
                    Environment.ExitCode = 0;
                    return;
                }

                var compiledAssembly = new CompiledAssembly(dllPath, references, new string[0]);
                var weaverLogger = new WeaverLogger(false);
                var weaver = new Weaver(weaverLogger);
                var result = weaver.Process(compiledAssembly, hints.ToArray());

                if (result.Type == ResultType.Success)
                {
                    Write(result, dllPath, compiledAssembly.PdbPath);
                }

                Environment.ExitCode = CheckDiagnostics(result.Diagnostics);
            }
            catch (Exception e)
            {
                Environment.ExitCode = 1;
                Console.Error.WriteLine(e);
                return;
            }
        }

        private static int CheckDiagnostics(List<DiagnosticMessage> diagnostics)
        {
            var exitCode = 0;
            foreach (var message in diagnostics)
            {
                var data = message.MessageData;
                var type = message.DiagnosticType;
                Console.WriteLine($"[{type}]: {data}");

                if (type == DiagnosticMessage.Type.Error)
                    exitCode = 1;
            }
            return exitCode;
        }

        private static void Write(Result result, string dllPath, string pdbPath)
        {
            var inMemory = result.InMemoryAssembly;

            var pe = inMemory.PeData;
            var pdb = inMemory.PdbData;

            File.WriteAllBytes(dllPath, pe.ToArray());
            File.WriteAllBytes(pdbPath, pdb.ToArray());
        }
    }

    public class CompiledAssembly
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

        public readonly InMemoryAssembly InMemoryAssembly;

        public readonly string Name;
        public readonly string PdbPath;
        public readonly string[] References;
        public readonly string[] Defines;
    }
    public class InMemoryAssembly
    {
        public InMemoryAssembly(byte[] peData, byte[] pdbData)
        {
            PeData = peData;
            PdbData = pdbData;
        }

        public readonly byte[] PeData;
        public readonly byte[] PdbData;
    }
}


