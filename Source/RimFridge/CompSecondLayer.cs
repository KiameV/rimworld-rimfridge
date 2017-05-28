using System;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace RimFridge
{
    class CompSecondLayer : ThingComp
    {
        public CompProperties_SecondLayer Props
        {
            get
            {
                return (CompProperties_SecondLayer)props;
            }
        }

        private Graphic graphicInt;

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
                    this.graphicInt = Props.graphicData.GraphicColoredFor(parent);
                }
                return this.graphicInt;
            }
        }

        public override void PostDraw()
        {
            base.PostDraw();
            Graphic.Draw(Gen.TrueCenter(parent.Position, parent.Rotation, parent.def.size, Props.Altitude), parent.Rotation, parent);
        }
    }
}
