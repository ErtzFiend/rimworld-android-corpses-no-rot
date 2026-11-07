using HarmonyLib;
using RimWorld;
using Verse;

namespace AndroidCorpsesNoRot
{
    /// <summary>
    /// Postfix on RimWorld.ThingDefGenerator_Corpses.GenerateCorpseDef.
    /// Ensures that any dynamically generated or hot-reloaded android corpse defs
    /// immediately have their rotting and corpse-bile components stripped.
    /// </summary>
    [HarmonyPatch(typeof(ThingDefGenerator_Corpses), "GenerateCorpseDef", new[] { typeof(ThingDef), typeof(bool), typeof(string) })]
    public static class Patch_ThingDefGenerator_Corpses_GenerateCorpseDef
    {
        public static void Postfix(ThingDef pawnDef, ThingDef __result)
        {
            if (__result == null || pawnDef == null)
                return;

            if (AndroidCorpsesNoRotMain.IsAndroidRace(pawnDef))
            {
                AndroidCorpsesNoRotMain.StripCorpseComps(__result);
            }
        }
    }
}
