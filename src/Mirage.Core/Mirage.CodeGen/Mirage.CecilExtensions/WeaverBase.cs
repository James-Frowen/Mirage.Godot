using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Mirage.Weaver;
using Mono.Cecil;
using Mono.Cecil.Cil;

namespace Mirage.CodeGen
{
    public enum ResultType
    {
        Success,
        NoChanges,
        Failed,
    }

    public struct Result
    {
        public ResultType Type;
        public AssemblyDefinition AssemblyDefinition;
        public InMemoryAssembly InMemoryAssembly;
        public List<DiagnosticMessage> Diagnostics;
    }

    /// <summary>
    /// Use as a base class for custom IL weaver. This class will read and write the AssemblyDefinition and return the results.
    /// </summary>
    public abstract class WeaverBase
    {
        public virtual string Name => GetType().FullName;

        public readonly IWeaverLogger logger;
        public readonly WeaverDiagnosticsTimer timer;

        private AssemblyDefinition _assemblyDefinition;

        public WeaverBase(IWeaverLogger logger = null)
        {
            this.logger = logger ?? new WeaverLogger(false);
            timer = new WeaverDiagnosticsTimer(GetType().Name)
            {
                writeToFile = true,
            };
        }

        protected abstract ResultType Process(AssemblyDefinition assembly, CompiledAssembly compiledAssembly);

        public Result Process(CompiledAssembly compiledAssembly, string[] hintDirectories)
        {
            try
            {
                timer.Start(compiledAssembly.Name);

                using (timer.Sample("AssemblyDefinitionFor"))
                {
                    _assemblyDefinition = ReadAssembly(compiledAssembly, hintDirectories);
                }

                var result = Process(_assemblyDefinition, compiledAssembly);
                var diagnostics = logger.GetDiagnostics();
                InsertDebugIfErrors(diagnostics, compiledAssembly);

                return new Result
                {
                    Type = result,
                    AssemblyDefinition = _assemblyDefinition,
                    Diagnostics = diagnostics,
                    InMemoryAssembly = result == ResultType.Success ? Success() : null,
                };
            }
            catch (Exception e)
            {
                // this means that Weaver had problems
                // we should never get here unless there is a bug in the implementation
                // for problems with user code, use logger instead of throwing so that multiple errors can be reported to the user.

                return new Result
                {
                    Type = ResultType.Failed,
                    Diagnostics = Error(e)
                };
            }
            finally
            {
                // end in finally incase it return early
                timer?.End();
            }
        }

        private InMemoryAssembly Success()
        {
            // write assembly to file on success
            var pe = new MemoryStream();
            var pdb = new MemoryStream();

            var writerParameters = new WriterParameters
            {
                SymbolWriterProvider = new PortablePdbWriterProvider(),
                SymbolStream = pdb,
                WriteSymbols = true
            };

            _assemblyDefinition.Write(pe, writerParameters);
            return new InMemoryAssembly(pe.ToArray(), pdb.ToArray());
        }

        private void InsertDebugIfErrors(List<DiagnosticMessage> diag, CompiledAssembly compiledAssembly)
        {
            var errorCount = diag.Where(x => x.DiagnosticType == DiagnosticMessage.Type.Error).Count();
            if (errorCount == 0)
                return;

            var defineMsg = ArrayMessage("Defines", compiledAssembly.Defines);
            var refMsg = ArrayMessage("References", compiledAssembly.References);
            var msg = $"Weaver Failed with {errorCount} errors on {compiledAssembly.Name}. See Editor log for full details.\n{defineMsg}\n{refMsg}";

            // insert debug info for weaver as first message,
            diag.Insert(0, new DiagnosticMessage
            {
                DiagnosticType = DiagnosticMessage.Type.Error,
                MessageData = msg
            });
        }

        private static string ArrayMessage(string prefix, string[] array)
        {
            return array.Length == 0
                ? $"{prefix}:[]"
                : $"{prefix}:[\n  {string.Join("\n  ", array)}\n]";
        }

        private List<DiagnosticMessage> Error(Exception e)
        {
            var message = new DiagnosticMessage
            {
                DiagnosticType = DiagnosticMessage.Type.Error,
                MessageData = $"Weaver {Name} failed on {_assemblyDefinition?.Name} because of Exception: {e}",
            };
            return new List<DiagnosticMessage> { message };
        }

        private static AssemblyDefinition ReadAssembly(CompiledAssembly compiledAssembly, string[] hintDirectories)
        {
            var assemblyResolver = new PostProcessorAssemblyResolver(compiledAssembly, hintDirectories);
            var readerParameters = new ReaderParameters
            {
                SymbolStream = new MemoryStream(compiledAssembly.InMemoryAssembly.PdbData),
                SymbolReaderProvider = new PortablePdbReaderProvider(),
                AssemblyResolver = assemblyResolver,
                ReflectionImporterProvider = new PostProcessorReflectionImporterProvider(),
                ReadingMode = ReadingMode.Immediate
            };

            var assemblyDefinition = AssemblyDefinition.ReadAssembly(new MemoryStream(compiledAssembly.InMemoryAssembly.PeData), readerParameters);

            // apparently, it will happen that when we ask to resolve a type that lives inside the MyRuntime.asmdef, and we
            // are also postprocessing MyRuntime.asmdef, type resolving will fail, because we do not actually try to resolve
            // inside the assembly we are processing. Let's make sure we do that, so that we can use postprocessor features inside
            // MyRuntime.asmdef itself as well.
            assemblyResolver.AddAssemblyDefinitionBeingOperatedOn(assemblyDefinition);

            return assemblyDefinition;
        }
    }
}
