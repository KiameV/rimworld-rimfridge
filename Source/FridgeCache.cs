using Verse;
using System.Collections.Generic;
using HarmonyLib;

namespace RimFridge
{
    [StaticConstructorOnStartup]
    public static class FridgeCache
    {
        public static Dictionary<IntVec3, CompRefrigerator>[] FridgeGrid = new Dictionary<IntVec3, CompRefrigerator>[10];

        static FridgeCache()
        {
            for (int i = 0; i < 10; i++) FridgeGrid[i] = new Dictionary<IntVec3, CompRefrigerator>(200);
        }
    }


    [HarmonyPatch(typeof(Map), nameof(Map.FinalizeInit))]
    static class Patch_ClearCache
    {
        static void Postfix(Map __instance)
        {
            FridgeCache.FridgeGrid[__instance.Index].Clear();
        }
    }
}
