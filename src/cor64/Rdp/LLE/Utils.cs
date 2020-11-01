namespace cor64.Rdp.LLE {
    public static class Utils {
        public static int IRand(ref uint state) {
            state = (state * 0x343FD) + 0x269EC3;
            return (int)(state >> 16 & 0x7FFF);
        }

        // RGBA8888
        public static Color FromRGBA8888 (this uint value) {
            Color color;

            color.R = (int)(value >> 24) & 0xFF;
            color.G = (int)(value >> 16) & 0xFF;
            color.B = (int)(value >> 08) & 0xFF;
            color.A = (int)value & 0xFF;

            return color;
        }

        // RGBA5551 to RGBA8888
        public static Color FromRGBA5551(this uint value) {
            Color color;

            color.R = (int)(value >> 8) & 0xF8;
            color.G = (int)(value & 0x7C0) >> 3;
            color.B = (int)(value & 0x3E) << 2;
            color.A = (int)value & 1;

            return color;
        }

        public static int SignF(int value, int number) {
            return value | -(value & (1 << (number - 1)));
        }

        public static int Sign(int value, int number) {
            return (value & ((1 << number) - 1)) | -(value & (1 << (number - 1)));
        }

        public static int Clamp(this int value, int min, int max) {
            if (value < min) {
                return min;
            }
            else if (value > max) {
                return max;
            }
            else {
                return value;
            }
        }

        public static int TileRelative(int x, int y) {
            return x - (y << 3);
        }

        public static int PixelsToBytes(int pix, int size) {
            return (pix << size) >> 1;
        }

        public static int CommandId(int x) {
            return (x >> 24) & 0x3F;
        }

        public static bool IsTrue(this int value, int mask) {
            return (value & mask) != 0;
        }

        public static bool IsTrue(this uint value, uint mask) {
            return (value & mask) != 0;
        }

        public static bool IsTrue(this ulong value, uint mask) {
            return (value & mask) != 0;
        }

        public static bool IsTrue(this int value) {
            return value != 0;
        }

        public static bool IsTrue(this uint value) {
            return value != 0;
        }

        public static bool IsTrue(this ulong value) {
            return value != 0;
        }

        public static int ToBoolS32(this bool value) {
            return value ? 1 : 0;
        }

        public static int PixelsToBytesSpecial4(int pix, int siz) {
            return siz.IsTrue() ? PixelsToBytes(pix, siz) : pix;
        }
    }
}