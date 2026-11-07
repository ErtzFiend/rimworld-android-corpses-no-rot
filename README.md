# Android Corpses Don't Rot

A RimWorld 1.7 compatibility mod for **Androids** (`ChJees.Androids14`). Prevents mechanical android corpses from rotting or producing corpse bile.

## Behavior

- **No Rotting**: Mechanical android corpses never enter the rotting or desiccated states.
- **No Corpse Bile**: Android corpses do not emit `Filth_CorpseBile`.
- **No Decomposition Damage**: Without `CompRottable`, corpses are immune to rotting decomposition damage (`DamageDefOf.Rotting`).
- **Organic Unaffected**: Vanilla humans, animals, and other organic creatures continue rotting as normal.

## Root Cause & Architecture

In `Androids for RW 1.6/1.7`, the `FleshTypeDef` for droids (`ChJDroid`) has `<isOrganic>true</isOrganic>`. During implied def generation (`Verse.DefGenerator.GenerateImpliedDefs_PreResolve`), RimWorld's `ThingDefGenerator_Corpses.GenerateCorpseDef` inspects `raceDef.race.FleshType.isOrganic`. Because it evaluates to `true`, the game attaches:
- `CompProperties_Rottable` (`daysToRotStart: 2.5`, `daysToDessicated: 5`, `rotDamagePerDay: 2`, `dessicatedDamagePerDay: 0.7`)
- `CompProperties_SpawnerFilth` (`filthDef: Filth_CorpseBile`, `requiredRotStage: RotStage.Rotting`)

RimWorld's corpse lifecycle natively supports corpses without `CompRottable`:
- `RottableUtility.GetRotStage` defaults to `RotStage.Fresh` when `CompRottable` is null.
- `Corpse.CurRotDrawMode` safely handles a null `CompRottable`.
- Rotting damage is applied exclusively inside `CompRottable.TickInterval`.

### Implementation Approach

The mod uses a two-pronged strategy:
1. **Startup Def Pass (`AndroidCorpsesNoRotMain`)**:
   Runs during `[StaticConstructorOnStartup]`, scanning `DefDatabase<ThingDef>.AllDefsListForReading`. Strips `CompProperties_Rottable` and corpse-bile `CompProperties_SpawnerFilth` from all implied android corpse defs.
2. **Harmony Postfix (`Patch_ThingDefGenerator_Corpses_GenerateCorpseDef`)**:
   Patches `RimWorld.ThingDefGenerator_Corpses.GenerateCorpseDef(ThingDef, bool, string)` to ensure any corpse def generated dynamically, hot-reloaded, or created via anomaly handlers has its rotting comps stripped immediately.

### Android Classification

Pawn races are classified strictly without substring matching:
- The race def has a `DefModExtension` of exact type `Androids.MechanicalPawnProperties`, OR
- The race `FleshTypeDef` has `defName == "ChJDroid"`.

## Converted-Organic Scope Limitation (`ChjAndroidLike`)

- **Limitation**: Converted organic pawns (pawns converted to androids via `AndroidUtility.Androidify` who receive the `ChjAndroidLike` hediff) are **out of scope**.
- **Reason**: The conversion hediff is applied in-place to vanilla `Human` pawns without altering their `ThingDef`. Because all humans share the single implied `Corpse_Human` def, stripping rotting components at the def level from `Corpse_Human` would prevent all human corpses in the colony and world from rotting.
- Consequently, this mod targets only true mechanical android races that possess dedicated corpse definitions.

## Dependencies

- RimWorld 1.7
- [Harmony](https://github.com/pardeike/HarmonyRimWorld) (`brrainz.harmony`)
- [Androids for RW 1.6 / 1.7](https://steamcommunity.com/sharedfiles/filedetails/?id=2889326293) (`ChJees.Androids14`)

## Build & Test Instructions

### Build Mod Assembly
```bash
dotnet build -c Release AndroidCorpsesNoRot.csproj
```
The compiled DLL is output to `bin/Release/AndroidCorpsesNoRot.dll` and automatically copied to `1.7/Assemblies/AndroidCorpsesNoRot.dll`.

### Run Test Suite
```bash
dotnet run --project tests/AndroidCorpsesNoRot.Tests.csproj -c Release
```
