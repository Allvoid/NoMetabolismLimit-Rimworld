using Verse;

namespace NoMetabolismLimit
{
    public class NoMetabolismLimitSettings : ModSettings
    {
        // Vanilla slopes of GeneTuning.MetabolismToFoodConsumptionFactorCurve.
        // Below the cap the curve loses 0.25 hunger per metabolism point,
        // above it 0.10. Using these as defaults means the extension is just
        // the vanilla lines continued, not a new formula.
        public const float VanillaHungerPerPointBelowCap = 0.25f;
        public const float VanillaHungerPerPointAboveCap = 0.10f;

        // Core feature: drop the -5/+5 biostat cap.
        public bool removeCap = true;

        // Random genepack generation stays vanilla unless this is on, so the
        // world does not fill up with extreme-metabolism genepacks.
        public bool allowInWorldGeneration = false;

        // The old build bypassed CanAccept wholesale, which also removed the
        // gene assembler's complexity limit. Off by default: this mod is about
        // metabolism. Turn on to get the old side effect back.
        public bool bypassComplexityLimit = false;

        // Hunger past the cap. Off keeps vanilla behaviour (x225% floor).
        public bool scaleHungerBeyondCap = false;
        public float hungerPerPointBelowCap = VanillaHungerPerPointBelowCap;

        public bool extendPositiveBeyondCap = true;
        public float hungerPerPointAboveCap = VanillaHungerPerPointAboveCap;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref removeCap, "removeCap", true);
            Scribe_Values.Look(ref allowInWorldGeneration, "allowInWorldGeneration", false);
            Scribe_Values.Look(ref bypassComplexityLimit, "bypassComplexityLimit", false);
            Scribe_Values.Look(ref scaleHungerBeyondCap, "scaleHungerBeyondCap", false);
            Scribe_Values.Look(ref hungerPerPointBelowCap, "hungerPerPointBelowCap", VanillaHungerPerPointBelowCap);
            Scribe_Values.Look(ref extendPositiveBeyondCap, "extendPositiveBeyondCap", true);
            Scribe_Values.Look(ref hungerPerPointAboveCap, "hungerPerPointAboveCap", VanillaHungerPerPointAboveCap);
        }

        public void ResetToDefaults()
        {
            removeCap = true;
            allowInWorldGeneration = false;
            bypassComplexityLimit = false;
            scaleHungerBeyondCap = false;
            hungerPerPointBelowCap = VanillaHungerPerPointBelowCap;
            extendPositiveBeyondCap = true;
            hungerPerPointAboveCap = VanillaHungerPerPointAboveCap;
        }
    }
}
