using Harmony;
using RimWorld;
using System;
using System.Collections.Generic;
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
            var harmony = HarmonyInstance.Create("com.rimfridge.rimworld.mod");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Log.Message(
                "RimFridge Harmony Patches:" + Environment.NewLine +
                "  Prefix:" + Environment.NewLine +
                "    CompTemperatureRuinable.DoTicks - Will return false if within a RimFridge" + Environment.NewLine +
                "    ReachabilityUtility.CanReach" + Environment.NewLine +
                "  Postfix:" + Environment.NewLine +
                "    GameComponentUtility.StartedNewGame" + Environment.NewLine +
                "    GameComponentUtility.LoadedGame");
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

    [HarmonyPatch(typeof(CompTemperatureRuinable), "DoTicks")]
    static class Patch_CompTemperatureRuinable_DoTicks
    {
        private static FieldInfo ruinedPercentFI;
        private static FieldInfo RuinedPercentFI
        {
            get
            {
                if (ruinedPercentFI == null)
                {
                    ruinedPercentFI = typeof(CompTemperatureRuinable).GetField("ruinedPercent", BindingFlags.Instance | BindingFlags.NonPublic);
                }
                return ruinedPercentFI;
            }
        }
        static bool Prefix(CompTemperatureRuinable __instance, int ticks)
        {
            if (!__instance.Ruined)
            {
                IEnumerable<Thing> things = __instance.parent?.Map?.thingGrid.ThingsAt(__instance.parent.Position);
                if (things != null)
                {
                    foreach (Thing thing in things)
                    {
                        if (thing?.def.defName.StartsWith("RimFridge") == true)
                        {
                            Building_Refrigerator refridge = (Building_Refrigerator)thing;
                            float ruinedPercent = (float)RuinedPercentFI.GetValue(__instance);
                            if (refridge.CurrentTemp > __instance.Props.maxSafeTemperature)
                            {
                                ruinedPercent += (refridge.CurrentTemp - __instance.Props.maxSafeTemperature) * __instance.Props.progressPerDegreePerTick * (float)ticks;
                            }
                            else if (refridge.CurrentTemp < __instance.Props.minSafeTemperature)
                            {
                                ruinedPercent -= (refridge.CurrentTemp - __instance.Props.minSafeTemperature) * __instance.Props.progressPerDegreePerTick * (float)ticks;
                            }

                            if (ruinedPercent >= 1f)
                            {
                                ruinedPercent = 1f;
                                __instance.parent.BroadcastCompSignal("RuinedByTemperature");
                            }
                            else if (ruinedPercent < 0f)
                            {
                                ruinedPercent = 0f;
                            }
                            RuinedPercentFI.SetValue(__instance, ruinedPercent);
                            return false;
                        }
                    }
                }
            }
            return true;
        }
    }

    [HarmonyPatch(typeof(ReachabilityUtility), "CanReach")]
    static class Patch_ReachabilityUtility_CanReach
    {
        static bool Prefix(ref bool __result, Pawn pawn, LocalTargetInfo dest, PathEndMode peMode, Danger maxDanger, bool canBash, TraverseMode mode)
        {
            if (dest != null && dest.Thing != null && dest.Thing.def.category == ThingCategory.Item)
            {
                foreach (Thing t in Current.Game.CurrentMap.thingGrid.ThingsAt(dest.Thing.Position))
                {
                    if (t.def.defName.StartsWith("RimFridge_"))
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

    [HarmonyPatch(typeof(TradeShip), "ColonyThingsWillingToBuy")]
    static class Patch_PassingShip_TryOpenComms
    {
        // Before an orbital trade
        static void Postfix(ref IEnumerable<Thing> __result, Pawn playerNegotiator)
        {
            if (playerNegotiator != null && playerNegotiator.Map != null)
            {
                if (playerNegotiator.Map != null)
                {
                    List<Thing> result = null;
                    foreach (Thing t in playerNegotiator.Map.listerThings.AllThings)
                    {
                        if (t is Building_Refrigerator && t.def.defName.IndexOf("Wall") != -1)
                        {
                            foreach (Thing f in playerNegotiator.Map.thingGrid.ThingsAt(t.Position))
                            {
                                if (((Building_Refrigerator)t).settings.AllowedToAccept(f))
                                {
                                    if (result == null)
                                        result = new List<Thing>(__result);
                                    result.Add(f);
                                    break;
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
}
