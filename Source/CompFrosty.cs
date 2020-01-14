using System.Collections.Generic;
using System.Reflection;
using Verse;

namespace RimFridge
{
    internal class CompFrosty : ThingComp
    {
        // Most beer's ideal temperature is around 8 degC
        private const float IDEAL_TEMPERATURE = 8f;

        // Starting temperature
        public float temperature = 21f;

        public CompProperties_Frosty Props => (CompProperties_Frosty)props;

        public override void PostIngested(Pawn ingester)
        {
            base.PostIngested(ingester);
            if(temperature <= IDEAL_TEMPERATURE)
            {
                ingester.needs.mood.thoughts.memories.TryGainMemory(Props.thought, null);
            }
        }

        public override void PostSplitOff(Thing piece)
        {
            ThingWithComps thingWithComps = piece as ThingWithComps;
            
            if(ThingCompUtility.TryGetComp<CompFrosty>(thingWithComps) == null)
            {
                CompFrosty compFrosty = new CompFrosty();
                thingWithComps.AllComps.Add(compFrosty);
                compFrosty.props = CompProperties_Frosty.Beer;
                compFrosty.parent = thingWithComps;
                compFrosty.temperature = temperature;
                ((TickList)typeof(TickManager).GetField("tickListRare", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(Find.TickManager)).RegisterThing(thingWithComps);
            }
        }

        public override void CompTickRare()
        {
            base.CompTickRare();
            float num = 15f;
            if (parent.MapHeld != null)
            {
                num = GridsUtility.GetTemperature(parent.PositionHeld, parent.MapHeld);
            }
            CompEquippable comp = parent.GetComp<CompEquippable>();
            if (comp != null)
            {
                Pawn casterPawn = comp.PrimaryVerb.CasterPawn;
                if (casterPawn != null)
                {
                    num = GridsUtility.GetTemperature(casterPawn.PositionHeld, casterPawn.MapHeld);
                }
            }
            if (parent.Spawned)
            {
                List<Thing> thingList = GridsUtility.GetThingList(parent.PositionHeld, parent.MapHeld);
                for (int i = 0; i < thingList.Count; i++)
                {
                    CompRefrigerator fridge = ThingCompUtility.TryGetComp<CompRefrigerator>(thingList[i]);
                    if (fridge != null)
                    {
                        num = fridge.currentTemp;
                        break;
                    }
                }
            }
            temperature += (num - temperature) * 0.05f;
        }

        public override string CompInspectStringExtra()
        {
            return (temperature <= IDEAL_TEMPERATURE) ? "Frosty" : "";
        }
    }
}
