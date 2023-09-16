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

### Build from source

1) Clone repo `git clone git@github.com:James-Frowen/Mirage.Godot.git`
2) Build code `dotnet build`
    - use `[-o|--output <OUTPUT_DIRECTORY>]` to make the folder easier to find
3) Copy the `dll` files into your godot project, Or reference them in your `.csproj` file:
    - Mirage.godot.dll
    - Mirage.Logging.dll
    - Mirage.SocketLayer.dll
    - Mirage.Core.dll
5) Build `Mirage.CodeGen.exe` then include `PostBuild` target in your csproj (see below)

```sh
git clone git@github.com:James-Frowen/Mirage.Godot.git
cd Mirage.Godot
dotnet build
```

**note:** you may want to exclude the `src/Mirage.Godot/Scripts/Example1` folder when building or it will end up in the Mirage.Godot dll

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
