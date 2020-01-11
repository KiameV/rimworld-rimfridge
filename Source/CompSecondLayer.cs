using RimWorld;
using Verse;

namespace RimFridge
{
    internal class CompSecondLayer : ThingComp
    {
        private Graphic graphicInt;

        public CompProperties_SecondLayer Props => (CompProperties_SecondLayer)props;

        public virtual Graphic Graphic
        {
            get
            {
                if (graphicInt == null)
                {
                    if (Props.graphicData == null)
                    {
                        Log.ErrorOnce(parent.def + " has no SecondLayer graphicData but we are trying to access it.", 764532);
                        return BaseContent.BadGraphic;
                    }
                    graphicInt = Props.graphicData.GraphicColoredFor(parent);
                }
                return graphicInt;
            }
        }

        public override void PostDraw()
        {
            base.PostDraw();
            Graphic.Draw(GenThing.TrueCenter(parent.Position, parent.Rotation, parent.def.size, Props.Altitude), parent.Rotation, parent);
        }
    }
}
