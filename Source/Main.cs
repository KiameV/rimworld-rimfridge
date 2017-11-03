using Harmony;
using RimWorld;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace RimFridge
{
    [StaticConstructorOnStartup]
    class Main
    {
        static Main()
        {
            var harmony = HarmonyInstance.Create("com.rimfridge.rimworld.mod");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            Log.Message("RimFridge: Adding Harmony Postfix to GameComponentUtility.StartedNewGame");
            Log.Message("RimFridge: Adding Harmony Postfix to GameComponentUtility.LoadedGame");
            Log.Message("RimFridge: Adding Harmony Prefix to CompTemperatureRuinable.DoTicks - Will return false if within a RimFridge");
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
                                ruinedPercent += (refridge.CurrentTemp - __instance.Props.maxSafeTemperature) *  __instance.Props.progressPerDegreePerTick * (float)ticks;
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
}
