namespace cor64.Rdp.LLE
{
    public class ColorRef
    {
        public Color color;

        public ColorRef(Color color)
        {
            this.color = color;
        }

        public ColorRef()
        {
            color = Color.Zero;
        }

        public virtual void SetColor(Color color) {
            this.color = color;
        }

        public void SetColor(ColorRef color) {
            SetColor(color.GetColor());
        }

        public Color GetColor() => color;

        public virtual void SetRGBFromSingle(int value) {
            color.SetRGBFromSingle(value);
        }

        /// <summary>
        /// This allows a ColorRef class wrap another
        /// </summary>
        /// <returns></returns>
        public virtual ColorRef GetSource() {
            return this;
        }

        public virtual int R {
            get => color.R;
            set => color.R = value;
        }

        public virtual int G {
            get => color.G;
            set => color.G = value;
        }

        public virtual int B {
            get => color.B;
            set => color.B = value;
        }

        public virtual int A {
            get => color.A;
            set => color.A = value;
        }
    }
}