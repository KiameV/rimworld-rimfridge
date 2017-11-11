using RimWorld;
using System;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;
using System.Collections.Generic;

namespace RimFridge
{
    public class Building_Refrigerator : Building_Storage, IStoreSettingsParent
    {
        public CompPowerTrader powerComp;

        public CompGlower glow;

        private StorageSettings fixedStorageSettings;

        public const float DEFAULT_DESIRED_TEMP = -10f;
        public float DesiredTemp = DEFAULT_DESIRED_TEMP;
        public float CurrentTemp = -3000f;

        internal string label;
        public override string Label { get { return this.label; } }

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.powerComp = base.GetComp<CompPowerTrader>();
            this.glow = base.GetComp<CompGlower>();
            if (this.label == null || this.label.Trim().Length == 0)
                this.label = base.def.label;
        }

        public override void PostMake()
        {
            IntPtr functionPointer = typeof(ThingWithComps).GetMethod("PostMake").MethodHandle.GetFunctionPointer();
            Action action = (Action)Activator.CreateInstance(typeof(Action), new object[]
            {
                this,
                functionPointer
            });
            action();

            this.CreateFixedStorageSettings();

            base.settings = new StorageSettings(this);
            if (this.def.building.defaultStorageSettings != null)
            {
                base.settings.CopyFrom(this.def.building.defaultStorageSettings);
            }
        }

        private void CreateFixedStorageSettings()
        {
            this.fixedStorageSettings = new StorageSettings();
            this.fixedStorageSettings.CopyFrom(this.def.building.fixedStorageSettings);
            foreach (ThingDef td in DefDatabase<ThingDef>.AllDefs)
            {
                if (td.HasComp(typeof(CompRottable)) &&
                    !this.fixedStorageSettings.filter.Allows(td))
                {
                    this.fixedStorageSettings.filter.SetAllow(td, true);
                }
            }
        }

