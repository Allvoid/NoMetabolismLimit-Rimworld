using System;
using System.Collections.Generic;
using System.Reflection;
using RimWorld;
using UnityEngine;
using Verse;

namespace NoMetabolismLimit
{
    /// <summary>
    /// Rewrites the two pieces of vanilla tuning this mod cares about:
    ///   GeneTuning.BiostatRange                          - the -5/+5 cap
    ///   GeneTuning.MetabolismToFoodConsumptionFactorCurve - hunger per metabolism point
    ///
    /// Vanilla clamps the curve at its end points, which is why hunger stops at
    /// x225% no matter how far past -5 the metabolism goes. Extending the curve
    /// instead of patching its callers keeps hunger, the gene card readouts and
    /// the xenogerm implant warning in agreement, since all three read this curve.
    /// </summary>
    public static class GeneTuningOverride
    {
        // Exact curve points are emitted down to here; everything below is covered
        // by a single collinear anchor so Evaluate() never runs out of curve.
        private const int ExactPointsDownTo = -30;
        private const int AnchorMet = -1000;
        private const int UnlimitedBiostat = 9999;
        private const float MinSlope = 0.001f;

        private static IntRange vanillaRange;
        private static SimpleCurve vanillaCurve;
        private static List<CurvePoint> vanillaPoints;
        private static FieldInfo biostatRangeField;
        private static bool initialized;

        public static IntRange VanillaRange { get { return vanillaRange; } }

        private static NoMetabolismLimitSettings Settings { get { return NoMetabolismLimitMod.Settings; } }

        public static void Initialize()
        {
            if (initialized)
            {
                return;
            }

            biostatRangeField = typeof(GeneTuning).GetField("BiostatRange", BindingFlags.Public | BindingFlags.Static);
            if (biostatRangeField == null)
            {
                Log.Error("[NoMetabolismLimit] GeneTuning.BiostatRange not found - the metabolism cap will stay in place.");
            }

            vanillaRange = GeneTuning.BiostatRange;
            vanillaPoints = new List<CurvePoint>(GeneTuning.MetabolismToFoodConsumptionFactorCurve.Points);
            vanillaCurve = new SimpleCurve(vanillaPoints);
            initialized = true;

            WarnIfVanillaSlopesChanged();
            Apply();
        }

        /// <summary>Re-reads the settings and rewrites the range and the curve.</summary>
        public static void Apply()
        {
            if (!initialized)
            {
                return;
            }
            ApplyBiostatRange();
            ApplyHungerCurve();
        }

        /// <summary>
        /// The single source of truth for "how hungry is a pawn at this metabolism".
        /// ApplyHungerCurve() just materialises this as curve points, and the settings
        /// window uses it to preview values before anything is applied.
        /// </summary>
        public static float FoodConsumptionFactorAt(int met)
        {
            if (!initialized)
            {
                return 1f;
            }

            int capMin = vanillaRange.TrueMin;
            int capMax = vanillaRange.TrueMax;

            if (met >= capMin && met <= capMax)
            {
                return vanillaCurve.Evaluate(met);
            }

            NoMetabolismLimitSettings settings = Settings;

            if (met < capMin)
            {
                float atCap = vanillaCurve.Evaluate(capMin);
                if (!settings.scaleHungerBeyondCap)
                {
                    return atCap;
                }
                return atCap + (capMin - met) * Mathf.Max(MinSlope, settings.hungerPerPointBelowCap);
            }

            float atMax = vanillaCurve.Evaluate(capMax);
            if (!settings.scaleHungerBeyondCap || !settings.extendPositiveBeyondCap)
            {
                return atMax;
            }
            // Floored at zero: a negative factor would make pawns gain nutrition forever.
            return Mathf.Max(0f, atMax - (met - capMax) * Mathf.Max(MinSlope, settings.hungerPerPointAboveCap));
        }

        /// <summary>Metabolism at which hunger reaches 0%, or null if it never does.</summary>
        public static int? MetabolismForZeroHunger()
        {
            if (!initialized)
            {
                return null;
            }
            NoMetabolismLimitSettings settings = Settings;
            if (!settings.scaleHungerBeyondCap || !settings.extendPositiveBeyondCap)
            {
                return null;
            }
            int capMax = vanillaRange.TrueMax;
            float atMax = vanillaCurve.Evaluate(capMax);
            return capMax + Mathf.CeilToInt(atMax / Mathf.Max(MinSlope, settings.hungerPerPointAboveCap));
        }

