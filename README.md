# Zombified Initiative

## Key configuration

On its first launch, the mod creates the standard BepInEx configuration file:

```text
BepInEx/config/Zombified_Initiative.cfg
```

Each action has one entry in the `[Keyboard]` section and one in the `[Mouse]` section.
Set either entry to `None` to disable that input.
Both bindings remain active when both are configured.

| Config entry | Action | Keyboard default | Mouse default |
| --- | --- | --- | --- |
| `ToggleDebugLogging` | Toggle debug logging | `L` | `None` |
| `ToggleAutomaticPickups` | Toggle automatic pickups for all bots | `J` | `None` |
| `ToggleAutomaticResourceSharing` | Toggle automatic resource use/sharing for all bots | `K` | `None` |
| `SelectDauda` | Hold to select Dauda | `Alpha8` | `None` |
| `SelectHackett` | Hold to select Hackett | `Alpha9` | `None` |
| `SelectBishop` | Hold to select Bishop | `Alpha0` | `None` |
| `SelectWoods` | Hold to select Woods | `F6` | `None` |
| `AttackAimedEnemy` | Attack the aimed enemy | `None` | `Middle` |
| `PickUpAimedResource` | Pick up the aimed resource | `U` | `Forward` |
| `ShareResourceWithAimedPlayer` | Share resources with the aimed player | `I` | `Back` |

The attack, pickup, and share bindings are pressed while holding the desired bot's selector.
Mouse button names map to Unity's input indexes: `Left` = 0, `Right` = 1, `Middle` = 2, `Back` = 3, and `Forward` = 4. `Extra5` and `Extra6` are also available.
If the side buttons are reversed on your mouse or under Proton, swap `Back` and `Forward` in the config file.

## Development

### Setup

The project targets .NET 6 and uses the GTFO-specific [`BepInExPack_GTFO`](https://thunderstore.io/c/gtfo/p/BepInEx/BepInExPack_GTFO/) dependencies.

1. Get [`BepInExPack_GTFO` 3.2.1](https://thunderstore.io/c/gtfo/p/BepInEx/BepInExPack_GTFO/) and install it in the GTFO game folder.
2. Start the game once and wait for BepInEx to finish generating the interop assemblies. A populated `BepInEx/interop` directory indicates that generation completed successfully.
3. Copy the generated `BepInEx` directory into the root of this repository.

Before building, the copied files must include this layout:

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

The interop assemblies and `GTFO-API.dll` should match the GTFO version you are building the mod for.

### Windows

Install Visual Studio Code and .NET 6.0 framework when prompted.
Open this repository as a solution.

#### Compilation

Make the relevant changes then `Build` the solution.
The compiled plugin will be: `Zombified_Initiative\obj\x64\Release\Zombified_Initiative.dll`.

### Linux 

This is mainly based on the Arch Linux distribution, so adjust it to match yours.

First, install the .NET 6 SDK:

```bash
sudo pacman -S dotnet-sdk-6.0
```

#### Compilation

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
