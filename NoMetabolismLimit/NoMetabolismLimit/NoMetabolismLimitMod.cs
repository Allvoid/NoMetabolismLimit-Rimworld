using UnityEngine;
using Verse;

namespace NoMetabolismLimit
{
    public class NoMetabolismLimitMod : Mod
    {
        private static NoMetabolismLimitSettings settings;

        public static NoMetabolismLimitSettings Settings
        {
            get { return settings ?? (settings = new NoMetabolismLimitSettings()); }
        }

        public NoMetabolismLimitMod(ModContentPack content) : base(content)
        {
            settings = GetSettings<NoMetabolismLimitSettings>();
        }

        public override string SettingsCategory()
        {
            return "NML_ModTitle".Translate();
        }

        public override void WriteSettings()
        {
            base.WriteSettings();
            GeneTuningOverride.Apply();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            NoMetabolismLimitSettings s = Settings;
            Listing_Standard list = new Listing_Standard();
            list.Begin(inRect);

            Section(list, "NML_SectionCap");
            list.CheckboxLabeled("NML_RemoveCap".Translate(), ref s.removeCap, "NML_RemoveCapDesc".Translate());
            if (s.removeCap)
            {
                list.CheckboxLabeled("NML_BypassComplexity".Translate(), ref s.bypassComplexityLimit,
                    "NML_BypassComplexityDesc".Translate());
                list.CheckboxLabeled("NML_AllowInWorldGen".Translate(), ref s.allowInWorldGeneration,
                    "NML_AllowInWorldGenDesc".Translate());
            }

            list.GapLine();
            Section(list, "NML_SectionHunger");
            list.CheckboxLabeled("NML_ScaleHunger".Translate(), ref s.scaleHungerBeyondCap,
                "NML_ScaleHungerDesc".Translate());

            if (s.scaleHungerBeyondCap)
            {
                s.hungerPerPointBelowCap = PercentSlider(list, "NML_PerPointBelow", s.hungerPerPointBelowCap, 0.05f, 1f);

                list.CheckboxLabeled("NML_ExtendPositive".Translate(), ref s.extendPositiveBeyondCap,
                    "NML_ExtendPositiveDesc".Translate());
                if (s.extendPositiveBeyondCap)
                {
                    s.hungerPerPointAboveCap = PercentSlider(list, "NML_PerPointAbove", s.hungerPerPointAboveCap, 0.01f, 0.5f);
                }

                list.Gap();
                list.Label("NML_Preview".Translate() + ":");
                list.Label("   " + PreviewLine());

                int? zero = GeneTuningOverride.MetabolismForZeroHunger();
                if (zero.HasValue)
                {
                    list.Label("   " + "NML_ZeroHungerAt".Translate(zero.Value.ToString()));
                }
            }

            list.Gap();
            if (list.ButtonText("NML_Reset".Translate()))
            {
                s.ResetToDefaults();
            }

            list.End();
        }

        private static void Section(Listing_Standard list, string key)
        {
            Text.Font = GameFont.Medium;
            list.Label(key.Translate());
            Text.Font = GameFont.Small;
        }

        private static float PercentSlider(Listing_Standard list, string key, float value, float min, float max)
        {
            list.Label(key.Translate() + ": " + value.ToStringPercent());
            // 1% steps - the sliders exist for tuning, not for arbitrary precision.
            return Mathf.Round(list.Slider(value, min, max) * 100f) / 100f;
        }

        private static string PreviewLine()
        {
            int[] samples = { -10, -8, -6, 6, 8, 10 };
            string line = "";
            for (int i = 0; i < samples.Length; i++)
            {
                if (i > 0)
                {
                    line += "    ";
                }
                line += samples[i].ToStringWithSign() + " = x"
                    + GeneTuningOverride.FoodConsumptionFactorAt(samples[i]).ToStringPercent();
            }
            return line;
        }
    }
}
