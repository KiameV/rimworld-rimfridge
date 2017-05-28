using RimWorld;
using System;
using System.Reflection;
using System.Text;
using UnityEngine;
using Verse;

namespace RimFridge
{
    public class Building_Refrigerator : Building_Storage, IStoreSettingsParent
    {
        public CompPowerTrader powerComp;

        public CompGlower glow;

        private StorageSettings baseSettings;

        public float Temp = -3000f;

        public override void SpawnSetup(Map map, bool respawningAfterLoad)
        {
            base.SpawnSetup(map, respawningAfterLoad);
            this.powerComp = base.GetComp<CompPowerTrader>();
            this.glow = base.GetComp<CompGlower>();
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
            this.baseSettings = new StorageSettings();
            this.baseSettings.CopyFrom(this.def.building.fixedStorageSettings);
            foreach (ThingDef current in DefDatabase<ThingDef>.AllDefs)
            {
                bool flag = current.HasComp(typeof(CompRottable));
                if (flag)
                {
                    this.baseSettings.filter.SetAllow(current, true);
                }
            }
            this.settings = new StorageSettings(this);
            if (this.def.building.defaultStorageSettings != null)
            {
                this.settings.CopyFrom(this.def.building.defaultStorageSettings);
            }
        }

        public new StorageSettings GetStoreSettings()
        {
            return this.settings;
        }

        public new StorageSettings GetParentStoreSettings()
        {
            return this.baseSettings;
        }

        public override void ExposeData()
        {
            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                this.baseSettings = new StorageSettings();
            }

            base.ExposeData();
            Scribe_Values.Look<float>(ref this.Temp, "temp", -3000f, false);

            if (Scribe.mode == LoadSaveMode.LoadingVars)
            {
                this.baseSettings.CopyFrom(this.def.building.fixedStorageSettings);
                foreach (ThingDef current in DefDatabase<ThingDef>.AllDefs)
                {
                    if (current.HasComp(typeof(CompRottable)))
                    {
                        this.baseSettings.filter.SetAllow(current, true);
                    }
                }
            }
        }

        public override void TickRare()
        {
            bool flag = this.Temp < -2000f;
            if (flag)
            {
                this.Temp = GridsUtility.GetTemperature(base.Position, base.Map);
            }
            foreach (IntVec3 current in this.AllSlotCells())
            {
                foreach (Thing current2 in GridsUtility.GetThingList(current, base.Map))
                {
                    CompRottable compRottable = ThingCompUtility.TryGetComp<CompRottable>(current2);
                    bool flag2 = compRottable != null && !(compRottable is CompBetterRottable);
                    if (flag2)
                    {
                        ThingWithComps thingWithComps = current2 as ThingWithComps;
                        CompBetterRottable compBetterRottable = new CompBetterRottable();
                        thingWithComps.AllComps.Remove(compRottable);
                        thingWithComps.AllComps.Add(compBetterRottable);
                        compBetterRottable.props = compRottable.props;
                        compBetterRottable.parent = thingWithComps;
                        compBetterRottable.RotProgress = compRottable.RotProgress;
                    }

                    if (ThingCompUtility.TryGetComp<CompFrosty>(current2) == null && current2.def.defName == "Beer")
                    {
                        ThingWithComps thingWithComps2 = current2 as ThingWithComps;
                        CompFrosty compFrosty = new CompFrosty();
                        thingWithComps2.AllComps.Add(compFrosty);
                        compFrosty.props = CompProperties_Frosty.Beer;
                        compFrosty.parent = thingWithComps2;
                        ((TickList)typeof(TickManager).GetField("tickListRare", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Find.TickManager)).RegisterThing(thingWithComps2);
                    }
                }
            }
            float temperature = GridsUtility.GetTemperature(base.Position, base.Map);
            float num = (temperature - this.Temp) * 0.01f;
            float num2 = -num;
            float num3 = 0f;
            bool flag4 = this.Temp + num > -10f;
            if (flag4)
            {
                float num4 = Mathf.Max(-10f - (this.Temp + num), -1f);
                if (this.powerComp != null && this.powerComp.PowerOn)
                {
                    num += num4;
                    num2 -= num4 * 1.25f;
                }
                num3 = num4 * -1f;
            }
            this.Temp += num;
            GenTemperature.PushHeat(this, num2 * 1.25f);
            this.powerComp.PowerOutput = -((CompProperties_Power)this.powerComp.props).basePowerConsumption * (num3 * 0.9f + 0.1f);
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

