# Android Corpses Don't Rot

Stops mechanical corpses from Androids from rotting or producing corpse bile. It removes the relevant components from recognized mechanical corpse definitions.

Converted organic pawns are not covered: they share the human corpse definition, so changing it would also affect ordinary humans.

## Requirements and installation

- RimWorld 1.7
- [Harmony](https://github.com/pardeike/HarmonyRimWorld) (`brrainz.harmony`)
- [Androids](https://steamcommunity.com/sharedfiles/filedetails/?id=2889326293) by ChJees (`ChJees.Androids14`)

Copy the mod folder containing `About/` and `1.7/` into RimWorld's `Mods` folder. Enable it after Harmony and Androids. Package ID: `ertzfiend.androidcorpsesnorot`.

## Compatibility

Mechanical races are recognized by the Androids mechanical properties extension or the `ChJDroid` flesh type. Mods changing corpse definitions or rotting components may interact with this mod; live-game compatibility is not established here.

## Build and test

Requires the .NET 10 SDK and local RimWorld 1.7 and Harmony assemblies:

```sh
dotnet build AndroidCorpsesNoRot.csproj -c Release -p:GameManagedDir=/path/to/assemblies
dotnet run --project tests/AndroidCorpsesNoRot.Tests.csproj -c Release -p:GameManagedDir=/path/to/assemblies
```
