using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Mirage.CodeGen;
using Mirage.Serialization;
using Mirage.Weaver.Serialization;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;

namespace Mirage.Weaver
{
    public class Readers : SerializeFunctionBase
    {
        public Readers(ModuleDefinition module, IWeaverLogger logger) : base(module, logger) { }

        protected override string FunctionTypeLog => "read function";
        protected override Expression<Action> ArrayExpression => () => Mirage.Serialization.CollectionExtensions.ReadArray<byte>(default);

        protected override MethodReference GetGenericFunction()
        {
            var genericType = module.ImportReference(typeof(GenericTypesSerializationExtensions)).Resolve();
            var method = genericType.GetMethod(nameof(GenericTypesSerializationExtensions.Read));
            return module.ImportReference(method);
        }

        protected override MethodReference GenerateEnumFunction(TypeReference typeReference)
        {
            var readMethod = GenerateReaderFunction(typeReference);

            var worker = readMethod.worker;

            worker.Append(worker.Create(OpCodes.Ldarg_0));

            var underlyingType = typeReference.Resolve().GetEnumUnderlyingType();
            var underlyingFunc = TryGetFunction(underlyingType, null);

            worker.Append(worker.Create(OpCodes.Call, underlyingFunc));
            worker.Append(worker.Create(OpCodes.Ret));
            return readMethod.definition;
        }

        private struct ReadMethod
        {
            public readonly MethodDefinition definition;
            public readonly ParameterDefinition readParameter;
            public readonly ILProcessor worker;

            public ReadMethod(MethodDefinition definition, ParameterDefinition readParameter, ILProcessor worker)
            {
                this.definition = definition;
                this.readParameter = readParameter;
                this.worker = worker;
            }
        }
        private ReadMethod GenerateReaderFunction(TypeReference variable)
        {
            var functionName = "_Read_" + variable.FullName;

            // create new reader for this type
            var definition = module.GeneratedClass().AddMethod(functionName,
                    MethodAttributes.Public |
                    MethodAttributes.Static |
                    MethodAttributes.HideBySig,
                    variable);

            var readParameter = definition.AddParam<NetworkReader>("reader");
            definition.Body.InitLocals = true;
            Register(variable, definition);

            var worker = definition.Body.GetILProcessor();
            return new ReadMethod(definition, readParameter, worker);
        }

        protected override MethodReference GenerateCollectionFunction(TypeReference typeReference, List<TypeReference> elementTypes, MethodReference collectionMethod)
        {
            // generate readers for the element
            foreach (var elementType in elementTypes)
                _ = GetFunction_Throws(elementType);

            var readMethod = GenerateReaderFunction(typeReference);

            var collectionReader = collectionMethod.GetElementMethod();

            var methodRef = new GenericInstanceMethod(collectionReader);
            foreach (var elementType in elementTypes)
                methodRef.GenericArguments.Add(elementType);

            // generates
            // return reader.ReadList<T>()

            var worker = readMethod.worker;
            worker.Append(worker.Create(OpCodes.Ldarg_0)); // reader
            worker.Append(worker.Create(OpCodes.Call, methodRef)); // Read

            worker.Append(worker.Create(OpCodes.Ret));

            return readMethod.definition;
        }

        protected override MethodReference GenerateClassOrStructFunction(TypeReference typeReference)
        {
            var readMethod = GenerateReaderFunction(typeReference);

            // create local for return value
            var variable = readMethod.definition.AddLocal(typeReference);

            var worker = readMethod.worker;


            var td = typeReference.Resolve();

            if (!td.IsValueType)
                GenerateNullCheck(worker);

            CreateNew(variable, worker, td);
            ReadAllFields(typeReference, readMethod);

            worker.Append(worker.Create(OpCodes.Ldloc, variable));
            worker.Append(worker.Create(OpCodes.Ret));
            return readMethod.definition;
        }

