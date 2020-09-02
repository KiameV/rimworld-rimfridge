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
        static bool Prefix(ref bool __result, Pawn pawn, LocalTargetInfo dest, PathEndMode peMode, Danger maxDanger, bool canBash, TraverseMode mode)
        {
            if (dest != null && dest.Thing != null && dest.Thing.def.category == ThingCategory.Item)
            {
                foreach (Thing thing in Current.Game.CurrentMap.thingGrid.ThingsAt(dest.Thing.Position))
                {
                    if (thing is RimFridge_Building)
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

    [HarmonyPriority(Priority.Last)]
    [HarmonyPatch(typeof(Thing), "AmbientTemperature", MethodType.Getter)]
    static class Patch_Thing_AmbientTemperature
    {
        static void Postfix(Thing __instance, ref float __result)
        {
            if (__instance.Map != null && __instance.def.rotatable && 
                FridgeCache.TryGetFridge(__instance.Position, __instance.Map, out CompRefrigerator fridge) &&
                fridge != null && fridge.ShouldBeActive)
            {
                __result = 0;
            }
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
