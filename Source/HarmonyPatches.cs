using Harmony;
using RimWorld;
using System.Collections.Generic;
using System.Reflection;
using Verse;
using Verse.AI;

namespace RimFridge
{
    [StaticConstructorOnStartup]
    internal class HarmonyPatches
    {
        static HarmonyPatches()
        {
            HarmonyInstance.Create("com.rimfridge.rimworld.mod").PatchAll(Assembly.GetExecutingAssembly());

            Log.Message("RimFridge Harmony Patches:");
            Log.Message("  Prefix:");
            Log.Message("    ReachabilityUtility.CanReach - So pawns can get items in Wall-Fridges");
            Log.Message("  Postfix:");
            Log.Message("    GameComponentUtility.StartedNewGame - Apply power settings at start");
            Log.Message("    GameComponentUtility.LoadedGame - Apply power settings on load");
            Log.Message("    GenTemperature.TryGetTemperatureForCell - Overrides room temperature within the cells of the RimFridge");
            Log.Message("    TradeShip.ColonyThingsWillingToBuy - Add items stored inside a wall-fridge to the trade list if in a room with an orbital beacon");
        }
    }

    [HarmonyPatch(typeof(ReachabilityUtility), "CanReach")]
    internal static class Patch_ReachabilityUtility_CanReach
    {
        private static bool Prefix(ref bool __result, Pawn pawn, LocalTargetInfo dest, PathEndMode peMode, Danger maxDanger, bool canBash, TraverseMode mode)
        {
            if (dest != null && dest.Thing != null && dest.Thing.def.category == ThingCategory.Item)
            {
                foreach (Thing thing in Current.Game.CurrentMap.thingGrid.ThingsAt(dest.Thing.Position))
                {
                    if (ThingCompUtility.TryGetComp<CompRefrigerator>(thing) != null)
                    {
                        peMode = PathEndMode.Touch;
                        __result = pawn.Spawned && pawn.Map.reachability.CanReach(pawn.Position, dest, peMode, TraverseParms.For(pawn, maxDanger, mode, canBash));
                        return false;
                    }
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(GameComponentUtility), "StartedNewGame")]
    internal static class Patch_GameComponentUtility_StartedNewGame
    {
        private static void Postfix()
        {
            RimFridgeSettingsUtil.ApplyFactor(Settings.PowerFactor.AsFloat);
        }
    }

    [HarmonyPatch(typeof(GameComponentUtility), "LoadedGame")]
    internal static class Patch_GameComponentUtility_LoadedGame
    {
        private static void Postfix()
        {
            RimFridgeSettingsUtil.ApplyFactor(Settings.PowerFactor.AsFloat);
        }
    }

    [HarmonyPatch(typeof(GenTemperature), "TryGetTemperatureForCell")]
    internal static class Patch_GenTemperature_TryGetDirectAirTemperatureForCell
    {
        private static void Postfix(bool __result, ref IntVec3 c, ref Map map, ref float tempResult)
        {
            IEnumerable<Thing> things = map?.thingGrid.ThingsAt(c);
            if (things != null)
            {
                foreach (Thing thing in things)
                {
                    CompRefrigerator fridge = ThingCompUtility.TryGetComp<CompRefrigerator>(thing);
                    if(fridge !=null)
                    {
                        tempResult = fridge.currentTemp;
                        __result = true;
                    }                    
                }
            }
        }
    }

    [HarmonyPatch(typeof(TradeShip), "ColonyThingsWillingToBuy")]
    internal static class Patch_PassingShip_TryOpenComms
    {
        // Before an orbital trade
        private static void Postfix(ref IEnumerable<Thing> __result, Pawn playerNegotiator)
        {
            if (playerNegotiator != null && playerNegotiator.Map != null)
            {
                List<Thing> result = null;
                foreach (Thing thing in playerNegotiator.Map.listerThings.AllThings)
                {
                    if (ThingCompUtility.TryGetComp<CompRefrigerator>(thing) != null && thing.def.passability == Traversability.Impassable)
                    {
                        foreach (IntVec3 cell in ((Building_Storage)thing).AllSlotCells())
                        {
                            foreach (Thing refrigeratedItem in playerNegotiator.Map.thingGrid.ThingsAt(cell))
                            {
                                if (((Building_Storage)thing).settings.AllowedToAccept(refrigeratedItem))
                                {
                                    if (result == null)
                                        result = new List<Thing>(__result);
                                    result.Add(refrigeratedItem);
                                    break;
                                }
                            }
                        }
                    }
                }
                if (result != null)
                    __result = result;
               
            }
        }
    }
}
