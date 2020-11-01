using System;

namespace cor64.Rdp
{

    public static class TriangleTools
    {
        private static ushort Unsigned16(double value)
        {
            var u16 = value % 1 * 65536.0d;
            return (ushort)u16;
        }

        public enum TriangleType {
            Fill,
            FillZ,
            Texture,
            TextureZ,
            Shade,
            ShadeZ,
            ShadeTex,
            ShadeTexZ
        }

        private static String GetCommandName(TriangleType type) {
            return type switch
            {
                TriangleType.Fill => "Fill_Triangle",
                TriangleType.FillZ => "Fill_ZBuffer_Triangle",
                TriangleType.Texture => "Texture_Triangle",
                TriangleType.TextureZ => "Texture_ZBuffer_Triangle",
                TriangleType.Shade => "Shade_Triangle",
                TriangleType.ShadeZ => "Shade_ZBuffer_Triangle",
                TriangleType.ShadeTex => "Shade_Texture_Triangle",
                TriangleType.ShadeTexZ => "Shade_Texture_Z_Buffer_Triangle",
                _ => "Triangle",
            };
        }

        public static String MajorTriangleCalc(Tuple<double, double> low, Tuple<double, double> middle, Tuple<double, double> major, TriangleType type, bool isRight = false)
        {
            var X0 = low.Item1;
            var Y0 = low.Item2;
            var X1 = middle.Item1;
            var Y1 = middle.Item2;
            var X2 = major.Item1;
            var Y2 = major.Item2;

            var XL = X1;
            var XM = X2;

            var DxLDy = 0.0d;
            var DxMDy = 0.0d;
            var DxHDy = 0.0d;

            int dir;


            var triwinding =
                (X0 * Y1) - (X1 * Y0) +
                (X1 * Y2) - (X2 * Y1) +
                (X2 * Y0) - (X0 * Y2);

            if (triwinding > 0.0d)
            {
                // Clockwise Winding: Left Major Triangle
                dir = 0;
            }
            else
            {
                // Major Right Triangle
                dir = 1;
            }

            // If Left Major
            if (!isRight)
            {
                if ((Y1 - Y0) != 0)
                {
                    DxLDy = (X1 - X0) / (Y1 - Y0);
                }

                if ((Y2 - Y1) != 0)
                {
                    DxMDy = (X2 - X1) / (Y2 - Y1);
                }

                if ((Y2 - Y0) != 0)
                {
                    DxHDy = (X2 - X0) / (Y2 - Y0);
                }
            }
            else
            {
                if ((Y0 - Y1) != 0)
                {
                    DxLDy = (X0 - X1) / (Y0 - Y1);
                }

                if ((Y1 - Y2) != 0)
                {
                    DxMDy = (X1 - X2) / (Y1 - Y2);
                }

                if ((Y0 - Y2) != 0)
                {
                    DxHDy = (X0 - X2) / (Y0 - Y2);
                }
            }


            // Convert Into 11.2 Fixed Point Format
            Y0 *= 4.0d;
            Y1 *= 4.0d;
            Y2 *= 4.0d;

            // Convert to 16-Bit Fraction
            var XLf = Unsigned16(XL);
            var XMf = Unsigned16(XM);

            // Convert to 16-bit signed integer
            if (DxLDy < 0.0d && DxLDy > -1.0d)
            {
                DxLDy--;
            }

            if (DxMDy < 0.0d && DxMDy > -1.0d)
            {
                DxMDy--;
            }

            if (DxHDy < 0.0d && DxHDy > -1.0d)
            {
                DxHDy--;
            }

            var DxLDyf = Unsigned16(DxLDy);
            var DxMDyf = Unsigned16(DxMDy);
            var DxHDyf = Unsigned16(DxHDy);

            DxLDy = Math.Truncate(DxLDy);
            DxMDy = Math.Truncate(DxMDy);
            DxHDy = Math.Truncate(DxHDy);

            return String.Format("{0} {1}, 0, 0, {2},{3},{4}, {5},{6}, {7},{8}, {9},{10}, {11},{12}, {13},{14}, {15},{16}",
                GetCommandName(type),

                dir,

                Y0,
                Y1,
                Y2,

                XL,
                XLf,
                DxLDy,
                DxLDyf,

                XM,
                XMf,
                DxHDy,
                DxHDyf,

                XM,
                XMf,
                DxMDy,
                DxMDyf
            );
        }
    }
}