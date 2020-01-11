﻿using RimWorld;
using System;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;
using System.Collections.Generic;

namespace RimFridge
{
    public class CompRefrigerator : ThingComp 
    {
        // Default temperature just below freezing.

        public float desiredTemp;
        public float currentTemp = 21f;

        public string buildingLabel = "";
        private StorageSettings fixedStorageSettings;
        private CompPowerTrader powerTrader => parent.GetComp<CompPowerTrader>();

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            float offsetN10 = RoundedToCurrentTempModeOffset(-10f);
            float offsetN1 = RoundedToCurrentTempModeOffset(-1f);
            float offset1 = RoundedToCurrentTempModeOffset(1f);
            float offset10 = RoundedToCurrentTempModeOffset(10f);

            foreach (Gizmo g in base.CompGetGizmosExtra())
            {
                yield return g;
            }
            yield return new Command_Action
            {
                action = delegate { Find.WindowStack.Add(new Dialog_RenameFridge(this)); },
                defaultLabel = "Rename".Translate(),
                defaultDesc = "RimFridge.RenameTheRefrigerator".Translate(),
                icon = ContentFinder<Texture2D>.Get("UI/Icons/Rename", true),
                activateSound = SoundDef.Named("Click"),
                groupKey = 887767542
            };
            yield return new Command_Action
            {
                action = delegate { InterfaceChangeTargetTemperature(offsetN10); },
                defaultLabel = offsetN10.ToStringTemperatureOffset("F0"),
                defaultDesc = "CommandLowerTempDesc".Translate(),
                hotKey = KeyBindingDefOf.Misc5,
                icon = ContentFinder<Texture2D>.Get("UI/Commands/TempLower", true)
            };
            yield return new Command_Action
            {
                action = delegate { InterfaceChangeTargetTemperature(offsetN1); },
                defaultLabel = offsetN1.ToStringTemperatureOffset("F0"),
                defaultDesc = "CommandLowerTempDesc".Translate(),
                hotKey = KeyBindingDefOf.Misc4,
                icon = ContentFinder<Texture2D>.Get("UI/Commands/TempLower", true)
            };
            yield return new Command_Action
            {
                action = delegate { desiredTemp = defaultDesiredTemperature; },
                defaultLabel = "CommandResetTemp".Translate(),
                defaultDesc = "CommandResetTempDesc".Translate(),
                hotKey = KeyBindingDefOf.Misc1,
                icon = ContentFinder<Texture2D>.Get("UI/Commands/TempReset", true)
            };
            yield return new Command_Action
            {
                action = delegate { InterfaceChangeTargetTemperature(offset1); },
                defaultLabel = "+" + offset1.ToStringTemperatureOffset("F0"),
                defaultDesc = "CommandRaiseTempDesc".Translate(),
                hotKey = KeyBindingDefOf.Misc2,
                icon = ContentFinder<Texture2D>.Get("UI/Commands/TempRaise", true)
            };
            yield return new Command_Action
            {
                action = delegate { InterfaceChangeTargetTemperature(offset10); },
                defaultLabel = "+" + offset10.ToStringTemperatureOffset("F0"),
                defaultDesc = "CommandRaiseTempDesc".Translate(),
                hotKey = KeyBindingDefOf.Misc3,
                icon = ContentFinder<Texture2D>.Get("UI/Commands/TempRaise", true)
            };
        }

        public List<string> drinksBestCold => ((CompProperties_Refrigerator)props).drinksBestCold;
        public float defaultDesiredTemperature => ((CompProperties_Refrigerator)props).defaultDesiredTemperature;

        public override string TransformLabel(string label)
        {
            return buildingLabel == "" ? label : buildingLabel;
        }


        private void InterfaceChangeTargetTemperature(float offset)
        {
            desiredTemp += offset;
            desiredTemp = Mathf.Clamp(desiredTemp, -270f, 270f);
        }

