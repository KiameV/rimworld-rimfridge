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
            for (int i = 0; i < 10; i++) FridgeGrid[i] = new Dictionary<IntVec3, CompRefrigerator>();
        }

        public static void AddFridgeCompToCache(CompRefrigerator comp, Map map)
        {
            var index = map.Index;
            foreach (IntVec3 cell in GenAdj.OccupiedRect(comp.parent))
                FridgeCache.FridgeGrid[index][cell] = comp;
        }
    }
}
