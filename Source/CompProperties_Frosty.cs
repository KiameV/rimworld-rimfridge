using RimWorld;
using Verse;

namespace RimFridge
{
    internal class CompProperties_Frosty : CompProperties
    {
        private static ThoughtDef frosty = null;

        private static CompProperties_Frosty beer;

        public ThoughtDef thought;

        public static CompProperties_Frosty Beer
        {
            get
            {
                if (beer == null)
                {
                    if (frosty == null)
                        frosty = DefDatabase<ThoughtDef>.GetNamed("FrostyBeer", true);

                    beer = new CompProperties_Frosty
                    {
                        thought = frosty
                    };
                }
                return beer;
            }
        }
    }
}
