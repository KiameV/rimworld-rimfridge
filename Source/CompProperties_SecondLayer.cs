using Verse;

namespace RimFridge
{
    internal class CompProperties_SecondLayer : CompProperties
    {
        public GraphicData graphicData = null;

        public AltitudeLayer altitudeLayer = AltitudeLayer.MoteOverhead;

        public float Altitude => Altitudes.AltitudeFor(altitudeLayer);

        public CompProperties_SecondLayer()
        {
            compClass = typeof(CompSecondLayer);
        }
    }
}
