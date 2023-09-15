using System;
using Mirage.CodeGen;
using Mono.Cecil;
using Unity.CompilationPipeline.Common.ILPostProcessing;
using ConditionalAttribute = System.Diagnostics.ConditionalAttribute;

namespace Mirage.Weaver
{
    /// <summary>
    /// Weaves an Assembly
    /// <para>
    /// Debug Defines:<br />
    /// - <c>WEAVER_DEBUG_LOGS</c><br />
    /// - <c>WEAVER_DEBUG_TIMER</c><br />
    /// </para>
    /// </summary>
    public class Weaver : WeaverBase
    {
        private Readers readers;
        private Writers writers;
        private PropertySiteProcessor propertySiteProcessor;

        [Conditional("WEAVER_DEBUG_LOGS")]
        public static void DebugLog(TypeDefinition td, string message)
        {
            Console.WriteLine($"Weaver[{td.Name}] {message}");
        }

        private static void Log(string msg)
        {
            Console.WriteLine($"[Weaver] {msg}");
        }

        public Weaver(IWeaverLogger logger) : base(logger) { }

        protected override ResultType Process(AssemblyDefinition assembly, ICompiledAssembly compiledAssembly)
        {
            Log($"Starting weaver on {compiledAssembly.Name}");
            try
            {
                var module = assembly.MainModule;
                readers = new Readers(module, logger);
                writers = new Writers(module, logger);
                propertySiteProcessor = new PropertySiteProcessor();
                var rwProcessor = new ReaderWriterProcessor(module, readers, writers, logger);

                var modified = false;
                using (timer.Sample("ReaderWriterProcessor"))
                {
                    modified = rwProcessor.Process();
                }

                if (modified)
                {
                    using (timer.Sample("propertySiteProcessor"))
                    {
                        propertySiteProcessor.Process(module);
                    }

                    using (timer.Sample("InitializeReaderAndWriters"))
                    {
                        rwProcessor.InitializeReaderAndWriters();
                    }
                }

                return ResultType.Success;
            }
            catch (Exception e)
            {
                logger.Error("Exception :" + e);
                // write line too because the error about doesn't show stacktrace
                Console.WriteLine("[WeaverException] :" + e);
                return ResultType.Failed;
            }
            finally
            {
                Log($"Finished weaver on {compiledAssembly.Name}");
            }
        }
    }

    public class FoundType
    {
        public readonly TypeDefinition TypeDefinition;

        /// <summary>
        /// Is Derived From NetworkBehaviour
        /// </summary>
        public readonly bool IsNetworkBehaviour;

        public readonly bool IsMonoBehaviour;

        public FoundType(TypeDefinition typeDefinition, bool isNetworkBehaviour, bool isMonoBehaviour)
        {
            TypeDefinition = typeDefinition;
            IsNetworkBehaviour = isNetworkBehaviour;
            IsMonoBehaviour = isMonoBehaviour;
        }

        public override string ToString()
        {
            return TypeDefinition.ToString();
        }
    }
}
