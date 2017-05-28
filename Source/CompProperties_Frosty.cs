using RimWorld;
using System;
using Verse;

namespace RimFridge
{
    internal class CompProperties_Frosty : CompProperties
    {
        private static CompProperties_Frosty _Beer;

        public ThoughtDef thought;

        public static CompProperties_Frosty Beer
        {
            get
            {
                bool flag = CompProperties_Frosty._Beer == null;
                if (flag)
                {
                    CompProperties_Frosty._Beer = new CompProperties_Frosty();
                    CompProperties_Frosty._Beer.thought = DefDatabase<ThoughtDef>.GetNamed("FrostyBeer", true);
                }
                return CompProperties_Frosty._Beer;
            }
        }
    }
}
