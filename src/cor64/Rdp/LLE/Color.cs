using System;
using System.Linq;
using System.Security.Cryptography;
namespace cor64.Rdp.LLE {
    public struct Color {
        public int R;
        public int G;
        public int B;
        public int A;

        public Color(int r, int g, int b, int a) {
            this.R = r;
            this.G = g;
            this.B = b;
            this.A = a;
        }

        public Color(uint color) {
            this.R = (int)(color >> 24);
            this.G = (int)((color >> 16) & 0xFF);
            this.B = (int)((color >> 8) & 0xFF);
            this.A = (int)(color & 0xFF);
        }

        public Color(String hex) {
            var bytes = Enumerable.Range(0, hex.Length)
                     .Where(x => x % 2 == 0)
                     .Select(x => Convert.ToByte(hex.Substring(x, 2), 16))
                     .ToArray();

            R = bytes[0];
            G = bytes[1];
            B = bytes[2];
            A = bytes[3];
        }

        public void SetFromSingle(int value) {
            this.R = value;
            this.G = value;
            this.B = value;
            this.A = value;
        }

        public void SetRGBFromSingle(int value) {
            this.R = value;
            this.G = value;
            this.B = value;
        }

        public void Testing_SetRed() {
            R = 0xFF;
            G = 0;
            B = 0;
            A = 0;
        }

        public uint ToValue() {
            return ((uint)R << 24) | ((uint)G << 16) | ((uint)B << 8) | (uint)A;
        }

        public static readonly Color Zero = new Color(CTS.ZeroColor, CTS.ZeroColor, CTS.ZeroColor, CTS.ZeroColor);

        public static readonly Color One = new Color(CTS.OneColor, CTS.OneColor, CTS.OneColor, CTS.OneColor);

        public override string ToString() {
            return String.Format("{0:X8}{1:X8}{2:X8}{3:X8}", R, G, B, A);
        }
    }
}