        public new StorageSettings GetParentStoreSettings()
        {
            return this.fixedStorageSettings;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<float>(ref this.CurrentTemp, "temp", -3000f, false);
            Scribe_Values.Look<float>(ref this.DesiredTemp, "desiredTemp", DEFAULT_DESIRED_TEMP, false);

            string label = this.Label;
            if (Scribe.mode == LoadSaveMode.LoadingVars || (Scribe.mode == LoadSaveMode.Saving && this.label != null))
            {
                Scribe_Values.Look<string>(ref label, "label", base.def.label, false);
                this.label = label;
            }

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                this.CreateFixedStorageSettings();
            }
        }

        public override void TickRare()
        {
            if (this.CurrentTemp < -2000f)
            {
                this.CurrentTemp = GridsUtility.GetTemperature(base.Position, base.Map);
            }
            foreach (IntVec3 cell in this.AllSlotCells())
            {
                foreach (Thing thing in GridsUtility.GetThingList(cell, base.Map))
                {
                    CompRottable rottable = ThingCompUtility.TryGetComp<CompRottable>(thing);
                    if (rottable != null && !(rottable is CompBetterRottable))
                    {
                        ThingWithComps thingWithComps = thing as ThingWithComps;
                        CompBetterRottable compBetterRottable = new CompBetterRottable();
                        thingWithComps.AllComps.Remove(rottable);
                        thingWithComps.AllComps.Add(compBetterRottable);
                        compBetterRottable.props = rottable.props;
                        compBetterRottable.parent = thingWithComps;
                        compBetterRottable.RotProgress = rottable.RotProgress;
                    }

                    if (ThingCompUtility.TryGetComp<CompFrosty>(thing) == null && thing.def.defName == "Beer")
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

            float roomTemperature = GridsUtility.GetTemperature(base.Position, base.Map);
            float changetemperature = (roomTemperature - this.CurrentTemp) * 0.01f;
            float changeEnergy = -changetemperature;
            float powerMultiplier = 0f;
            if (this.CurrentTemp + changetemperature > this.DesiredTemp)
            {
                float change = Mathf.Max(this.DesiredTemp - (this.CurrentTemp + changetemperature), -1f);
                if (this.powerComp != null && this.powerComp.PowerOn)
                {
                    changetemperature += change;
                    changeEnergy -= change * 1.25f;
                }
                powerMultiplier = change * -1f;
            }
            this.CurrentTemp += changetemperature;
            GenTemperature.PushHeat(this, changeEnergy * 1.25f);
            this.powerComp.PowerOutput = -((CompProperties_Power)this.powerComp.props).basePowerConsumption * (powerMultiplier * 0.9f + 0.1f);
        }

        private float RoundedToCurrentTempModeOffset(float celsiusTemp)
        {
            float num = GenTemperature.CelsiusToOffset(celsiusTemp, Prefs.TemperatureMode);
            num = (float)Mathf.RoundToInt(num);
            return GenTemperature.ConvertTemperatureOffset(num, Prefs.TemperatureMode, TemperatureDisplayMode.Celsius);
        }

        private void InterfaceChangeTargetTemperature(float offset)
        {
            this.DesiredTemp += offset;
            this.DesiredTemp = Mathf.Clamp(this.DesiredTemp, -270f, 270f);
            this.ThrowCurrentTemperatureText();
        }

        private void ThrowCurrentTemperatureText()
        {
            MoteMaker.ThrowText(this.TrueCenter() + new Vector3(0.5f, 0f, 0.5f), this.Map, this.DesiredTemp.ToStringTemperature("F0"), Color.white, -1f);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            IEnumerable<Gizmo> gizmos = base.GetGizmos();
            if (gizmos != null)
            {
                foreach (Gizmo g in gizmos)
                {
                    yield return g;
                }
            }

            yield return new Command_Action
            {
                icon = ContentFinder<Texture2D>.Get("UI/Icons/Rename", true),
                defaultDesc = "RimFridge.RenameTheRefrigerator".Translate(),
                defaultLabel = "Rename".Translate(),
                activateSound = SoundDef.Named("Click"),
                action = delegate { Find.WindowStack.Add(new Dialog_Rename(this)); },
                groupKey = 887767542
            };

            float offsetN10 = this.RoundedToCurrentTempModeOffset(-10f);
            yield return new Command_Action
            {
                action = delegate
                {
                    this.InterfaceChangeTargetTemperature(offsetN10);
                },
                defaultLabel = offsetN10.ToStringTemperatureOffset("F0"),
                defaultDesc = "CommandLowerTempDesc".Translate(),
                hotKey = KeyBindingDefOf.Misc5,
                icon = ContentFinder<Texture2D>.Get("UI/Commands/TempLower", true)
            };

            float offsetN1 = this.RoundedToCurrentTempModeOffset(-1f);
            yield return new Command_Action
            {
                action = delegate
                {
                    this.InterfaceChangeTargetTemperature(offsetN1);
                },
                defaultLabel = offsetN1.ToStringTemperatureOffset("F0"),
                defaultDesc = "CommandLowerTempDesc".Translate(),
                hotKey = KeyBindingDefOf.Misc4,
                icon = ContentFinder<Texture2D>.Get("UI/Commands/TempLower", true)
            };

            yield return new Command_Action
            {
                action = delegate
                {
                    this.DesiredTemp = DEFAULT_DESIRED_TEMP;
                    this.ThrowCurrentTemperatureText();
                },
                defaultLabel = "CommandResetTemp".Translate(),
                defaultDesc = "CommandResetTempDesc".Translate(),
                hotKey = KeyBindingDefOf.Misc1,
                icon = ContentFinder<Texture2D>.Get("UI/Commands/TempReset", true)
            };

            float offset1 = this.RoundedToCurrentTempModeOffset(1f);
            yield return new Command_Action
            {
                action = delegate
                {
                    this.InterfaceChangeTargetTemperature(offset1);
                },
                defaultLabel = "+" + offset1.ToStringTemperatureOffset("F0"),
                defaultDesc = "CommandRaiseTempDesc".Translate(),
                hotKey = KeyBindingDefOf.Misc2,
                icon = ContentFinder<Texture2D>.Get("UI/Commands/TempRaise", true)
            };

            float offset10 = this.RoundedToCurrentTempModeOffset(10f);
            yield return new Command_Action
            {
                action = delegate
                {
                    this.InterfaceChangeTargetTemperature(offset10);
                },
                defaultLabel = "+" + offset10.ToStringTemperatureOffset("F0"),
                defaultDesc = "CommandRaiseTempDesc".Translate(),
                hotKey = KeyBindingDefOf.Misc3,
                icon = ContentFinder<Texture2D>.Get("UI/Commands/TempRaise", true)
            };
        }

        public override string GetInspectString()
        {
            StringBuilder sb = new StringBuilder();
            String s = base.GetInspectString();
            if (s.Length > 0)
            {
                sb.Append(s);
                sb.Append(Environment.NewLine);
            }
            sb.Append("RimFridge.TargetTemperature".Translate());
            sb.Append(": ");
            sb.Append(GenText.ToStringTemperature(this.DesiredTemp, "F0"));
            sb.Append(Environment.NewLine);
            sb.Append("RimFridge.CurrentTemperature".Translate());
            sb.Append(": ");
            sb.Append(GenText.ToStringTemperature(this.CurrentTemp, "F0"));
            sb.Append(Environment.NewLine);
            sb.Append("RimFridge.Power".Translate());
            sb.Append(": ");
            sb.Append((string)((this.powerComp != null && this.powerComp.PowerOn) ? "On".Translate() : "Off".Translate()));
            return sb.ToString().TrimEndNewlines();
        }
    }
}