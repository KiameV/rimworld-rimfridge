using HarmonyLib;
using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using UnityEngine;
using Verse;
using Verse.AI;
using Verse.Sound;

namespace RimFridge
{
    [StaticConstructorOnStartup]
    class HarmonyPatches
    {
        static HarmonyPatches()
        {
            var h = new Harmony("com.rimfridge.rimworld.mod");
            h.PatchAll(Assembly.GetExecutingAssembly());
        }
    }

    [HarmonyPatch(typeof(ReachabilityUtility), "CanReach")]
    static class Patch_ReachabilityUtility_CanReach
    {
        static bool Prefix(ref bool __result, Pawn pawn, LocalTargetInfo dest, PathEndMode peMode, Danger maxDanger, bool canBashDoors, TraverseMode mode)
        {
            if (dest.Thing?.def.category == ThingCategory.Item)
            {
                foreach (Thing thing in Current.Game?.CurrentMap?.thingGrid?.ThingsAt(dest.Thing.Position))
                {
                    if (thing is RimFridge_Building)
                    {
                        peMode = PathEndMode.Touch;
                        __result = pawn?.Spawned == true && pawn.Map?.reachability.CanReach(pawn.Position, dest, peMode, TraverseParms.For(pawn, maxDanger, mode, canBashDoors)) == true;
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

    /*[HarmonyPriority(Priority.Last)]
    [HarmonyPatch(typeof(CompRottable), "Active", MethodType.Getter)]
    static class Patch_CompRottable_Freeze
    {
        static void Postfix(ref bool __result, ThingComp __instance)
        {
            if (__instance.parent?.Map == null)
                return;

            if (FridgeCache.TryGetFridge(__instance.parent.Position, __instance.parent.Map, out CompRefrigerator fridge) &&
                fridge != null && fridge.ShouldBeActive)
            {
                __result = false;
            }
        }
    }*/

    [HarmonyBefore(new string[]{"io.github.dametri.thermodynamicscore"})]
    [HarmonyPriority(Priority.First)]
    [HarmonyPatch(typeof(Thing), "AmbientTemperature", MethodType.Getter)]
    static class Patch_Thing_AmbientTemperature
    {
        static bool Prefix(Thing __instance, ref float __result)
        {
            Pawn p = __instance as Pawn;
            if ((p == null || p.Dead) && __instance.Map != null &&
                FridgeCache.TryGetFridge(__instance.Position, __instance.Map, out CompRefrigerator fridge) &&
                fridge != null)
            {
                __result = fridge.currentTemp;
                return false;
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(TradeShip), "ColonyThingsWillingToBuy")]
    static class Patch_PassingShip_TryOpenComms
    {
        // Before an orbital trade
        static void Postfix(ref IEnumerable<Thing> __result, Pawn playerNegotiator)
        {
            if (!Settings.ActAsBeacon)
                return;

            List<Thing> things = null;
            Log.Message(playerNegotiator.Name.ToStringFull);
            if (playerNegotiator != null && playerNegotiator.Map != null)
            {
                foreach (Thing thing in playerNegotiator.Map.listerBuildings.allBuildingsColonist)
                {
                    if (thing is RimFridge_Building storage)//IsRimFridge(thing?.def))
                    {
                        //var storage = thing as Building_Storage;
                        foreach (IntVec3 cell in storage.AllSlotCells())
                        {
                            foreach (Thing refrigeratedItem in playerNegotiator.Map.thingGrid.ThingsAt(cell))
                            {
                                if (storage.settings.AllowedToAccept(refrigeratedItem))
                                {
                                    if (things == null)
                                    {
                                        if (__result?.Count() == 0)
                                            things = new List<Thing>();
                                        else
                                            things = new List<Thing>(__result);
                                    }
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

    [HarmonyPatch(typeof(FoodUtility), "TryFindBestFoodSourceFor_NewTemp")]
    static class Patch_FoodUtility_TryFindBestFoodSourceFor
    {
        static void Postfix(ref bool __result, Pawn getter, Pawn eater, bool desperate, ref Thing foodSource, ref ThingDef foodDef, bool canRefillDispenser, bool canUseInventory, bool canUsePackAnimalInventory, bool allowForbidden, bool allowCorpse, bool allowSociallyImproper, bool allowHarvest, bool forceScanWholeMap, bool ignoreReservations, bool calculateWantedStackCount, FoodPreferability minPrefOverride)
        {
            if (__result == false &&
                getter.Map != null &&
                //getter.Faction != Faction.OfPlayer &&
                getter == eater &&
                getter.RaceProps.ToolUser &&
                getter.health.capacities.CapableOf(PawnCapacityDefOf.Manipulation))
            {
                Room prison = getter.Position.GetRoomOrAdjacent(getter.Map);
                if (prison != null && prison.IsPrisonCell)
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

    [HarmonyPatch(typeof(Building_NutrientPasteDispenser), "FindFeedInAnyHopper")]
    public static class Patch_Building_NutrientPasteDispenser_FindFeedInAnyHopper
    {
        static bool Prefix(Building_NutrientPasteDispenser __instance, ref Thing __result)
        {
            foreach (IntVec3 cell in __instance.AdjCellsCardinalInBounds)
            {
                Thing thing = null;
                Thing holder = null;
                foreach (Thing t in cell.GetThingList(__instance.Map))
                {
                    if (Building_NutrientPasteDispenser.IsAcceptableFeedstock(t.def))
                    {
                        thing = t;
                    }
                    if (t.def == ThingDefOf.Hopper || t is RimFridge_Building)
                    {
                        holder = t;
                    }
                }
                if (thing != null && holder != null)
                {
                    __result = thing;
                    return false;
                }
            }
            return false;
        }
    }

    [HarmonyPatch(typeof(Building_NutrientPasteDispenser), "HasEnoughFeedstockInHoppers")]
    public static class Patch_Building_NutrientPasteDispenser_HasEnoughFeedstockInHoppers
    {
        static bool Prefix(Building_NutrientPasteDispenser __instance, ref bool __result)
        {
            float num = 0f;
            for (int i = 0; i < __instance.AdjCellsCardinalInBounds.Count; i++)
            {
                IntVec3 c = __instance.AdjCellsCardinalInBounds[i];
                Thing thing = null;
                Thing thing2 = null;
                List<Thing> thingList = c.GetThingList(__instance.Map);
                for (int j = 0; j < thingList.Count; j++)
                {
                    Thing thing3 = thingList[j];
                    if (Building_NutrientPasteDispenser.IsAcceptableFeedstock(thing3.def))
                    {
                        thing = thing3;
                    }
                    if (thing3.def == ThingDefOf.Hopper || thing3 is RimFridge_Building)
                    {
                        thing2 = thing3;
                    }
                }
                if (thing != null && thing2 != null)
                {
                    num += (float)thing.stackCount * thing.GetStatValue(StatDefOf.Nutrition);
                }
                if (num >= __instance.def.building.nutritionCostPerDispense)
                {
                    __result = true;
                    return false;
                }
            }
            __result = false;
            return false;
        }
    }

    [HarmonyPatch(typeof(Alert_PasteDispenserNeedsHopper), "BadDispensers", MethodType.Getter)]
    public static class Patch_Alert_PasteDispenserNeedsHopper_BadDispensers_Getter
    {
        static bool Prefix(Alert_PasteDispenserNeedsHopper __instance, ref List<Thing> __result)
        {
            __result = (List<Thing>)__instance.GetType().GetField("badDispensersResult", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(__instance);
            __result.Clear();
            List<Map> maps = Find.Maps;
            for (int i = 0; i < maps.Count; i++)
            {
                foreach (Thing item in maps[i].listerThings.ThingsInGroup(ThingRequestGroup.FoodDispenser))
                {
                    bool flag = false;
                    ThingDef hopper = ThingDefOf.Hopper;
                    foreach (IntVec3 adjCellsCardinalInBound in ((Building_NutrientPasteDispenser)item).AdjCellsCardinalInBounds)
                    {
                        Thing edifice = adjCellsCardinalInBound.GetEdifice(item.Map);
                        if (edifice != null && (edifice.def == hopper || edifice is RimFridge_Building))
                        {
                            flag = true;
                            break;
                        }
                    }
                    if (!flag)
                    {
                        __result.Add(item);
                    }
                }
            }
            return false;
        }
    }

    /*
        [HarmonyPatch(typeof(Dialog_BillConfig), "DoWindowContents", new Type[] {typeof(Rect)})]
        public static class Patch_Dialog_BillConfig_DoWindowContents
        {
            public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
            {
                List<CodeInstruction> instructionList = instructions.ToList();
                FieldInfo billFI = typeof(Dialog_BillConfig).GetField("bill", BindingFlags.NonPublic | BindingFlags.Instance);

                bool found = false;
                for (int i = 0; i < instructionList.Count; ++i)
                {
                    if (instructionList[i].opcode == OpCodes.Ldsfld &&
                        instructionList[i].operand?.ToString() == "RimWorld.BillStoreModeDef SpecificStockpile")
                    {
                        found = true;

                        yield return new CodeInstruction(OpCodes.Ldfld, billFI);
                        yield return new CodeInstruction(OpCodes.Ldloc_S, 15);
                        yield return new CodeInstruction(OpCodes.Ldloc_S, 13);
                        yield return new CodeInstruction(
                            OpCodes.Call, 
                            typeof(Patch_Dialog_BillConfig_DoWindowContents).GetMethod(
                                nameof(Patch_Dialog_BillConfig_DoWindowContents.AddStorageBuildings), BindingFlags.Static | BindingFlags.NonPublic));
                    }
                    yield return instructionList[i];
                }

                if (!found)
                {
                    Log.Error("NOT FOUND!!!");
                }
            }

            private static bool CanPossiblyStoreInBuildingStorage(Bill_Production bill, Building_Storage s)
            {
                var recipe = bill.recipe;
                if (!recipe.WorkerCounter.CanCountProducts(bill))
                {
                    return true;
                }
                return s.GetStoreSettings().AllowedToAccept(recipe.products[0].thingDef);
            }

            private static void AddStorageBuildings(Bill_Production bill, BillStoreModeDef item, List<FloatMenuOption> list)
            {
                List<SlotGroup> allGroupsListInPriorityOrder = bill.billStack.billGiver.Map.haulDestinationManager.AllGroupsListInPriorityOrder;
                sb.AppendLine($"allGroupsListInPriorityOrder is null: {allGroupsListInPriorityOrder == null}");
                sb.AppendLine($"count is null: {allGroupsListInPriorityOrder.Count}");
                int count = allGroupsListInPriorityOrder.Count;
                for (int i = 0; i < count; i++)
                {
                    SlotGroup group = allGroupsListInPriorityOrder[i];
                    sb.AppendLine($"{i}   group is null: {group == null}");
                    sb.AppendLine($"{i}   parent is null: {group.parent == null}");

                    if (group.parent is Building_Storage s)
                    {
                        if (!CanPossiblyStoreInBuildingStorage(bill, s))
                        {
                            list.Add(new FloatMenuOption(string.Format("{0} ({1})", string.Format(item.LabelCap, group.parent.SlotYielderLabel()), "IncompatibleLower".Translate()), null));
                        }
                        else
                        {
                            list.Add(new FloatMenuOption(string.Format(item.LabelCap, group.parent.SlotYielderLabel()), delegate
                            {
                                bill.SetStoreMode(BillStoreModeDefOf.SpecificStockpile, s);
                            }));
                        }
                    }
                }

                Log.ErrorOnce(sb.ToString(), sb.GetHashCode());
            }
        }*/
}