        private void GenerateNullCheck(ILProcessor worker)
        {
            // if (!reader.ReadBoolean())
            //   return null
            worker.Append(worker.Create(OpCodes.Ldarg_0));
            worker.Append(worker.Create(OpCodes.Call, TryGetFunction<bool>(null)));

            var labelEmptyArray = worker.Create(OpCodes.Nop);
            worker.Append(worker.Create(OpCodes.Brtrue, labelEmptyArray));
            // return null
            worker.Append(worker.Create(OpCodes.Ldnull));
            worker.Append(worker.Create(OpCodes.Ret));
            worker.Append(labelEmptyArray);
        }

        // Initialize the local variable with a new instance
        private void CreateNew(VariableDefinition variable, ILProcessor worker, TypeDefinition td)
        {
            var type = variable.VariableType;
            if (type.IsValueType)
            {
                // structs are created with Initobj
                worker.Append(worker.Create(OpCodes.Ldloca, variable));
                worker.Append(worker.Create(OpCodes.Initobj, type));
            }
            else
            {
                // classes are created with their constructor
                var ctor = Resolvers.ResolveDefaultPublicCtor(type);
                if (ctor == null)
                {
                    throw new SerializeFunctionException($"{type.Name} can't be deserialized because it has no default constructor", type);
                }

                var ctorRef = worker.Body.Method.Module.ImportReference(ctor);

                worker.Append(worker.Create(OpCodes.Newobj, ctorRef));
                worker.Append(worker.Create(OpCodes.Stloc, variable));
            }
        }

        private void ReadAllFields(TypeReference type, ReadMethod readMethod)
        {
            var worker = readMethod.worker;
            // create copy here because we might add static packer field
            var fields = type.FindAllPublicFields();
            foreach (var fieldDef in fields)
            {
                // note:
                // - fieldDef to get attributes
                // - fieldType (made non-generic if possible) used to get type (eg if MyMessage<int> and field `T Value` then get writer for int)
                // - fieldRef (imported) to emit IL codes
                var fieldType = fieldDef.GetFieldTypeIncludingGeneric(type);
                var fieldRef = module.ImportField(fieldDef, type);

                var valueSerialize = ValueSerializerFinder.GetSerializer(module, fieldDef, fieldType, null, this);

                // load this, write value, store value

                // mismatched ldloca/ldloc for struct/class combinations is invalid IL, which causes crash at runtime
                var opcode = type.IsValueType ? OpCodes.Ldloca : OpCodes.Ldloc;
                worker.Append(worker.Create(opcode, 0));

                valueSerialize.AppendRead(module, worker, readMethod.readParameter, fieldType);

                worker.Append(worker.Create(OpCodes.Stfld, fieldRef));
            }
        }

        /// <summary>
        /// Save a delegate for each one of the readers into <see cref="Reader{T}.Read"/>
        /// </summary>
        /// <param name="worker"></param>
        internal void InitializeReaders(ILProcessor worker)
        {
            var genericReaderClassRef = module.ImportReference(typeof(Reader<>));

            var readProperty = typeof(Reader<>).GetProperty(nameof(Reader<object>.Read));
            var fieldRef = module.ImportReference(readProperty.GetSetMethod());
            var networkReaderRef = module.ImportReference(typeof(NetworkReader));
            var funcRef = module.ImportReference(typeof(Func<,>));
            var funcConstructorRef = module.ImportReference(typeof(Func<,>).GetConstructors()[0]);

            foreach (var readFunc in funcs.Values)
            {
                var dataType = readFunc.ReturnType;

                // create a Func<NetworkReader, T> delegate
                worker.Append(worker.Create(OpCodes.Ldnull));
                worker.Append(worker.Create(OpCodes.Ldftn, readFunc));
                var funcGenericInstance = funcRef.MakeGenericInstanceType(networkReaderRef, dataType);
                var funcConstructorInstance = funcConstructorRef.MakeHostInstanceGeneric(funcGenericInstance);
                worker.Append(worker.Create(OpCodes.Newobj, funcConstructorInstance));

                // save it in Reader<T>.Read
                var genericInstance = genericReaderClassRef.MakeGenericInstanceType(dataType);
                var specializedField = fieldRef.MakeHostInstanceGeneric(genericInstance);
                worker.Append(worker.Create(OpCodes.Call, specializedField));
            }
        }
    }
}