        private float RoundedToCurrentTempModeOffset(float celsiusTemp)
        {
            float num = GenTemperature.CelsiusToOffset(celsiusTemp, Prefs.TemperatureMode);
            num = Mathf.RoundToInt(num);
            return GenTemperature.ConvertTemperatureOffset(num, Prefs.TemperatureMode, TemperatureDisplayMode.Celsius);
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            //Check for alcohol that is best drunk cold.
            foreach (IntVec3 cell in ((Building_Storage)parent).AllSlotCells())
            {
                foreach (Thing thing in GridsUtility.GetThingList(cell, parent.Map))
                {
                    if (drinksBestCold.Contains(thing.def.defName) && ThingCompUtility.TryGetComp<CompFrosty>(thing) == null)
                    {
                        ThingWithComps thingWithComps = thing as ThingWithComps;
                        CompFrosty compFrosty = new CompFrosty();
                        thingWithComps.AllComps.Add(compFrosty);
                        compFrosty.props = CompProperties_Frosty.Beer;
                        compFrosty.parent = thingWithComps;
                        ((TickList)typeof(TickManager).GetField("tickListRare", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Find.TickManager)).RegisterThing(thingWithComps);
                    }
                }
            }
            //Get the actual temperature at the fridge, since we're patching the game's method.
            //These call the internal functions of GenTemperature.TryGetTemperatureForCell()

            IntVec3 position = parent.Position;
            Map map = parent.Map;
            float roomTemperature = 21f;  // The game uses 21 as it's general default as well.

            // This bit will work for normal furniture RimFridges
            if (!GenTemperature.TryGetDirectAirTemperatureForCell(position, map, out roomTemperature))
            {
                List<Thing> list = map.thingGrid.ThingsListAtFast(position);
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i].def.passability == Traversability.Impassable)
                    {
                        // This is if it's a wall-mount RimFridge and not part of a "room"
                        GenTemperature.TryGetAirTemperatureAroundThing(list[i], out roomTemperature);
                    }
                }
            }
            
            float changetemperature = (roomTemperature - currentTemp) * 0.01f;
            float changeEnergy = -changetemperature;
            float powerMultiplier = 0f;
            if (currentTemp + changetemperature > desiredTemp)
            {
                // When the RimFridge's compressor is working and it's pushing the internal temperature down, it can draw a lot of power!  
                // Once it gets to temp, maintaining it isn't bad.
                float change = Mathf.Max(desiredTemp - (currentTemp + changetemperature), -3f);
                if (powerTrader != null && powerTrader.PowerOn)
                {
                    changetemperature += change;
                    changeEnergy -= change * 1.25f;
                }
                powerMultiplier = change * -1f;
            }
            // Like all refrigerators, the RimFridge is insulated.  It won't instantly drop to room-temp from loss of power and things inside
            // should be good through brief power interruptions.
            currentTemp += changetemperature;
            IntVec3 pos = position + IntVec3.North.RotatedBy(parent.Rotation);
            GenTemperature.PushHeat(pos, map, changeEnergy * 1.25f);
            powerTrader.PowerOutput = -((CompProperties_Power)powerTrader.props).basePowerConsumption * ((powerMultiplier * 0.9f) + 0.1f);
        }

        private void CreateFixedStorageSettings()
        {
            fixedStorageSettings = new StorageSettings();
            fixedStorageSettings.CopyFrom(parent.def.building.fixedStorageSettings);
            foreach (ThingDef td in DefDatabase<ThingDef>.AllDefs)
            {
                if (td.HasComp(typeof(CompRottable)) && !fixedStorageSettings.filter.Allows(td))
                {
                    fixedStorageSettings.filter.SetAllow(td, true);
                }
            }
        }
        public override void Initialize(CompProperties props)
        {
            base.Initialize(props);
            CreateFixedStorageSettings();

            ((Building_Storage)parent).settings = new StorageSettings((Building_Storage)parent);
            if (parent.def.building.defaultStorageSettings != null)
            {
                ((Building_Storage)parent).settings.CopyFrom(parent.def.building.defaultStorageSettings);
            }
            desiredTemp = defaultDesiredTemperature;
        }

        public override void PostExposeData()
        {
            base.PostExposeData();

            Scribe_Values.Look(ref currentTemp, "currentTemp", 21f, false);
            Scribe_Values.Look(ref desiredTemp, "desiredTemp", defaultDesiredTemperature, false);

            string label = parent.Label;
            if (Scribe.mode == LoadSaveMode.LoadingVars || (Scribe.mode == LoadSaveMode.Saving && buildingLabel != null))
            {
                Scribe_Values.Look(ref buildingLabel, "buildingLabel", "", false);
            }

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                CreateFixedStorageSettings();
            }
        }

        public override string CompInspectStringExtra()
        {
            StringBuilder sb = new StringBuilder();
      
            sb.Append("RimFridge.TargetTemperature".Translate());
            sb.Append(": ");
            sb.Append(GenText.ToStringTemperature(desiredTemp, "F0"));
            sb.Append(Environment.NewLine);
            sb.Append("RimFridge.CurrentTemperature".Translate());
            sb.Append(": ");
            sb.Append(GenText.ToStringTemperature(currentTemp, "F0"));
            sb.Append(Environment.NewLine);
            sb.Append("RimFridge.Power".Translate());
            sb.Append(": ");
            sb.Append((powerTrader != null && powerTrader.PowerOn) ? "On".Translate() : "Off".Translate());
            return sb.ToString().TrimEndNewlines();
        }
    }
}
