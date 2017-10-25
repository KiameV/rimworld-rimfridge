using RimWorld;
using UnityEngine;
using Verse;

namespace RimFridge
{
    public class SettingsController : Mod
    {
        public SettingsController(ModContentPack content) : base(content)
        {
            base.GetSettings<Settings>();
        }

        public override string SettingsCategory()
        {
            return "RimFridge";
        }

        public override void DoSettingsWindowContents(Rect rect)
        {
            GUI.BeginGroup(new Rect(0, 60, 600, 200));
            Text.Font = GameFont.Small;
            Widgets.Label(new Rect(0, 40, 300, 20), "Modify Base Power Requirement" + ":");
            Settings.PowerFactor.AsString = Widgets.TextField(new Rect(320, 40, 100, 20), Settings.PowerFactor.AsString);
            if (Widgets.ButtonText(new Rect(320, 65, 100, 20), "Apply"))
            {
                if (Settings.PowerFactor.ValidateInput())
                {
                    base.GetSettings<Settings>().Write();
                    Messages.Message("New Power Factor Applied", MessageTypeDefOf.PositiveEvent);
                }
            }
            Widgets.Label(new Rect(20, 100, 400, 30), "<new power usage> = <input value> * <original power usage>");
            if (Current.Game != null)
            {
                RimFridgeSettingsUtil.ApplyFactor(Settings.PowerFactor.AsFloat);
            }
            GUI.EndGroup();
        }
    }

    class Settings : ModSettings
    {
        public static readonly FloatInput PowerFactor = new FloatInput("Base Power Factor");

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look<string>(ref (PowerFactor.AsString), "RimFridge.PowerFactor", "1.00", false);
        }
    }
}