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

## Docs

Documentation for the unity version of Mirage can be found at [https://miragenet.github.io/Mirage/](https://miragenet.github.io/Mirage/). Most of the same concepts will apply to the Godot version.

## Install 

Requires installation of .NET 8: https://dotnet.microsoft.com/en-us/download/dotnet/8.0

1) Clone repo `git clone git@github.com:James-Frowen/Mirage.Godot.git`
2) Copy `src/Mirage.Godot/Scripts` into your godot project
    - Make sure to create c# solution in godot, [this page](https://docs.godotengine.org/en/stable/tutorials/scripting/c_sharp/c_sharp_basics.html) goes over the basics for using c# inside godot
3) In your project's main `.csproj` add reference to:
    - `Mirage.Logging.csproj`
    - `Mirage.SocketLayer.csproj`
4) Also add in project's main `.csproj` file:
    - ```<AllowUnsafeBlocks>true</AllowUnsafeBlocks>```  
5) Build CodeGen: `dotnet build Mirage.CodeGen.csproj -c Release`
    - use `[-o|--output <OUTPUT_DIRECTORY>]` to make the path easier to find
6) Add Build Targets to your main csproj
```xml
<Project Sdk="Godot.NET.Sdk/4.1.1">
  ...

  <Target Name="PostBuild" AfterTargets="PostBuildEvent">
    <Exec Command="path/to/Mirage.CodeGen.exe $(TargetPath) -force" />
    <Error Condition="$(ExitCode) == 1" />
  </Target>
  <Target Name="PrePublish" BeforeTargets="Publish">
    <Exec Command="path/to/Mirage.CodeGen.exe $(PublishDir)$(TargetFileName) $(TargetDir) -force" />
    <Error Condition="$(ExitCode) == 1" />
  </Target>
</Project>
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
  <Exec Command="path/to/Mirage.CodeGen.exe $(TargetPath) -force" />
  <Error Condition="$(ExitCode) == 1" />
</Target>
<Target Name="PrePublish" BeforeTargets="Publish">
  <Exec Command="path/to/Mirage.CodeGen.exe $(PublishDir)$(TargetFileName) $(TargetDir) -force" />
  <Error Condition="$(ExitCode) == 1" />
</Target>
```
and modify the `Path/To/Mirage.CodeGen.exe ` path to where you built the `Mirage.CodeGen.exe` file.

Note, both targets are required:
- `TargetPath` works best in editor to ensure code gen changes are applied before running
- `PublishDir` is needed because `TargetPath` is not the path copied when exporting the build


## Development

The example use symlinks to include the Mirage.Godot scripts in the 2nd project. 

To clone this repo with those symlinks run as administrator:
```sh
git clone -c core.symlinks=true git@github.com:James-Frowen/Mirage.Godot.git
```

If downloading without symlinks (like from zip file) then you will need to manually copy (not move) the files from `src/Mirage.Godot/Scripts` to `src/Mirage.Godot.Example1/Mirage.Godot`

### Codegen 
when developing the code gen locally you might want to add this step to the start of PostBuild targets so that it will rebuild the codegen project before running it
```xml
    <Exec Command="dotnet build $(ProjectDir)/../Mirage.Core/Mirage.CodeGen/Mirage.CodeGen.csproj -c Release" />
```
