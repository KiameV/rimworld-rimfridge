using Verse;

namespace RimFridge
{
    class FloatInput
    {
        private readonly string name;
        public string AsString;

        public FloatInput(string name, float initialValue = 1f)
        {
            this.name = name;
            this.AsString = initialValue.ToString();
        }

        public float AsFloat
        {
            get
            {
                if (this.ValidateInput())
                {
                    return float.Parse(AsString);
                }
                return 1f;
            }
            set
            {
                this.AsString = value.ToString();
            }
        }

        public bool ValidateInput()
        {
            float f;
            if (float.TryParse(AsString, out f))
            {
                if (f <= 0)
                {
                    Messages.Message(name + " cannot be less than or equal to 0.", MessageSound.RejectInput);
                    return false;
                }
            }
            else
            {
                Messages.Message("Unable to parse " + name +  " to a number.", MessageSound.RejectInput);
                return false;
            }
            return true;
        }

        public void Copy(FloatInput fi)
        {
            this.AsString = fi.AsString;
        }
    }
}
