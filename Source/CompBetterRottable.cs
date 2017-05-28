using RimWorld;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Verse;

namespace RimFridge
{
    public class CompBetterRottable : CompRottable
    {
        private CompProperties_Rottable PropsRot
        {
            get
            {
                return (CompProperties_Rottable)this.props;
            }
        }

        public override string CompInspectStringExtra()
        {
            StringBuilder sb = new StringBuilder();
            switch (base.Stage)
            {
                case RotStage.Fresh:
                    sb.AppendLine(Translator.Translate("RotStateFresh") + ".");
                    break;
                case RotStage.Rotting:
                    sb.AppendLine(Translator.Translate("RotStateRotting") + ".");
                    break;
                case RotStage.Dessicated:
                    sb.AppendLine(Translator.Translate("RotStateDessicated") + ".");
                    break;
            }
            float num = (float)this.PropsRot.TicksToRotStart - base.RotProgress;
            bool flag = num > 0f;
            if (flag)
            {
                float num2 = GenTemperature.GetTemperatureForCell(this.parent.PositionHeld, this.parent.Map);
                List<Thing> thingList = GridsUtility.GetThingList(this.parent.PositionHeld, this.parent.Map);
                for (int i = 0; i < thingList.Count; i++)
                {
                    bool flag2 = thingList[i] is Building_Refrigerator;
                    if (flag2)
                    {
                        Building_Refrigerator building_Refrigerator = thingList[i] as Building_Refrigerator;
                        num2 = building_Refrigerator.Temp;
                        break;
                    }
                }
                num2 = (float)Mathf.RoundToInt(num2);
                float num3 = GenTemperature.RotRateAtTemperature(num2);
                int ticksUntilRotAtCurrentTemp = base.TicksUntilRotAtCurrentTemp;
                bool flag3 = num3 < 0.001f;
                if (flag3)
                {
                    sb.Append(Translator.Translate("CurrentlyFrozen") + ".");
                }
                else
                {
                    bool flag4 = num3 < 0.999f;
                    if (flag4)
                    {
                        sb.Append(Translator.Translate("CurrentlyRefrigerated", new object[]
                        {
                            GenDate.ToStringTicksToPeriodVagueMax(ticksUntilRotAtCurrentTemp)
                        }) + ".");
                    }
                    else
                    {
                        sb.Append(Translator.Translate("NotRefrigerated", new object[]
                        {
                            GenDate.ToStringTicksToPeriodVagueMax(ticksUntilRotAtCurrentTemp)
                        }) + ".");
                    }
                }
            }
            return sb.ToString().TrimEndNewlines();
        }

        public override void CompTickRare()
        {
            float rotProgress = base.RotProgress;
            float num = 1f;
            float num2 = GenTemperature.GetTemperatureForCell(this.parent.PositionHeld, this.parent.Map);
            List<Thing> thingList = GridsUtility.GetThingList(this.parent.PositionHeld, this.parent.Map);
            for (int i = 0; i < thingList.Count; i++)
            {
                bool flag = thingList[i] is Building_Refrigerator;
                if (flag)
                {
                    Building_Refrigerator building_Refrigerator = thingList[i] as Building_Refrigerator;
                    num2 = building_Refrigerator.Temp;
                    break;
                }
            }
            num *= GenTemperature.RotRateAtTemperature(num2);
            base.RotProgress = base.RotProgress + Mathf.Round(num * 250f);
            bool flag2 = base.Stage == RotStage.Rotting && this.PropsRot.rotDestroys;
            if (flag2)
            {
                bool flag3 = StoreUtility.GetSlotGroup(this.parent.Position, this.parent.Map) != null;
                if (flag3)
                {
                    Messages.Message(GenText.CapitalizeFirst(Translator.Translate("MessageRottedAwayInStorage", new object[]
                    {
                        this.parent.Label
                    })), 0);
                    LessonAutoActivator.TeachOpportunity(ConceptDefOf.SpoilageAndFreezers, 0);
                }
                this.parent.Destroy(0);
            }
            else
            {
                bool flag4 = Mathf.FloorToInt(rotProgress / 60000f) != Mathf.FloorToInt(base.RotProgress / 60000f);
                bool flag5 = flag4;
                if (flag5)
                {
                    bool flag6 = base.Stage == RotStage.Rotting && this.PropsRot.rotDamagePerDay > 0f;
                    if (flag6)
                    {
                        this.parent.TakeDamage(new DamageInfo(DamageDefOf.Rotting, GenMath.RoundRandom(this.PropsRot.rotDamagePerDay), (float)-1, null, null, null));
                    }
                    else
                    {
                        bool flag7 = base.Stage == RotStage.Rotting && this.PropsRot.dessicatedDamagePerDay > 0f && this.ShouldTakeDessicateDamage();
                        if (flag7)
                        {
                            this.parent.TakeDamage(new DamageInfo(DamageDefOf.Rotting, GenMath.RoundRandom(this.PropsRot.dessicatedDamagePerDay), (float)-1, null, null, null));
                        }
                    }
                }
            }
        }

        private bool ShouldTakeDessicateDamage()
        {
            /*bool flag = this.parent.holdingOwner != null;
            if (flag)
            {
                Thing thing = this.parent.holdingContainer.owner as Thing;
                bool flag2 = thing != null && thing.def.category == 4 && thing.def.building.preventDeterioration;
                if (flag2)
                {
                    return false;
                }
            }*/
            return true;
        }

        private void StageChanged()
        {
            Corpse corpse = this.parent as Corpse;
            bool flag = corpse != null;
            if (flag)
            {
                corpse.RotStageChanged();
            }
        }
    }
}