        private static void ApplyBiostatRange()
        {
            if (biostatRangeField == null)
            {
                return;
            }

            IntRange target = Settings.removeCap
                ? new IntRange(-UnlimitedBiostat, UnlimitedBiostat)
                : vanillaRange;

            IntRange current = GeneTuning.BiostatRange;
            if (current.min == target.min && current.max == target.max)
            {
                return;
            }

            try
            {
                biostatRangeField.SetValue(null, target);
            }
            catch (Exception ex)
            {
                Log.Error("[NoMetabolismLimit] Could not write GeneTuning.BiostatRange: " + ex);
                return;
            }

            // BiostatRange is static readonly; verify the write actually landed
            // rather than trusting reflection to have done it.
            IntRange written = GeneTuning.BiostatRange;
            if (written.min != target.min || written.max != target.max)
            {
                Log.Error("[NoMetabolismLimit] GeneTuning.BiostatRange did not change (still "
                    + written.min + ".." + written.max + ") - the metabolism cap is still active.");
            }
        }

        private static void ApplyHungerCurve()
        {
            SimpleCurve curve = GeneTuning.MetabolismToFoodConsumptionFactorCurve;

            if (!Settings.scaleHungerBeyondCap)
            {
                curve.SetPoints(vanillaPoints);
                return;
            }

            List<CurvePoint> points = new List<CurvePoint>(vanillaPoints);
            int capMin = vanillaRange.TrueMin;
            int capMax = vanillaRange.TrueMax;

            for (int met = capMin - 1; met >= ExactPointsDownTo; met--)
            {
                points.Add(new CurvePoint(met, FoodConsumptionFactorAt(met)));
            }
            points.Add(new CurvePoint(AnchorMet, FoodConsumptionFactorAt(AnchorMet)));

            if (Settings.extendPositiveBeyondCap)
            {
                // One point where hunger hits zero is enough - Evaluate() clamps to the
                // last point beyond it, which is exactly the "no hunger at all" behaviour.
                float atMax = vanillaCurve.Evaluate(capMax);
                float slope = Mathf.Max(MinSlope, Settings.hungerPerPointAboveCap);
                points.Add(new CurvePoint(capMax + atMax / slope, 0f));
            }

            curve.SetPoints(points);
        }

        private static void WarnIfVanillaSlopesChanged()
        {
            int capMin = vanillaRange.TrueMin;
            int capMax = vanillaRange.TrueMax;
            float below = vanillaCurve.Evaluate(capMin) - vanillaCurve.Evaluate(capMin + 1);
            float above = vanillaCurve.Evaluate(capMax - 1) - vanillaCurve.Evaluate(capMax);

            if (Mathf.Abs(below - NoMetabolismLimitSettings.VanillaHungerPerPointBelowCap) > 0.001f
                || Mathf.Abs(above - NoMetabolismLimitSettings.VanillaHungerPerPointAboveCap) > 0.001f)
            {
                Log.Warning("[NoMetabolismLimit] Vanilla hunger curve slopes changed (below cap "
                    + below.ToString("0.###") + "/pt, above cap " + above.ToString("0.###")
                    + "/pt). The defaults in mod settings no longer match vanilla - reset them if you want the vanilla lines continued.");
            }
        }

        public static string StateDescription()
        {
            NoMetabolismLimitSettings settings = Settings;
            string state = settings.removeCap
                ? "cap removed (" + GeneTuning.BiostatRange.min + ".." + GeneTuning.BiostatRange.max + ")"
                : "cap left at vanilla";

            if (!settings.scaleHungerBeyondCap)
            {
                return state + ", hunger scaling off";
            }

            string hunger = ", hunger scaling on (" + settings.hungerPerPointBelowCap.ToStringPercent() + "/pt below cap";
            if (settings.extendPositiveBeyondCap)
            {
                int? zero = MetabolismForZeroHunger();
                hunger += ", " + settings.hungerPerPointAboveCap.ToStringPercent() + "/pt above cap"
                    + (zero.HasValue ? ", 0% at +" + zero.Value : "");
            }
            return state + hunger + ")";
        }
    }
}
