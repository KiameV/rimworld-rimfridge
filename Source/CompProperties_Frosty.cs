using RimWorld;
using Verse;

namespace RimFridge
{
    internal class CompProperties_Frosty : CompProperties
    {
        private static CompProperties_Frosty beer;

        public ThoughtDef thought;

        public static CompProperties_Frosty Beer
        {
            get
            {
                if (beer == null)
                {
                    beer = new CompProperties_Frosty();
                    beer.thought = DefDatabase<ThoughtDef>.GetNamed("FrostyBeer", true);
                }
                return beer;
            }
        }
    }
}
