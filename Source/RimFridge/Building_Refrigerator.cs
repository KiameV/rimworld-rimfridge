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

        internal static float dildo = -10f;

        public new bool StorageTabVisible
        {
            get
            {
                return base.StorageTabVisible;
            }
        }

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
            bool flag2 = this.def.building.defaultStorageSettings != null;
            if (flag2)
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
            bool flag = Scribe.mode == LoadSaveMode.LoadingVars;
            if (flag)
            {
                this.baseSettings = new StorageSettings();
            }
            base.ExposeData();
            Scribe_Values.Look<float>(ref this.Temp, "temp", -3000f, false);
            bool flag2 = Scribe.mode == LoadSaveMode.LoadingVars;
            if (flag2)
            {
                this.baseSettings.CopyFrom(this.def.building.fixedStorageSettings);
                foreach (ThingDef current in DefDatabase<ThingDef>.AllDefs)
                {
                    bool flag3 = current.HasComp(typeof(CompRottable));
                    if (flag3)
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
                this.Temp = base.Position.GetTemperature(base.Map);
            }
            foreach (IntVec3 current in this.AllSlotCells())
            {
                foreach (Thing current2 in current.GetThingList(base.Map))
                {
                    CompRottable compRottable = current2.TryGetComp<CompRottable>();
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
                    bool flag3 = current2.TryGetComp<CompFrosty>() == null && current2.def.defName == "Beer";
                    if (flag3)
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
            float temperature = base.Position.GetTemperature(base.Map);
            float num = (temperature - this.Temp) * 0.01f;
            float num2 = -num;
            float num3 = 0f;
            bool flag4 = this.Temp + num > -10f;
            if (flag4)
            {
                float num4 = Mathf.Max(-10f - (this.Temp + num), -1f);
                bool flag5 = this.powerComp != null && this.powerComp.PowerOn;
                if (flag5)
                {
                    num += num4;
                    num2 -= num4 * 1.25f;
                }
                num3 = num4 * -1f;
            }
            this.Temp += num;
            base.Position.GetRoomGroup(base.Map).PushHeat(num2 * 1.25f);
            this.powerComp.PowerOutput = -((CompProperties_Power)this.powerComp.props).basePowerConsumption * (num3 * 0.9f + 0.1f);
        }

        public override string GetInspectString()       
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(base.GetInspectString());
            stringBuilder.Append("TargetTemperature".Translate() + ": " + dildo.ToStringTemperature("F0"));
            stringBuilder.Append("Temperature".Translate() + ": " + this.Temp.ToStringTemperature("F0"));
            stringBuilder.Append("Power: " + ((this.powerComp != null && this.powerComp.PowerOn) ? "On" : "Off"));
            return stringBuilder.ToString();
        }
    }
}

