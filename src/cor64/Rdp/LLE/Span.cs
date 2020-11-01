namespace cor64.Rdp.LLE {
    public unsafe struct Span {
        public int lx;
        public int rx;
        public int unscrx;
        public bool ValidLine;
        public int r, g, b, a, s, t, w, z;
        public fixed int majorx[4];
        public fixed int minorx[4];
        public fixed bool InvalidYScan[4];
    }
}