using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using RimWorld;
using Verse;

namespace AndroidCorpsesNoRot
{
    /// <summary>
    /// Mod entry point and def-stripping logic. Strips CompProperties_Rottable and
    /// corpse-bile CompProperties_SpawnerFilth from android corpse defs.
    /// </summary>
    [StaticConstructorOnStartup]
    public static class AndroidCorpsesNoRotMain
    {
        public const string HarmonyId = "ertzfiend.androidcorpsesnorot";
        public const string MechanicalPropertiesType = "Androids.MechanicalPawnProperties";
        public const string FleshTypeDefName = "ChJDroid";
        public const string BileDefName = "Filth_CorpseBile";

        static AndroidCorpsesNoRotMain()
        {
            var harmony = new Harmony(HarmonyId);
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            int strippedCount = StripAllAndroidCorpses();
            Log.Message($"[AndroidCorpsesNoRot] Initialized. Stripped rotting components from {strippedCount} android corpse def(s).");
        }

        /// <summary>
        /// Exact classification (no substrings):
        /// 1. Race def has DefModExtension of type Androids.MechanicalPawnProperties
        /// 2. Race fleshType defName == 'ChJDroid'
        /// Converted-organics (ChjAndroidLike hediff on human pawn) are intentionally excluded.
        /// </summary>
        public static bool IsAndroidRace(ThingDef raceDef)
        {
            if (raceDef == null || raceDef.race == null)
                return false;

            var extensions = raceDef.modExtensions;
            if (extensions != null)
            {
                for (int i = 0; i < extensions.Count; i++)
                {
                    var ext = extensions[i];
                    if (ext != null && string.Equals(ext.GetType().FullName, MechanicalPropertiesType, StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            if (raceDef.race.FleshType != null && string.Equals(raceDef.race.FleshType.defName, FleshTypeDefName, StringComparison.Ordinal))
            {
                return true;
            }

            return false;
        }

        /// <summary>
        /// Identifies rotting-related components that should be stripped from android corpses:
        /// - CompProperties_Rottable (decay and decomposition damage)
        /// - CompProperties_SpawnerFilth for corpse bile (Filth_CorpseBile / RotStage.Rotting)
        /// </summary>
        public static bool ShouldRemoveComp(CompProperties comp)
        {
            if (comp == null)
                return false;

            if (comp is CompProperties_Rottable)
                return true;

            if (comp is CompProperties_SpawnerFilth filth)
            {
                if (filth.filthDef != null && string.Equals(filth.filthDef.defName, BileDefName, StringComparison.Ordinal))
                    return true;

                if (filth.filthDef == ThingDefOf.Filth_CorpseBile)
                    return true;

                if (filth.requiredRotStage.HasValue && filth.requiredRotStage.Value == RotStage.Rotting)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Strips CompProperties_Rottable and corpse-bile CompProperties_SpawnerFilth
        /// from the specified corpse def. Returns true if any components were removed.
        /// </summary>
        public static bool StripCorpseComps(ThingDef corpseDef)
        {
            if (corpseDef?.comps == null || corpseDef.comps.Count == 0)
                return false;

            int removed = corpseDef.comps.RemoveAll(ShouldRemoveComp);
            return removed > 0;
        }

        /// <summary>
        /// Def-pass on startup: scans all defs in DefDatabase and strips rotting components
        /// from all android corpses generated before startup.
        /// </summary>
        public static int StripAllAndroidCorpses()
        {
            var allDefs = DefDatabase<ThingDef>.AllDefsListForReading;
            if (allDefs == null)
                return 0;

            var processed = new HashSet<ThingDef>();
            int strippedDefCount = 0;

            for (int i = 0; i < allDefs.Count; i++)
            {
                var def = allDefs[i];
                if (def == null)
                    continue;

                // Path 1: From race pawnDef to its corpseDef
                if (def.race != null && IsAndroidRace(def))
                {
                    var corpse = def.race.corpseDef;
                    if (corpse != null && processed.Add(corpse))
                    {
                        if (StripCorpseComps(corpse))
                            strippedDefCount++;
                    }
                }

                // Path 2: Directly on corpse def if linked back via ingestible.sourceDef
                if (def.IsCorpse && def.ingestible?.sourceDef != null && IsAndroidRace(def.ingestible.sourceDef))
                {
                    if (processed.Add(def))
                    {
                        if (StripCorpseComps(def))
                            strippedDefCount++;
                    }
                }
            }

            return strippedDefCount;
        }
    }
}
