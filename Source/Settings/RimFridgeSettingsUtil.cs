using RimWorld;
using System.Collections.Generic;
using Verse;

namespace RimFridge
{
    internal static class RimFridgeSettingsUtil
    {
        public static Dictionary<string, float> BaseEnergy { get; set; }
        public static Dictionary<string, ThingDef> FridgeDefs { get; set; }

        static RimFridgeSettingsUtil()
        {
            BaseEnergy = null;
        }

        private static void CreateBaseEnergyMap()
        {
            if (BaseEnergy == null)
            {
                BaseEnergy = new Dictionary<string, float>();
                FridgeDefs = new Dictionary<string, ThingDef>();
                foreach (ThingDef def in DefDatabase<ThingDef>.AllDefsListForReading)
                {
                    if (def.defName.StartsWith("RimFridge"))
                    {
                        CompProperties_Power power = def.GetCompProperties<CompProperties_Power>();
                        if (power != null)
                        {
                            BaseEnergy.Add(def.defName, power.basePowerConsumption);
                            FridgeDefs.Add(def.defName, def);
                        }
                    }
                }
            }
        }

        public static void ApplyFactor(float newFactor)
        {
            CreateBaseEnergyMap();

            foreach (KeyValuePair<string, float> basePower in BaseEnergy)
            {
                ThingDef def = FridgeDefs[basePower.Key];
                CompProperties_Power power = def.GetCompProperties<CompProperties_Power>();
                power.basePowerConsumption = basePower.Value * newFactor;
            }
        }
    }
}
