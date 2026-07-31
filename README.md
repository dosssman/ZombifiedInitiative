# Zombified Initiative

## Linux

### Setup

The project targets .NET 6 and uses the GTFO-specific `BepInExPack_GTFO`
dependencies.

1. Get `BepInExPack_GTFO` 3.2.1 and install it in the GTFO game folder.
2. Start the game once and wait for BepInEx to finish generating the interop
   assemblies. A populated `BepInEx/interop` directory indicates that generation
   completed successfully.
3. Copy the generated `BepInEx` directory into the root of this repository.

Starting the game is not required if you already have a populated `interop`
directory generated for the same GTFO version. Before building, the copied files
must include this layout:

```text
BepInEx/
├── core/
│   ├── 0Harmony.dll
│   ├── BepInEx.Core.dll
│   ├── BepInEx.Unity.IL2CPP.dll
│   ├── Il2CppInterop.Common.dll
│   └── Il2CppInterop.Runtime.dll
├── interop/
│   └── ... generated GTFO and Unity DLLs
└── plugins/
    └── GTFO-API.dll
```

The interop assemblies and `GTFO-API.dll` should match the GTFO version you are
building the mod for.

Install the .NET 6 SDK. On Arch Linux:

```bash
sudo pacman -S dotnet-sdk-6.0
```

### Compilation

Run the build from the root of this repository so that `$PWD/BepInEx` resolves to
the copied dependency directory:

```bash
dotnet build Zombified_Initiative.sln \
  --configuration Release \
  -p:Platform=x64 \
  -p:BepInEx="$PWD/BepInEx" \
  -p:OutputPath="$PWD/out/"
```

The resulting DLLs are placed in the `out` directory:

```text
out/Zombified_Initiative.dll       # Deployable plugin; install this one
out/ref/Zombified_Initiative.dll   # Compiler reference assembly; do not install
```
