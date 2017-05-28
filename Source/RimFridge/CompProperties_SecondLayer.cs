using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace RimFridge
{
    class CompProperties_SecondLayer : CompProperties
    {
        public GraphicData graphicData;
        public AltitudeLayer altitudeLayer;

        public float Altitude
        {
            get
            {
                return Altitudes.AltitudeFor(this.altitudeLayer);
            }
        }

        public CompProperties_SecondLayer()
        {
            this.compClass = typeof(CompSecondLayer);
        }
    }
}
