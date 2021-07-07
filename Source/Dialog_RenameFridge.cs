using UnityEngine;
using Verse;


namespace RimFridge
{
    public class Dialog_RenameFridge : Dialog_Rename
    {
        public Dialog_RenameFridge(CompRefrigerator fridge)
        {
            forcePause = true;
            doCloseX = true;
            closeOnClickedOutside = true;
            absorbInputAroundWindow = true;
            closeOnClickedOutside = true;
            curName = fridge.parent.Label;
            this.fridge = fridge;
        }
          public override Vector2 InitialSize
        {
            get
            {
                var o = base.InitialSize;
                o.y += 50f;
                return o;
            }
        }

        private readonly CompRefrigerator fridge;

        protected override void SetName(string name)
        {
            fridge.buildingLabel = name;
            //Messages.Message("RimFridge_GainsName".Translate(this.fridge.parent.def.label, fridge.parent.Label),
            //                 MessageTypeDefOf.TaskCompletion, false);
        }

        protected override AcceptanceReport NameIsValid(string name)
        {
            if (name.Length == 0) return true;
            AcceptanceReport result = base.NameIsValid(name);
            return !result.Accepted ? result : AcceptanceReport.WasAccepted;
        }

        public override void DoWindowContents(Rect inRect)
        {
            base.DoWindowContents(inRect);

            if (Widgets.ButtonText(new Rect(15f, inRect.height - 35f - 15f - 50f, inRect.width - 15f - 15f, 35f), "ResetButton".Translate(),
                          true, false, true))
            {
                SetName("");
                Find.WindowStack.TryRemove(this, true);
            }
        }
    }
}
