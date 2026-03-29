using System;
using HarmonyLib;
using RimWorld;
using Verse;
using UnityEngine;
using System.Collections.Generic;

namespace NoMetabolismLimit
{
    [StaticConstructorOnStartup]
    public static class Loader
    {
        static Loader()
        {
            try
            {
                var harmony = new Harmony("com.Allvoid.nometa");
                harmony.PatchAll();
                Log.Message("[NoMetabolismLimit] Harmony patches applied");
            }
            catch (Exception ex)
            {
                Log.Error("[NoMetabolismLimit] Harmony initialization failed: " + ex);
            }
        }
    }

    // Снимаем ограничение метаболизма у GeneSet
    [HarmonyPatch(typeof(GeneSet), "MetabolismTotal", MethodType.Getter)]
    public static class MetabolismTotalPatch
    {
        static void Postfix(GeneSet __instance, ref int __result)
        {
            __result = 0;
        }
    }

    // Обходим проверку CanAccept() в Dialog_CreateXenogerm
    [HarmonyPatch(typeof(Dialog_CreateXenogerm), "CanAccept")]
    public static class CanAcceptPatch
    {
        static bool Prefix(Dialog_CreateXenogerm __instance, ref bool __result)
        {
            // Всегда можно принимать (игнорируем лимиты)
            __result = true;
            return false; // не вызываем оригинальный метод
        }
    }

    // Для логирования DrawGenes, чтобы видеть, что патч работает
    [HarmonyPatch(typeof(Dialog_CreateXenogerm), "DrawGenes")]
    public static class DrawGenesPatch
    {
        static void Postfix(Dialog_CreateXenogerm __instance, Rect rect)
        {
            Log.Message("[NoMetabolismLimit] DrawGenes_Postfix called");
        }
    }
}