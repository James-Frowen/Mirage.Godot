# Mirage.Godot

High level c# Networking for [Godot](https://godotengine.org/) based on popular open source Unity networking solutions [Mirror](https://github.com/MirrorNetworking/Mirror) and [Mirage](https://github.com/MirageNet/Mirage)

## Features

- Multiple Transport support. Use built in UDP transport or add your own to work with other services
- RPC calls via attributes
- SyncVar to sync fields without any extra code
- Automatic serialization
- Spawn Objects other the network
- Object ownership. Allows the client to have control over object and automatically sync changes to other clients

## Install 

*work in progrss*

## Coge Generation

Mirage.Godot uses Mono.Cecil to modify the c# source code after it is compiled, this allowes for features to have high performance and easy to use.

To Setup add this code to the default csproj for the godot project
```csproj
  <Target Name="PostBuild" AfterTargets="PostBuildEvent">
    <Exec Command="Path/To/Mirage.CodeGen.exe $(IntermediateOutputPath)$(TargetFileName) $(TargetDir)" />
    <Error Condition="$(ExitCode) == 1" />
  </Target>
```
and modify the `Path/To/Mirage.CodeGen.exe ` path to where you built the `Mirage.CodeGen.exe` file
