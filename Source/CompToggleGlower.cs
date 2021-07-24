using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace RimFridge
{
    [StaticConstructorOnStartup]
    static class IconUtil
    {
        public static Texture2D Normal;
        public static Texture2D Dark;

        static IconUtil()
        {
            Normal = ContentFinder<Texture2D>.Get("UI/Icons/normal", true);
            Dark = ContentFinder<Texture2D>.Get("UI/Icons/dark", true);
        }
    }

    public class CompProperties_ToggleGlower : CompProperties_Glower
    {
        public CompProperties_ToggleGlower()
        {
            base.compClass = typeof(CompToggleGlower);
        }
    }

    class CompToggleGlower : CompGlower
    {
        bool isDarklight = false;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            foreach (var g in base.CompGetGizmosExtra())
                yield return g;
            if (ModsConfig.IdeologyActive)
            {
                Texture2D icon;

                if (isDarklight)
                    icon = IconUtil.Dark;
                else
                    icon = IconUtil.Normal;

                yield return new Command_Action
                {
                    action = delegate {
                        isDarklight = !isDarklight;
                        SetLightColor();
                        base.parent.Map.glowGrid.DeRegisterGlower(this);
                        base.parent.Map.glowGrid.RegisterGlower(this);
                        base.parent.Map.mapDrawer.MapMeshDirty(base.parent.Position, MapMeshFlag.Things);
                    },
                    defaultLabel = "RimFridge.ToggleGlowColor".Translate(),
                    defaultDesc = "RimFridge.ToggleGlowColorDesc".Translate(),
                    icon = icon
                };
            }
        }

        public override void PostSpawnSetup(bool respawningAfterLoad)
        {
            base.PostSpawnSetup(respawningAfterLoad);
            SetLightColor();

        }

        private void SetLightColor()
        {
            if (isDarklight)
                base.Props.glowColor = new ColorInt(78, 226, 229, 0);
            else
                base.Props.glowColor = new ColorInt(89, 188, 255, 0); // new ColorInt(252, 187, 113, 0);
        }

        public override void PostExposeData()
        {
            base.PostExposeData();
            Scribe_Values.Look(ref isDarklight, "isDarklight");
        }
    }
}
