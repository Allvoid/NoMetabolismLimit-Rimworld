using HarmonyLib;
using RimWorld;
using Verse;

namespace NoMetabolismLimit
{
    /// <summary>
    /// Keeps randomly generated genepacks inside the vanilla biostat range.
    /// Widening GeneTuning.BiostatRange lifts the cap everywhere at once, which is
    /// what we want for anything the player does deliberately - but world generation
    /// would otherwise start handing out genepacks no vanilla game could produce.
    /// </summary>
    [HarmonyPatch(typeof(GeneSet), nameof(GeneSet.CanAddGeneDuringGeneration))]
    public static class GeneSet_CanAddGeneDuringGeneration_Patch
    {
        private static void Postfix(GeneSet __instance, GeneDef gene, ref bool __result)
        {
            if (!__result)
            {
                return;
            }

            NoMetabolismLimitSettings settings = NoMetabolismLimitMod.Settings;
            if (!settings.removeCap || settings.allowInWorldGeneration)
            {
                return;
            }

            // Mirrors the vanilla guard: the first gene of a set is always allowed.
            if (__instance.GenesListForReading.Count == 0)
            {
                return;
            }

            // IntRange.Includes is internal to Assembly-CSharp, so compare directly.
            IntRange vanilla = GeneTuningOverride.VanillaRange;
            int total = gene.biostatMet + __instance.MetabolismTotal;
            if (total < vanilla.TrueMin || total > vanilla.TrueMax)
            {
                __result = false;
            }
        }
    }

    /// <summary>
    /// Optional: restores the complexity bypass the previous build had as a side effect
    /// of skipping Dialog_CreateXenogerm.CanAccept entirely.
    /// </summary>
    [HarmonyPatch(typeof(Building_GeneAssembler), nameof(Building_GeneAssembler.MaxComplexity))]
    public static class Building_GeneAssembler_MaxComplexity_Patch
    {
        private const int Unlimited = 9999;

        private static void Postfix(ref int __result)
        {
            if (NoMetabolismLimitMod.Settings.bypassComplexityLimit)
            {
                __result = Unlimited;
            }
        }
    }
}
