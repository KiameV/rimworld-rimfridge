using UnityEngine;
using Verse;

namespace RimFridge
{
    public class Dialog_Rename : Window
    {
        protected virtual int MaxNameLength
        {
            get
            {
                return 28;
            }
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(280f, 175f);
            }
        }

        private string inputText = "";
        private Building_Refrigerator fridge;

        public Dialog_Rename(Building_Refrigerator fridge)
        {
            this.forcePause = true;
            this.doCloseX = true;
            this.closeOnEscapeKey = true;
            this.absorbInputAroundWindow = true;
            this.closeOnClickedOutside = true;
            this.fridge = fridge;
            this.inputText = fridge.Label;
        }

        protected virtual AcceptanceReport NameIsValid(string name)
        {
            if (name.Length == 0)
            {
                return false;
            }
            return true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;
            bool flag = false;
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
            {
                flag = true;
                Event.current.Use();
            }
            string text = Widgets.TextField(new Rect(0f, 15f, inRect.width, 35f), this.inputText);
            if (text.Length < this.MaxNameLength)
            {
                this.inputText = text;
            }
            if (Widgets.ButtonText(new Rect(15f, inRect.height - 35f - 15f, inRect.width - 15f - 15f, 35f), "OK", true, false, true) || flag)
            {
                AcceptanceReport acceptanceReport = this.NameIsValid(this.inputText);
                if (acceptanceReport.Accepted)
                {
                    this.fridge.label = this.inputText;
                    Find.WindowStack.TryRemove(this, true);
                }
            }
        }
    }
}