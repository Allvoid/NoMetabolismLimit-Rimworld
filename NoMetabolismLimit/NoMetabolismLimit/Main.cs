using System;
using HarmonyLib;
using Verse;

namespace NoMetabolismLimit
{
    [StaticConstructorOnStartup]
    public static class Loader
    {
        static Loader()
        {
            try
            {
                new Harmony("com.Allvoid.nometa").PatchAll();
                GeneTuningOverride.Initialize();
                Log.Message("[NoMetabolismLimit] " + GeneTuningOverride.StateDescription());
            }
            catch (Exception ex)
            {
                Log.Error("[NoMetabolismLimit] Initialization failed: " + ex);
            }
        }
    }
}
