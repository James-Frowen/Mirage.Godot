using System;
using System.Linq;
using System.Reflection;
using Mono.Cecil;

namespace Mirage.CodeGen
{
    internal class PostProcessorReflectionImporterProvider : IReflectionImporterProvider
    {
        public IReflectionImporter GetReflectionImporter(ModuleDefinition module)
        {
            return new PostProcessorReflectionImporter(module);
        }
    }
    // original code under MIT Copyright (c) 2021 Unity Technologies
    // https://github.com/Unity-Technologies/com.unity.netcode.gameobjects/blob/472d51b34520e8fb6f0aa43fd56d162c3029e0b0/com.unity.netcode.gameobjects/Editor/CodeGen/PostProcessorReflectionImporter.cs
    internal class PostProcessorReflectionImporter : DefaultReflectionImporter
    {
        private const string SystemPrivateCoreLib = "System.Private.CoreLib";
        private readonly AssemblyNameReference _correctCorlib;
        private readonly ModuleDefinition _mainModule;

        public PostProcessorReflectionImporter(ModuleDefinition module) : base(module)
        {
            _mainModule = module;
            _correctCorlib = module.AssemblyReferences.FirstOrDefault(a => a.Name == "mscorlib" || a.Name == "netstandard" || a.Name == SystemPrivateCoreLib);
        }

        /// <summary>
        /// This is called per Import, so it needs to be fast
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public override AssemblyNameReference ImportReference(AssemblyName name)
        {
            if (_correctCorlib != null && name.Name == SystemPrivateCoreLib)
            {
                return _correctCorlib;
            }

            if (TryImportFast(name, out var reference))
            {
                return reference;
            }

            return base.ImportReference(name);
        }

        /// <summary>
        /// Tries to import a reference faster than the base method does
        /// </summary>
        /// <param name="name"></param>
        /// <param name="assembly_reference"></param>
        /// <returns>false if referene failed to be found</returns>
        private bool TryImportFast(AssemblyName name, out AssemblyNameReference assembly_reference)
        {
            // getting full name is expensive
            // we cant cache it because the AssemblyName object might be different each time (different hashcode)
            // we can get it once before the loop instead of inside the loop, like in DefaultImporter:
            // https://github.com/jbevain/cecil/blob/0.10/Mono.Cecil/Import.cs#L335
            var fullName = name.FullName;

            var references = module.AssemblyReferences;
            for (var i = 0; i < references.Count; i++)
            {
                var reference = references[i];
                if (fullName == reference.FullName)
                {
                    assembly_reference = reference;
                    return true;
                }
            }

            assembly_reference = null;
            return false;
        }


        public override TypeReference ImportReference(Type type, IGenericParameterProvider context)
        {
            if (TryGetMirageType(type, out var mirageType))
            {
                return mirageType;
            }

            return base.ImportReference(type, context);
        }
        public override MethodReference ImportReference(MethodBase method, IGenericParameterProvider context)
        {
            if (TryGetMirageType(method.DeclaringType, out var mirageType))
            {
                var methods = mirageType.GetMethods(method.Name);
                if (methods.Length == 1)
                    return methods[0];

                MethodDefinition match = null;
                foreach (var m in methods)
                {
                    var methodParams = method.GetParameters();
                    var mParams = m.Parameters;
                    if (mParams.Count != methodParams.Length)
                        continue;

                    var allParamsMatch = true;
                    for (var i = 0; i < methodParams.Length; i++)
                    {
                        var paramTypeName = methodParams[i].ParameterType.Name;
                        var mParamTypeName = mParams[i].ParameterType.Name;
                        if (paramTypeName != mParamTypeName)
                        {
                            break;
                        }
                    }

                    if (allParamsMatch)
                    {
                        if (match != null)
                            throw new Exception($"Multiple methods with the same params. type={method.DeclaringType.FullName} method={method.Name}");

                        match = m;
                    }
                }

                if (match != null)
                    throw new Exception($"Failed to find field in Mirage.Godot. type={method.DeclaringType.FullName} method={method.Name}");
                return match;
            }

            return base.ImportReference(method, context);
        }
        public override FieldReference ImportReference(FieldInfo field, IGenericParameterProvider context)
        {
            if (TryGetMirageType(field.DeclaringType, out var mirageType))
            {
                var fieldRef = mirageType.GetField(field.Name);
                if (fieldRef == null)
                    throw new Exception($"Failed to find field in Mirage.Godot. type={field.DeclaringType.FullName} field={field.Name}");
                return fieldRef;
            }

            return base.ImportReference(field, context);
        }

        private bool TryGetMirageType(Type type, out TypeDefinition mirageType)
        {
            if (type.Assembly.FullName == "Mirage.Godot" || type.Assembly.FullName.StartsWith("Mirage.Godot,"))
            {
                mirageType = _mainModule.GetType(type.Namespace, type.Name);
                if (mirageType == null)
                    throw new Exception($"Failed to find type in Mirage.Godot. type={type.FullName}");
                return true;
            }

            mirageType = null;
            return false;
        }
    }
}
