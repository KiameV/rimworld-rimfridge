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

        public float Temp = -3000f;

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
            Scribe_Values.Look<float>(ref this.Temp, "temp", -3000f, false);

            string label = this.Label;
            if (Scribe.mode != LoadSaveMode.Saving || this.label != null)
            {
                Scribe_Values.Look<string>(ref label, "label", base.def.label, false);
            }

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                this.CreateFixedStorageSettings();
            }
        }

        public override void TickRare()
        {
            if (this.Temp < -2000f)
            {
                this.Temp = GridsUtility.GetTemperature(base.Position, base.Map);
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
            float changetemperature = (roomTemperature - this.Temp) * 0.01f;
            float changeEnergy = -changetemperature;
            float powerMultiplier = 0f;
            if (this.Temp + changetemperature > -10f)
            {
                float change = Mathf.Max(-10f - (this.Temp + changetemperature), -1f);
                if (this.powerComp != null && this.powerComp.PowerOn)
                {
                    changetemperature += change;
                    changeEnergy -= change * 1.25f;
                }
                powerMultiplier = change * -1f;
            }
            this.Temp += changetemperature;
            GenTemperature.PushHeat(this, changeEnergy * 1.25f);
            this.powerComp.PowerOutput = -((CompProperties_Power)this.powerComp.props).basePowerConsumption * (powerMultiplier * 0.9f + 0.1f);
        }

        public override IEnumerable<Gizmo> GetGizmos()
        {
            List<Gizmo> list = new List<Gizmo>(base.GetGizmos());
            if (list == null)
                list = new List<Gizmo>();

            Command_Action a = new Command_Action();
            a.icon = ContentFinder<Texture2D>.Get("UI/Icons/Rename", true);
            a.defaultDesc = "RimFridge.RenameTheRefrigerator".Translate();
            a.defaultLabel = "Rename".Translate();
            a.activateSound = SoundDef.Named("Click");
            a.action = delegate { Find.WindowStack.Add(new Dialog_Rename(this)); };
            a.groupKey = 887767542;
            list.Add(a);

            list = SaveStorageSettingsUtil.SaveStorageSettingsGizmoUtil.AddSaveLoadGizmos(list, "RimFridge", this.settings.filter);

            return list;
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
            sb.Append(GenText.ToStringTemperature(-10f, "F0"));
            sb.Append(Environment.NewLine);
            sb.Append("RimFridge.CurrentTemperature".Translate());
            sb.Append(": ");
            sb.Append(GenText.ToStringTemperature(this.Temp, "F0"));
            sb.Append(Environment.NewLine);
            sb.Append("RimFridge.Power".Translate());
            sb.Append(": ");
            sb.Append((string)((this.powerComp != null && this.powerComp.PowerOn) ? "On".Translate() : "Off".Translate()));
            return sb.ToString().TrimEndNewlines();
        }
    }
}

