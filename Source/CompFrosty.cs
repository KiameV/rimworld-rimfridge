using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace RimFridge
{
    internal class CompFrosty : ThingComp
    {
        public float Temp = 21f;

        public CompProperties_Frosty Props
        {
            get
            {
                return (CompProperties_Frosty)this.props;
            }
        }

        public override void PostIngested(Pawn ingester)
        {
            base.PostIngested(ingester);
            bool flag = this.Temp < 5f;
            if (flag)
            {
                ingester.needs.mood.thoughts.memories.TryGainMemory(this.Props.thought, null);
            }
        }

        public override void PostSplitOff(Thing piece)
        {
            ThingWithComps thingWithComps = piece as ThingWithComps;
            bool flag = ThingCompUtility.TryGetComp<CompFrosty>(thingWithComps) == null;
            if (flag)
            {
                CompFrosty compFrosty = new CompFrosty();
                thingWithComps.AllComps.Add(compFrosty);
                compFrosty.props = CompProperties_Frosty.Beer;
                compFrosty.parent = thingWithComps;
                compFrosty.Temp = this.Temp;
                ((TickList)typeof(TickManager).GetField("tickListRare", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Find.TickManager)).RegisterThing(thingWithComps);
            }
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            float num = 15f;
            if (this.parent.MapHeld != null)
            {
                num = GridsUtility.GetTemperature(this.parent.PositionHeld, this.parent.MapHeld);
            }
            CompEquippable comp = this.parent.GetComp<CompEquippable>();
            if (comp != null)
            {
                Pawn casterPawn = comp.PrimaryVerb.CasterPawn;
                if (casterPawn != null)
                {
                    num = GridsUtility.GetTemperature(casterPawn.PositionHeld, casterPawn.MapHeld);
                }
            }
            if (this.parent.Spawned)
            {
                List<Thing> thingList = GridsUtility.GetThingList(this.parent.PositionHeld, this.parent.MapHeld);
                for (int i = 0; i < thingList.Count; i++)
                {
                    bool flag4 = thingList[i] is Building_Refrigerator;
                    if (flag4)
                    {
                        Building_Refrigerator building_Refrigerator = thingList[i] as Building_Refrigerator;
                        num = building_Refrigerator.CurrentTemp;
                        break;
                    }
                }
            }
            this.Temp += (num - this.Temp) * 0.05f;
        }

        public override string CompInspectStringExtra()
        {
            return (this.Temp < 5f) ? "Frosty." : "";
        }
    }
}
