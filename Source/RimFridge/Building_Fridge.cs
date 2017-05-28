using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using UnityEngine;

namespace RimFridge
{
    public class Building_Refrigerator : Building_Storage
    {
        public CompPowerTrader powerComp;
        public CompGlower glow;

        public override void SpawnSetup()
        {
            base.SpawnSetup();
            this.powerComp = base.GetComp<CompPowerTrader>();
            glow = GetComp<CompGlower>();
        }

        public float Temp = -3000f;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.LookValue<float>(ref this.Temp, "temp", -3000f, false);
        }

        public override void TickRare()
        {
            if(Temp < -2000f)
            {
                Temp = Position.GetTemperature();
            }

            foreach(var cell in AllSlotCells())
            {
                foreach (var thing in cell.GetThingList())
                {
                    var rottable = thing.TryGetComp<CompRottable>();
                    if (rottable != null && !(rottable is CompBetterRottable))
                    {
                        var li = thing as ThingWithComps;
                        var newRot = new CompBetterRottable();
                        li.AllComps.Remove(rottable);
                        li.AllComps.Add(newRot);
                        newRot.props = rottable.props;
                        newRot.parent = li;
                        newRot.RotProgress = rottable.RotProgress;
                    }
                }
            }

            var roomTemp = Position.GetTemperature();
            var changeTemp = (roomTemp - Temp) * 0.01f;
            var changeEnergy = -changeTemp;
            var powerMult = 0f;
            if((Temp + changeTemp) > -10) {
                var change = Mathf.Max((-10 - (Temp + changeTemp)), -1f);
                if (powerComp != null && powerComp.PowerOn)
                {
                    changeTemp += change;
                    changeEnergy -= change * 1.25f;
                }
                powerMult = change * -1f;
            }
            Temp += changeTemp;
            Position.GetRoom().PushHeat(changeEnergy * 1.25f);
            powerComp.PowerOutput = -((CompProperties_Power)powerComp.props).basePowerConsumption * (powerMult * 0.9f + 0.1f);
        }

        

        public override string GetInspectString()
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append(base.GetInspectString());
            stringBuilder.AppendLine("TargetTemperature".Translate() + ": " + (-10f).ToStringTemperature("F0"));
            stringBuilder.AppendLine("Temperature".Translate() + ": " + Temp.ToStringTemperature("F0"));
            stringBuilder.AppendLine("Power: " + (powerComp != null && powerComp.PowerOn ? "On" : "Off"));
            return stringBuilder.ToString();
        }
    }
}
