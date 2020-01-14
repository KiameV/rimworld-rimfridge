using System.Collections.Generic;
using Verse;

namespace RimFridge
{
    public class CompProperties_Refrigerator : CompProperties
    {
        public CompProperties_Refrigerator()
        {
            compClass = typeof(CompRefrigerator);
        }

        public List<string> drinksBestCold;
        public float defaultDesiredTemperature = -5f;
    }
}
