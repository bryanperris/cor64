using System;
using System.Runtime.InteropServices;
namespace cor64.Rdp.LLE
{
    public static class Coverage
    {
        public const int CVG_CLAMP = 0;
        public const int CVG_WRAP = 1;
        public const int CVG_ZAP = 2;
        public const int CVG_SAVE = 3;

        private static readonly byte[] s_YArray = { 0, 0, 1, 0, 2, 0, 1, 0, 3, 0, 1, 0, 2, 0, 1, 0 };
        private static readonly byte[] s_XArray = { 0, 3, 2, 2, 1, 1, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0 };

        public struct Data
        {
            public byte Coverage;
            public bool CvBit;
            public byte x;
            public byte y;
        }

        private static readonly Data[] s_CoverageLut = new Data[0x100];

        static Coverage()
        {
            for (int i = 0; i < 0x100; i++)
            {
                var mask = DecompressMask((uint)i);

                s_CoverageLut[i].Coverage = 0;
                s_CoverageLut[i].CvBit = (i >> 7).IsTrue(1);

                for (int k = 0; k < 8; k++)
                {
                    s_CoverageLut[i].Coverage += (byte)((i >> k) & 1);
                }

                ushort maskx;
                ushort masky = 0;

                for (int k = 0; k < 4; k++)
                {
                    ushort temp = mask;
                    temp &= (ushort)((0xF000 >> (k << 2)) > 0 ? 1 : 0);
                    masky |= (ushort)(temp << k);
                }

                byte y = s_YArray[masky];

                maskx = (ushort)((mask & (0xF000 >> (y << 2))) >> ((y ^ 3) << 2));

                s_CoverageLut[i].x = s_XArray[maskx];
                s_CoverageLut[i].y = y;
            }
        }

        private static uint RightHex(int x, int fmask)
        {
            int covered = ((x & 7) + 1) >> 1;
            return (0xF0U >> covered) & (uint)fmask;
        }

        private static uint LeftHex(int x, int fmask)
        {
            int covered = ((x & 7) + 1) >> 1;
            return (0xFU >> covered) & (uint)fmask;
        }

        public unsafe static void Compute(State state, int scanline, bool flip = false)
        {
            uint minorHex(int x, int mask)
            {
                return flip ? RightHex(x, mask) : LeftHex(x, mask);
            }

            uint majorHex(int x, int mask)
            {
                return flip ? LeftHex(x, mask) : RightHex(x, mask);
            }

            int purgeStart = flip ? state.SpanBuffer[scanline].rx : state.SpanBuffer[scanline].lx;
            int purgeEnd = flip ? state.SpanBuffer[scanline].lx : state.SpanBuffer[scanline].rx;
            int length = purgeEnd - purgeStart;

            if (length >= 0)
            {
                for (int i = 0; i <= length; i++)
                {
                    state.ConverageBuffer[purgeStart + i] = 0xFF;
                }

                for (int i = 0; i < 4; i++)
                {
                    int fmask = 0xA >> (i & 1);
                    int maskShift = (i - 2) & 4;
                    byte fmaskShifted = (byte)~(fmask << maskShift);

                    if (!state.SpanBuffer[scanline].InvalidYScan[i])
                    {
                        int minorCur = state.SpanBuffer[scanline].minorx[i];
                        int majorCur = state.SpanBuffer[scanline].majorx[i];
                        int minorCurInt = minorCur >> 3;
                        int majorCurInt = majorCur >> 3;

                        int a = flip ? majorCurInt : minorCurInt;
                        int b = flip ? minorCurInt : majorCurInt;

                        for (int k = purgeStart; k <= a; k++)
                        {
                            state.ConverageBuffer[k] &= fmaskShifted;
                        }

                        for (int k = b; k <= purgeEnd; k++)
                        {
                            state.ConverageBuffer[k] &= fmaskShifted;
                        }

                        if (b > a)
                        {
                            state.ConverageBuffer[minorCurInt] |= (byte)(minorHex(minorCur, fmask) << maskShift);
                            state.ConverageBuffer[majorCurInt] |= (byte)(majorHex(majorCur, fmask) << maskShift);
                        }

                        if (minorCurInt == majorCurInt)
                        {
                            uint sameCvg = minorHex(minorCur, fmask) & majorHex(majorCur, fmask);
                            state.ConverageBuffer[majorCurInt] |= (byte)(sameCvg << maskShift);
                        }
                    }
                    else
                    {
                        for (int k = purgeStart; k <= purgeEnd; k++)
                        {
                            state.ConverageBuffer[k] &= fmaskShifted;
                        }
                    }
                }
            }
        }

        public static int FinializeSpanAlpha(int cvgDest, bool enableBlend, uint pixelCoverage, uint pixelMemCoverage)
        {
            switch (cvgDest)
            {
                case CVG_CLAMP:
                    {
                        int finalCvg = !enableBlend ? (int)pixelCoverage - 1 : (int)pixelCoverage + (int)pixelMemCoverage;
                        return !finalCvg.IsTrue(8) ? finalCvg & 7 : 7;
                    }

                case CVG_WRAP:
                    {
                        return ((int)pixelCoverage + (int)pixelMemCoverage) & 7;
                    }

                case CVG_ZAP:
                    {
                        return 7;
                    }

                case CVG_SAVE:
                    {
                        return (int)pixelMemCoverage;
                    }

                default: throw new RdpException("Invalid coverage type for FinalizeSpanAlpha");
            }
        }

        public static ushort DecompressMask(uint x)
        {
            return (ushort)((x & 0x5) | ((x & 0x5A) << 4) | ((x & 0xA0) << 8));
        }

        public static void LookupMaskDerivatives(byte mask, ref byte x, ref byte y, ref uint coverage, ref bool cvgbit)
        {
            coverage = s_CoverageLut[mask].Coverage;
            cvgbit = s_CoverageLut[mask].CvBit;
            x = s_CoverageLut[mask].x;
            y = s_CoverageLut[mask].y;
        }
    }
}