using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using Verse.AI;

namespace RimFridge
{
    [StaticConstructorOnStartup]
    class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var h = new Harmony("com.rimfridge.rimworld.mod");
            h.PatchAll(Assembly.GetExecutingAssembly());

            Log.Message("RimFridge Harmony Patches:" + Environment.NewLine +
                        "  Prefix:" + Environment.NewLine +
                        "    ReachabilityUtility.CanReach - So pawns can get items in Wall-Fridges" + Environment.NewLine +
                        "  Postfix:" + Environment.NewLine +
                        "    GameComponentUtility.StartedNewGame - Apply power settings at start" + Environment.NewLine +
                        "    GameComponentUtility.LoadedGame - Apply power settings on load" + Environment.NewLine +
                        "    GenTemperature.TryGetTemperatureForCell - Overrides room temperature within the cells of the RimFridge" + Environment.NewLine +
                        "    TradeShip.ColonyThingsWillingToBuy - Add items stored inside a wall-fridge to the trade list if in a room with an orbital beacon" + Environment.NewLine +
                        "    FoodUtility.TryFindBestFoodSourceFor - Allow Prisoners to eat food from fridges" + Environment.NewLine +
                        "    TradeShip.ColonyThingsWillingToBuy");
        }
    }

    [HarmonyPatch(typeof(ReachabilityUtility), "CanReach")]
    static class Patch_ReachabilityUtility_CanReach
    {
        static bool Prefix(ref bool __result, Pawn pawn, LocalTargetInfo dest, PathEndMode peMode, Danger maxDanger, bool canBash, TraverseMode mode)
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
    static class Patch_GameComponentUtility_StartedNewGame
    {
        static void Postfix()
        {
            RimFridgeSettingsUtil.ApplyFactor(Settings.PowerFactor.AsFloat);
        }
    }

    [HarmonyPatch(typeof(GameComponentUtility), "LoadedGame")]
    static class Patch_GameComponentUtility_LoadedGame
    {
        static void Postfix()
        {
            RimFridgeSettingsUtil.ApplyFactor(Settings.PowerFactor.AsFloat);
        }
    }

    [HarmonyPatch(typeof(GenTemperature), "TryGetTemperatureForCell")]
    static class Patch_GenTemperature_TryGetDirectAirTemperatureForCell
    {
        static void Postfix(bool __result, ref IntVec3 c, ref Map map, ref float tempResult)
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

    [HarmonyPatch(typeof(TradeUtility), "PlayerSellableNow")]
    static class Patch_PlayerSellableNow
    {
        static void Postfix(bool __result, Thing t)
        {
            Log.Message($"{__result} -- {t.def.defName}");
        }
    }

        [HarmonyPatch(typeof(TradeShip), "ColonyThingsWillingToBuy")]
    static class Patch_PassingShip_TryOpenComms
    {
        private readonly static HashSet<ThingDef> RimFridgeDefs = new HashSet<ThingDef>();
        public static bool IsRimFridge(ThingDef def)
        {
            if (RimFridgeDefs.Count == 0)
            {
                DefDatabase<ThingDef>.AllDefsListForReading.ForEach(d =>
                {
                    switch (d.defName)
                    {
                        case "RimFridge_WallRefrigerator":
                        case "RimFridge_SingleWallRefrigerator":
                        case "RimFridge_Refrigerator":
                        case "RimFridge_SingleRefrigerator":
                        case "RimFridge_QuadRefrigerator":
                            RimFridgeDefs.Add(d);
                            break;
                    }
                });
            }
            if (def == null)
                return false;
            return RimFridgeDefs.Contains(def);
        }

        // Before an orbital trade
        static void Postfix(ref IEnumerable<Thing> __result, Pawn playerNegotiator)
        {
            List<Thing> things = null;
            Log.Message(playerNegotiator.Name.ToStringFull);
            if (playerNegotiator != null && playerNegotiator.Map != null)
            {
                foreach (Thing thing in playerNegotiator.Map.listerBuildings.allBuildingsColonist)
                {
                    if (IsRimFridge(thing?.def))
                    {
                        var storage = thing as Building_Storage;
                        foreach (IntVec3 cell in storage.AllSlotCells())
                        {
                            foreach (Thing refrigeratedItem in playerNegotiator.Map.thingGrid.ThingsAt(cell))
                            {
                                if (storage.settings.AllowedToAccept(refrigeratedItem))
                                {
                                    if (things == null)
                                        things = new List<Thing>(__result);
                                    things.Add(refrigeratedItem);
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            if (things != null)
                __result = things;
        }
    }

    [HarmonyPatch(typeof(FoodUtility), "TryFindBestFoodSourceFor")]
    static class Patch_FoodUtility_TryFindBestFoodSourceFor
    {
        static void Postfix(ref bool __result, Pawn getter, Pawn eater, ref Thing foodSource, ref ThingDef foodDef, bool canRefillDispenser, bool canUseInventory, bool allowForbidden, bool allowCorpse, bool allowSociallyImproper, bool allowHarvest, bool forceScanWholeMap)
        {
            if (__result == false &&
                getter.Map != null &&
                getter.Faction != Faction.OfPlayer &&
                getter == eater &&
                getter.RaceProps.ToolUser &&
                getter.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                Room prison = getter.Position.GetRoomOrAdjacent(getter.Map);
                if (prison != null && prison.isPrisonCell)
                {
                    foreach (Thing t in prison.ContainedAndAdjacentThings)
                    {
                        if (t.Map != null &&
                            !t.IsForbidden(getter) &&
                            t is Building_Storage storage)
                        {
                            foreach (IntVec3 cell in storage.AllSlotCells())
                            {
                                foreach (Thing possibleFood in t.Map.thingGrid.ThingsAt(cell))
                                {
                                    if (!possibleFood.IsForbidden(getter) &&
                                        storage.Map.reservationManager.CanReserve(getter, new LocalTargetInfo(possibleFood)) &&
                                        getter.RaceProps.CanEverEat(possibleFood))
                                    {
                                        __result = true;
                                        foodSource = possibleFood;
                                        foodDef = possibleFood.def;
                                        return;
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
