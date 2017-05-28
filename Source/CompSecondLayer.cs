using System;
using Verse;

namespace RimFridge
{
    internal class CompSecondLayer : ThingComp
    {
        private Graphic graphicInt;

        public CompProperties_SecondLayer Props
        {
            get
            {
                return (CompProperties_SecondLayer)this.props;
            }
        }

        public virtual Graphic Graphic
        {
            get
            {
                bool flag = this.graphicInt == null;
                Graphic badGraphic;
                if (flag)
                {
                    bool flag2 = this.Props.graphicData == null;
                    if (flag2)
                    {
                        Log.ErrorOnce(this.parent.def + " has no SecondLayer graphicData but we are trying to access it.", 764532);
                        badGraphic = BaseContent.BadGraphic;
                        return badGraphic;
                    }
                    this.graphicInt = this.Props.graphicData.GraphicColoredFor(this.parent);
                }
                badGraphic = this.graphicInt;
                return badGraphic;
            }
        }

        public override void PostDraw()
        {
            base.PostDraw();
            this.Graphic.Draw(Gen.TrueCenter(this.parent.Position, this.parent.Rotation, this.parent.def.size, this.Props.Altitude), this.parent.Rotation, this.parent);
        }
    }
}
