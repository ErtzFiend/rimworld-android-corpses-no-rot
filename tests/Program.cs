using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;
using AndroidCorpsesNoRot;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Androids
{
    public class MechanicalPawnProperties : DefModExtension
    {
    }
}

namespace CustomOther
{
    public class MechanicalPawnProperties : DefModExtension
    {
    }
}

internal static class Program
{
    private static int failures;

    private static void Check(bool ok, string name)
    {
        Console.WriteLine((ok ? "PASS " : "FAIL ") + name);
        if (!ok) failures++;
    }

    private static int Main()
    {
        // Microsoft.Extensions.* 10.x runtime assembly fallback
        AssemblyLoadContext.Default.Resolving += (ctx, name) =>
        {
            var path = "/root/.nuget/packages/microsoft.aspnetcore.app.runtime.linux-x64/10.0.14/runtimes/linux-x64/lib/net10.0/" + name.Name + ".dll";
            return File.Exists(path) ? ctx.LoadFromAssemblyPath(path) : null;
        };

        // 1. Initialize patches and startup def pass via static constructor
        RuntimeHelpers.RunClassConstructor(typeof(AndroidCorpsesNoRotMain).TypeHandle);

        // --- HARMONY REGISTRATION ---
        var genCorpseTarget = AccessTools.Method(typeof(ThingDefGenerator_Corpses), "GenerateCorpseDef",
            new[] { typeof(ThingDef), typeof(bool), typeof(string) });
        Check(genCorpseTarget != null, "Target resolves: ThingDefGenerator_Corpses.GenerateCorpseDef");

        var patchInfo = Harmony.GetPatchInfo(genCorpseTarget);
        bool postfixRegistered = patchInfo != null && patchInfo.Postfixes.Any(p =>
            p.PatchMethod.DeclaringType == typeof(Patch_ThingDefGenerator_Corpses_GenerateCorpseDef));
        Check(postfixRegistered, "Harmony postfix registered on ThingDefGenerator_Corpses.GenerateCorpseDef");

        // --- CLASSIFIER CHECKS ---
        Check(!AndroidCorpsesNoRotMain.IsAndroidRace(null), "Classifier: null raceDef is false");

        var emptyDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        emptyDef.race = null;
        Check(!AndroidCorpsesNoRotMain.IsAndroidRace(emptyDef), "Classifier: raceDef with null race is false");

        var humanRaceDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        humanRaceDef.defName = "Human";
        humanRaceDef.race = (RaceProperties)RuntimeHelpers.GetUninitializedObject(typeof(RaceProperties));
        Check(!AndroidCorpsesNoRotMain.IsAndroidRace(humanRaceDef), "Classifier: human race is false");

        // Android via Androids.MechanicalPawnProperties DefModExtension
        var androidExtRaceDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        androidExtRaceDef.defName = "ChjAndroid";
        androidExtRaceDef.race = (RaceProperties)RuntimeHelpers.GetUninitializedObject(typeof(RaceProperties));
        androidExtRaceDef.modExtensions = new List<DefModExtension> { new Androids.MechanicalPawnProperties() };
        Check(AndroidCorpsesNoRotMain.IsAndroidRace(androidExtRaceDef),
            "Classifier: race with Androids.MechanicalPawnProperties is true");

        // Android via ChJDroid fleshType
        var droidRaceDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        droidRaceDef.defName = "ChjDroid";
        droidRaceDef.race = (RaceProperties)RuntimeHelpers.GetUninitializedObject(typeof(RaceProperties));
        var droidFlesh = (FleshTypeDef)RuntimeHelpers.GetUninitializedObject(typeof(FleshTypeDef));
        droidFlesh.defName = "ChJDroid";
        var fleshField = typeof(RaceProperties).GetField("fleshType", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        fleshField.SetValue(droidRaceDef.race, droidFlesh);
        Check(AndroidCorpsesNoRotMain.IsAndroidRace(droidRaceDef),
            "Classifier: race with FleshTypeDef ChJDroid is true");

        // Exact match check: substring fleshType defName must be false
        var substringFleshDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        substringFleshDef.defName = "FakeDroid";
        substringFleshDef.race = (RaceProperties)RuntimeHelpers.GetUninitializedObject(typeof(RaceProperties));
        var fakeFlesh = (FleshTypeDef)RuntimeHelpers.GetUninitializedObject(typeof(FleshTypeDef));
        fakeFlesh.defName = "ChJDroidSubString";
        fleshField.SetValue(substringFleshDef.race, fakeFlesh);
        Check(!AndroidCorpsesNoRotMain.IsAndroidRace(substringFleshDef),
            "Classifier: fleshType with substring 'ChJDroidSubString' is false");

        // Exact match check: different namespace extension must be false
        var otherExtDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        otherExtDef.defName = "OtherExtPawn";
        otherExtDef.race = (RaceProperties)RuntimeHelpers.GetUninitializedObject(typeof(RaceProperties));
        otherExtDef.modExtensions = new List<DefModExtension> { new CustomOther.MechanicalPawnProperties() };
        Check(!AndroidCorpsesNoRotMain.IsAndroidRace(otherExtDef),
            "Classifier: extension with different namespace 'CustomOther.MechanicalPawnProperties' is false");

        // --- COMPONENT FILTER CHECKS (ShouldRemoveComp) ---
        Check(!AndroidCorpsesNoRotMain.ShouldRemoveComp(null), "ShouldRemoveComp: null is false");

        var forbiddableComp = new CompProperties_Forbiddable();
        Check(!AndroidCorpsesNoRotMain.ShouldRemoveComp(forbiddableComp), "ShouldRemoveComp: CompProperties_Forbiddable is false");

        var genericComp = new CompProperties();
        Check(!AndroidCorpsesNoRotMain.ShouldRemoveComp(genericComp), "ShouldRemoveComp: generic CompProperties is false");

        var rottableComp = new CompProperties_Rottable { daysToRotStart = 2.5f, daysToDessicated = 5f };
        Check(AndroidCorpsesNoRotMain.ShouldRemoveComp(rottableComp), "ShouldRemoveComp: CompProperties_Rottable is true");

        var bileDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        bileDef.defName = "Filth_CorpseBile";

        var filthBileComp = new CompProperties_SpawnerFilth
        {
            filthDef = bileDef,
            requiredRotStage = RotStage.Rotting
        };
        Check(AndroidCorpsesNoRotMain.ShouldRemoveComp(filthBileComp), "ShouldRemoveComp: Filth_CorpseBile SpawnerFilth is true");

        var otherFilthDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        otherFilthDef.defName = "Filth_Trash";
        var otherFilthComp = new CompProperties_SpawnerFilth
        {
            filthDef = otherFilthDef,
            requiredRotStage = null
        };
        Check(!AndroidCorpsesNoRotMain.ShouldRemoveComp(otherFilthComp), "ShouldRemoveComp: non-bile Filth_Trash SpawnerFilth is false");

        // --- STRIPPING LOGIC CHECKS (StripCorpseComps) ---
        Check(!AndroidCorpsesNoRotMain.StripCorpseComps(null), "StripCorpseComps: null corpseDef returns false");

        var corpseNullComps = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        corpseNullComps.comps = null;
        Check(!AndroidCorpsesNoRotMain.StripCorpseComps(corpseNullComps), "StripCorpseComps: corpseDef with null comps returns false");

        var corpseWithComps = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        corpseWithComps.defName = "Corpse_ChjDroid";
        corpseWithComps.comps = new List<CompProperties>
        {
            forbiddableComp,
            rottableComp,
            filthBileComp,
            genericComp
        };

        bool stripped = AndroidCorpsesNoRotMain.StripCorpseComps(corpseWithComps);
        Check(stripped, "StripCorpseComps: returns true when comps removed");
        Check(corpseWithComps.comps.Count == 2, "StripCorpseComps: exactly 2 components remain in list");
        Check(corpseWithComps.comps.Contains(forbiddableComp), "StripCorpseComps: preserves CompProperties_Forbiddable");
        Check(corpseWithComps.comps.Contains(genericComp), "StripCorpseComps: preserves generic CompProperties");
        Check(!corpseWithComps.comps.Any(c => c is CompProperties_Rottable), "StripCorpseComps: CompProperties_Rottable removed");
        Check(!corpseWithComps.comps.Any(c => c is CompProperties_SpawnerFilth), "StripCorpseComps: bile SpawnerFilth removed");

        // Idempotency: stripping already-stripped corpse does nothing and returns false
        bool strippedAgain = AndroidCorpsesNoRotMain.StripCorpseComps(corpseWithComps);
        Check(!strippedAgain, "StripCorpseComps: repeated call returns false (idempotent)");
        Check(corpseWithComps.comps.Count == 2, "StripCorpseComps: repeated call preserves comps count");

        // --- HARMONY POSTFIX LOGIC CHECKS ---
        // Null safety
        Patch_ThingDefGenerator_Corpses_GenerateCorpseDef.Postfix(null, null);
        Check(true, "Postfix: handles null arguments safely");

        // Organic / human corpse: postfix must NOT strip comps
        var humanCorpseDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        humanCorpseDef.defName = "Corpse_Human";
        var humanRottable = new CompProperties_Rottable { daysToRotStart = 2.5f };
        humanCorpseDef.comps = new List<CompProperties> { humanRottable };
        Patch_ThingDefGenerator_Corpses_GenerateCorpseDef.Postfix(humanRaceDef, humanCorpseDef);
        Check(humanCorpseDef.comps.Count == 1 && humanCorpseDef.comps.Contains(humanRottable),
            "Postfix: does not strip rottable comp from human corpse");

        // Android corpse: postfix MUST strip comps
        var androidCorpseDef = (ThingDef)RuntimeHelpers.GetUninitializedObject(typeof(ThingDef));
        androidCorpseDef.defName = "Corpse_ChjDroid";
        var droidRottable = new CompProperties_Rottable { daysToRotStart = 2.5f };
        var droidBile = new CompProperties_SpawnerFilth { filthDef = bileDef, requiredRotStage = RotStage.Rotting };
        androidCorpseDef.comps = new List<CompProperties> { forbiddableComp, droidRottable, droidBile };
        Patch_ThingDefGenerator_Corpses_GenerateCorpseDef.Postfix(droidRaceDef, androidCorpseDef);
        Check(androidCorpseDef.comps.Count == 1 && androidCorpseDef.comps.Contains(forbiddableComp),
            "Postfix: strips rottable and bile comps from droid corpse");

        // --- STARTUP DEF PASS METHOD CHECK ---
        int startupDefsStripped = AndroidCorpsesNoRotMain.StripAllAndroidCorpses();
        Check(startupDefsStripped >= 0, "StripAllAndroidCorpses: runs safely on def database");

        Console.WriteLine(failures == 0 ? "ALL TESTS PASSED" : failures + " TEST(S) FAILED");
        return failures == 0 ? 0 : 1;
    }
}
