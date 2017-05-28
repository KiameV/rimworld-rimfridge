using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using RimWorld.Planet;
using Verse;
using UnityEngine;

namespace RimFridge
{

 
    public class CompBetterRottable : RimWorld.CompRottable
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
            StringBuilder stringBuilder = new StringBuilder();
            switch (this.Stage)
            {
                case RotStage.Fresh:
                    stringBuilder.Append("RotStateFresh".Translate());
                    break;
                case RotStage.Rotting:
                    stringBuilder.Append("RotStateRotting".Translate());
                    break;
                case RotStage.Dessicated:
                    stringBuilder.Append("RotStateDessicated".Translate());
                    break;
            }
            float num = (float)this.PropsRot.TicksToRotStart - this.RotProgress;
            if (num > 0f)
            {
                float num2 = GenTemperature.GetTemperatureForCell(this.parent.PositionHeld, this.parent.MapHeld);
                List<Thing> list = this.parent.MapHeld.thingGrid.ThingsListAtFast(this.parent.PositionHeld);
                for (int i = 0; i < list.Count; i++)
                {
                    if (list[i] is Building_Refrigerator)
                    {
                        var bf = list[i] as Building_Refrigerator;
                        num2 = bf.Temp;
                        break;
                    }
                }
                num2 = (float)Mathf.RoundToInt(num2);
                float num3 = GenTemperature.RotRateAtTemperature(num2);
                int ticksUntilRotAtCurrentTemp = this.TicksUntilRotAtCurrentTemp;
                if (num3 < 0.001f)
                {
                    stringBuilder.Append("CurrentlyFrozen".Translate());
                }
                else if (num3 < 0.999f)
                {
                    stringBuilder.Append("CurrentlyRefrigerated".Translate(new object[]
                    {
                ticksUntilRotAtCurrentTemp.ToStringTicksToPeriodVagueMax()
                    }));
                }
                else
                {
                    stringBuilder.Append("NotRefrigerated".Translate(new object[]
                    {
                ticksUntilRotAtCurrentTemp.ToStringTicksToPeriodVagueMax()
                    }));
                }
            }
            return stringBuilder.ToString();
        }


        public override void CompTickRare()
        {
            float rotProgress = this.RotProgress;
            float num = 1f;
            float temperatureForCell = GenTemperature.GetTemperatureForCell(this.parent.PositionHeld, this.parent.MapHeld);
            List<Thing> list = this.parent.MapHeld.thingGrid.ThingsListAtFast(this.parent.PositionHeld);
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] is Building_Refrigerator)
                {
                    var bf = list[i] as Building_Refrigerator;
                    temperatureForCell = bf.Temp;
                    break;
                }
            }

            num *= GenTemperature.RotRateAtTemperature(temperatureForCell);
            this.RotProgress += Mathf.Round(num * 250f);
            if (this.Stage == RotStage.Rotting && this.PropsRot.rotDestroys)
            {
                if (this.parent.Map.slotGroupManager.SlotGroupAt(this.parent.Position) != null)
                {
                    Messages.Message("MessageRottedAwayInStorage".Translate(new object[]
                    {
                this.parent.Label
                    }).CapitalizeFirst(), MessageSound.Silent);
                    LessonAutoActivator.TeachOpportunity(ConceptDefOf.SpoilageAndFreezers, OpportunityType.GoodToKnow);
                }
                this.parent.Destroy(DestroyMode.Vanish);
                return;
            }
            bool flag = Mathf.FloorToInt(rotProgress / 60000f) != Mathf.FloorToInt(this.RotProgress / 60000f);
            if (flag)
            {
                if (this.Stage == RotStage.Rotting && this.PropsRot.rotDamagePerDay > 0f)
                {
                    this.parent.TakeDamage(new DamageInfo(DamageDefOf.Rotting, GenMath.RoundRandom(this.PropsRot.rotDamagePerDay), -1f, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown));
                }
                else if (this.Stage == RotStage.Dessicated && this.PropsRot.dessicatedDamagePerDay > 0f && this.ShouldTakeDessicateDamage())
                {
                    this.parent.TakeDamage(new DamageInfo(DamageDefOf.Rotting, GenMath.RoundRandom(this.PropsRot.dessicatedDamagePerDay), -1f, null, null, null, DamageInfo.SourceCategory.ThingOrUnknown));
                }
            }
        }

        private bool ShouldTakeDessicateDamage()
        {
            if (this.parent.ParentHolder != null)
            {
                Thing thing = this.parent.ParentHolder as Thing;
                if (thing != null && thing.def.category == ThingCategory.Building && thing.def.building.preventDeterioration)
                {
                    return false;
                }
            }
            return true;
        }

        private void StageChanged()
        {
            Corpse corpse = this.parent as Corpse;
            if (corpse != null)
            {
                corpse.RotStageChanged();
            }
        }

    }
}
