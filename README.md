# Mirage.Godot

High level c# Networking for [Godot](https://godotengine.org/) based on popular open source Unity networking solutions [Mirror](https://github.com/MirrorNetworking/Mirror) and [Mirage](https://github.com/MirageNet/Mirage)

Video Demo of example project: https://youtu.be/Ty55PZWtsJI

## Features

- Multiple Transport support. Use built in UDP transport or add your own to work with other services
- RPC calls via attributes
- SyncVar to sync fields without any extra code
- Automatic serialization
- Spawn Objects other the network
- Object ownership. Allows the client to have control over object and automatically sync changes to other clients

## Install 

1) Clone repo `git clone git@github.com:James-Frowen/Mirage.Godot.git`
2) Copy `src/Mirage.Godot/Scripts` into your godot project
3) In your project's main `.csproj` add reference to:
    - `Mirage.Logging.csproj`
    - `Mirage.SocketLayer.csproj`
4) Build CodeGen: `dotnet build Mirage.CodeGen.csproj -c Release`
    - use `[-o|--output <OUTPUT_DIRECTORY>]` to make the path easier to find
5) Add `PostBuild` target to your main csproj
```xml
  <Target Name="PostBuild" AfterTargets="PostBuildEvent">
    <Exec Command="path/to/Mirage.CodeGen.exe $(IntermediateOutputPath)$(TargetFileName) $(TargetDir) -force" />
    <Error Condition="$(ExitCode) == 1" />
  </Target>
```

#### Notes

`Mirage.CodeGen.csproj` currently uses reference to `Mirage.Godot.csproj` to find Mirage types, but when running will use the types inside the target csproj.

#### Setup commands

Commands to run steps above (replace `path/to/project` with your project)
```sh
git clone git@github.com:James-Frowen/Mirage.Godot.git
cd Mirage.Godot
cp src/Mirage.Godot/Scripts "path/to/project/Mirage.Godot"
dotnet build src/Mirage.Core/Mirage.CodeGen/Mirage.CodeGen.csproj -o ./CodeGen
```
and then add `PostBuild` target manually with path to `CodeGen/CodeGen.exe`

**note:** you may want to exclude the `src/Mirage.Godot/Scripts/Example1` folder when building or it will end up in the Mirage.Godot dll

## Code Generation

Mirage.Godot uses Mono.Cecil to modify the c# source code after it is compiled, this allows for features to have high performance and easy to use.

To Setup add this code to the default csproj for the godot project
```xml
  <Target Name="PostBuild" AfterTargets="PostBuildEvent">
    <Exec Command="path/to/Mirage.CodeGen.exe $(IntermediateOutputPath)$(TargetFileName) $(TargetDir) -force" />
    <Error Condition="$(ExitCode) == 1" />
  </Target>
```
and modify the `Path/To/Mirage.CodeGen.exe ` path to where you built the `Mirage.CodeGen.exe` file


## Development

The example use symlinks to include the Mirage.Godot scripts in the 2nd project. 

To clone this repo with those symlinks run as administrator:
```sh
git clone -c core.symlinks=true git@github.com:James-Frowen/Mirage.Godot.git
```

### Codegen 
when developing the code gen locally you might want to add this step to the start of PostBuild targets so that it will rebuild the codegen project before running it
```xml
    <Exec Command="dotnet build $(ProjectDir)/../Mirage.Core/Mirage.CodeGen/Mirage.CodeGen.csproj -c Release" />
```
