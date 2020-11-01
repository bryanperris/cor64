namespace cor64.Rdp.LLE {
    public struct TextureCoordinate {
        public int s;
        public int t;
        public int w;

        public TextureCoordinate(int s, int t, int w) {
            this.s = s;
            this.t = t;
            this.w = w;
        }

        public static TextureCoordinate operator +(TextureCoordinate left, TextureCoordinate right)
        {
            return new TextureCoordinate {
                s = left.s + right.s,
                t = left.t + right.t,
                w = left.w + right.w
            };
        }
    }
}