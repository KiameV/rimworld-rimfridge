using Verse;
using System.Collections.Generic;

namespace RimFridge
{
    public class FridgeCache : MapComponent
    {
        private const string COULD_NOT_FIND_MAP_COMP = "unable to find fridge grid in map";

        private Dictionary<IntVec3, CompRefrigerator> FridgeGrid = new Dictionary<IntVec3, CompRefrigerator>();

        public FridgeCache(Map map) : base(map) { }

        public bool HasFridgeAt(IntVec3 cell)
        {
            return this.FridgeGrid.ContainsKey(cell);
        }

        public static FridgeCache GetFridgeCache(Map map)
        {
            if (map != null)
            {
                foreach (var c in map.components)
                    if (c is FridgeCache fc)
                        return fc;
                Log.Error(COULD_NOT_FIND_MAP_COMP);//, COULD_NOT_FIND_MAP_COMP.GetHashCode());
            }
            return null;
        }

        public static void AddFridge(CompRefrigerator comp, Map map)
        {
            var c = GetFridgeCache(map);
            if (c != null)
            {
                foreach (IntVec3 cell in GenAdj.OccupiedRect(comp.parent))
                {
                    c.FridgeGrid[cell] = comp;
                }
            }
        }

        public static bool TryGetFridge(IntVec3 cell, Map map, out CompRefrigerator comp)
        {
            var c = GetFridgeCache(map);
            if (c != null)
            {
                return c.FridgeGrid.TryGetValue(cell, out comp);
            }
            comp = null;
            return false;
        }

        public static void RemoveFridge(CompRefrigerator comp, Map map)
        {
            var c = GetFridgeCache(map);
            if (c != null)
            {
                foreach (IntVec3 cell in GenAdj.OccupiedRect(comp.parent))
                {
                    c.FridgeGrid.Remove(cell);
                }
            }
        }

        public override void ExposeData()
        {
            base.ExposeData();
        }
    }
}
