using System;
using System.Collections.Generic;
using System.IO;
using Mirage.Weaver;
using Mono.Cecil;

namespace Mirage.CodeGen
{
    // original code under MIT Copyright (c) 2021 Unity Technologies
    // https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/blob/472d51b34520e8fb6f0aa43fd56d162c3029e0b0/com.unity.netcode.gameobjects/Editor/CodeGen/PostProcessorAssemblyResolver.cs
    internal sealed class PostProcessorAssemblyResolver : IAssemblyResolver
    {
        private readonly Dictionary<string, AssemblyDefinition> _assemblyCache = new Dictionary<string, AssemblyDefinition>();
        private AssemblyDefinition _selfAssembly;
        private readonly string _selfName;
        private readonly string _selfNameDll;
        private readonly FoundAssembly[] _foundAssemblies;
        private readonly string[] _hintDirectories;

        private readonly DefaultAssemblyResolver _defaultResolver = new DefaultAssemblyResolver();
        private readonly ReaderParameters _defaultReadParams = new ReaderParameters(ReadingMode.Deferred);

        private class FoundAssembly
        {
            public string FileName;
            public string ReferenceHint;

            /// <summary>
            /// file that exists
            /// </summary>
            public string FoundPath;
        }

        public PostProcessorAssemblyResolver(CompiledAssembly compiledAssembly, string[] hintDirectories)
        {
            var name = compiledAssembly.Name;
            if (name.EndsWith(".dll"))
            {
                _selfName = name.Substring(0, name.Length - 4);
                _selfNameDll = name;
            }
            else
            {
                _selfName = name;
                _selfNameDll = name + ".dll";
            }

            _hintDirectories = hintDirectories;
            _foundAssemblies = new FoundAssembly[compiledAssembly.References.Length];

            for (var i = 0; i < _foundAssemblies.Length; i++)
            {
                var refHint = compiledAssembly.References[i];
                _foundAssemblies[i] = new FoundAssembly
                {
                    ReferenceHint = refHint,
                    FileName = Path.GetFileName(refHint),
                };
            }
        }

        public void Dispose()
        {
            foreach (var asm in _assemblyCache.Values)
                asm.Dispose();
            _assemblyCache.Clear();
        }

        public void AddAssemblyDefinitionBeingOperatedOn(AssemblyDefinition assemblyDefinition)
        {
            _selfAssembly = assemblyDefinition;
        }

        public AssemblyDefinition Resolve(AssemblyNameReference name) => Resolve(name, null);

        public AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
        {
            if (name.Name == _selfName || name.Name == _selfNameDll)
            {
                if (_selfAssembly == null)
                    throw new Exception("Trying to resolve before self assembly is set");
                return _selfAssembly;
            }

            if (!TryFindFile(name.Name, out var fileName))
            {
                // if we fail to resolve the path, then use the default resolving instead
                // the default resolving knows how to find system types
                var defaultResult = _defaultResolver.Resolve(name, parameters ?? _defaultReadParams);
                return defaultResult;
            }

            var lastWriteTime = File.GetLastWriteTime(fileName);

            var cacheKey = fileName + lastWriteTime;

            if (_assemblyCache.TryGetValue(cacheKey, out var result))
                return result;

            if (parameters == null)
                parameters = new ReaderParameters(ReadingMode.Deferred);

            parameters.AssemblyResolver = this;

            var ms = MemoryStreamFor(fileName);

            var pdb = fileName + ".pdb";
            if (File.Exists(pdb))
                parameters.SymbolStream = MemoryStreamFor(pdb);

            var assemblyDefinition = AssemblyDefinition.ReadAssembly(ms, parameters);
            _assemblyCache.Add(cacheKey, assemblyDefinition);
            return assemblyDefinition;
        }

        private bool TryFindFile(string name, out string fileName)
        {
            var dllName = name + ".dll";
            var exeName = name + ".exe";

            // This method is called a lot, avoid linq
            foreach (var assembly in _foundAssemblies)
            {
                if (assembly.FileName == name || assembly.FileName == dllName || assembly.FileName == exeName)
                {
                    if (assembly.FoundPath == null)
                    {
                        var hint = assembly.ReferenceHint;
                        if (hint.EndsWith(".dll") || hint.EndsWith(".exe"))
                        {
                            assembly.FoundPath = hint;
                        }

                        var hintDll = hint + ".dll";
                        if (File.Exists(hintDll))
                        {
                            assembly.FoundPath = hintDll;
                        }

                        if (_hintDirectories != null)
                        {
                            foreach (var dir in _hintDirectories)
                            {
                                var guess = Path.Combine(dir, assembly.FileName + ".dll");
                                if (File.Exists(guess))
                                {
                                    assembly.FoundPath = guess;
                                    break;
                                }
                            }
                        }
                    }

                    fileName = assembly.FoundPath;
                    return fileName != null;
                }
            }
            fileName = null;
            return false;
        }

        private static MemoryStream MemoryStreamFor(string fileName)
        {
            MemoryStream Read()
            {
                byte[] byteArray;
                using (var fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    byteArray = new byte[fs.Length];
                    var readLength = fs.Read(byteArray, 0, (int)fs.Length);
                    if (readLength != fs.Length)
                        throw new InvalidOperationException("File read length is not full length of file.");
                }

                return new MemoryStream(byteArray);
            }
            void HandleError(IOException e, int retryCount)
            {
                Console.WriteLine($"Caught IO Exception for {fileName}, trying {retryCount} more times");
            }

            return IORetry.Retry(Read, HandleError);
        }
    }
}
