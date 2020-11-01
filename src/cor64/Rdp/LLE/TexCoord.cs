namespace cor64.Rdp.LLE {
    public static class TexCoord {
        public static readonly int[] s_MaskTable = new int[16];
        private static readonly int[] s_DivTable = new int[0x8000];

        public static int LookupMaskBits(int index) => s_MaskTable[index];

        private static readonly int[] NormalPointTable = {
            0x4000, 0x3F04, 0x3E10, 0x3D22, 0x3C3C, 0x3B5D, 0x3A83, 0x39B1,
            0x38E4, 0x381C, 0x375A, 0x369D, 0x35E5, 0x3532, 0x3483, 0x33D9,
            0x3333, 0x3291, 0x31F4, 0x3159, 0x30C3, 0x3030, 0x2FA1, 0x2F15,
            0x2E8C, 0x2E06, 0x2D83, 0x2D03, 0x2C86, 0x2C0B, 0x2B93, 0x2B1E,
            0x2AAB, 0x2A3A, 0x29CC, 0x2960, 0x28F6, 0x288E, 0x2828, 0x27C4,
            0x2762, 0x2702, 0x26A4, 0x2648, 0x25ED, 0x2594, 0x253D, 0x24E7,
            0x2492, 0x243F, 0x23EE, 0x239E, 0x234F, 0x2302, 0x22B6, 0x226C,
            0x2222, 0x21DA, 0x2193, 0x214D, 0x2108, 0x20C5, 0x2082, 0x2041
        };

        private static readonly int[] NormalSlopeTable = {
            0xF03, 0xF0B, 0xF11, 0xF19, 0xF20, 0xF25, 0xF2D, 0xF32,
            0xF37, 0xF3D, 0xF42, 0xF47, 0xF4C, 0xF50, 0xF55, 0xF59,
            0xF5D, 0xF62, 0xF64, 0xF69, 0xF6C, 0xF70, 0xF73, 0xF76,
            0xF79, 0xF7C, 0xF7F, 0xF82, 0xF84, 0xF87, 0xF8A, 0xF8C,
            0xF8E, 0xF91, 0xF93, 0xF95, 0xF97, 0xF99, 0xF9B, 0xF9D,
            0xF9F, 0xFA1, 0xFA3, 0xFA4, 0xFA6, 0xFA8, 0xFA9, 0xFAA,
            0xFAC, 0xFAE, 0xFAF, 0xFB0, 0xFB2, 0xFB3, 0xFB5, 0xFB5,
            0xFB7, 0xFB8, 0xFB9, 0xFBA, 0xFBC, 0xFBC, 0xFBE, 0xFBE
        };

        static TexCoord() {
            DivTableInit(s_DivTable);
            MaskTableInit(s_MaskTable);
        }

        private static void DivTableInit(int[] table) {
            int tempPoint, tempSlope;
            int normOut;
            int wnorm;
            int shift;
            int tlu_rcp;

            for (int i = 0; i < table.Length; i++) {
                int k;

                for (k = 1; k <= 14 && ((i << k) & 0x8000) == 0; k++) ;

                shift = k - 1;
                normOut = (i << shift) & 0x3FFF;
                wnorm = (normOut & 0xFF) << 2;
                normOut >>= 8;

                tempPoint = NormalPointTable[normOut];
                tempSlope = NormalSlopeTable[normOut];

                tempSlope = (tempSlope | ~0x3FF) + 1;

                tlu_rcp = (((tempSlope * wnorm) >> 10) + tempPoint) & 0x7FFF;

                table[i] = shift | (tlu_rcp << 4);
            }
        }

        private static void MaskTableInit(int[] table) {
            table[0] = 0x3FF;
            const ushort mask = 0xFFFF;

            for (int i = 1; i < table.Length; i++) {
                var x = mask >> (16 - i);
                table[i] = x & 0x3FF;
            }
        }

        public static void PerspectiveDivide(int ss, int st, int sw, ref int sss, ref int sst) {
            bool wCarry = (short)sw <= 0;
            int tempS, tempT;
            int overunderS = 0;
            int overunderT = 0;
            int shift = s_DivTable[sw & 0x7FFF];
            int tlu_rcp = shift >> 4;

            shift &= 0xF;

            int sprod = (short)ss * tlu_rcp;
            int tprod = (short)st * tlu_rcp;

            int tempmask = ((1 << 30) - 1) & -((1 << 29) >> shift);

            int outOfBoundsS = sprod & tempmask;
            int outOfBoundsT = tprod & tempmask;

            if (shift != 0xE) {
                int shiftValue = 13 - shift;
                tempS = sprod >>= shiftValue;
                tempT = tprod >>= shiftValue;
            }
            else {
                tempS = sprod << 1;
                tempT = tprod << 1;
            }

            if (outOfBoundsS != tempmask && outOfBoundsS != 0) {
                overunderS = (sprod & (1 << 29)) == 0 ? 2 << 17 : 1 << 17;
            }

            if (outOfBoundsT != tempmask && outOfBoundsT != 0) {
                overunderT = (tprod & (1 << 29)) == 0 ? 2 << 17 : 1 << 17;
            }

            if (wCarry) {
                overunderS |= 2 << 17;
                overunderT |= 2 << 17;
            }

            sss = (tempS & 0x1FFFF) | overunderS;
            sst = (tempT & 0x1FFFF) | overunderT;
        }

        public static void NonPerspectiveDivide(int ss, int st, int sw, ref int sss, ref int sst) {
            sss = (short)ss & 0x1FFFF;
            sst = (short)st & 0x1FFFF;
        }

        public static void MaskCopy(Tile tile, ref int s, ref int s1, ref int s2, ref int s3, ref int t) {
            if (tile.S.Mask != 0) {
                if (tile.S.UseMirroring) {
                    int wrapThreshold = tile.S.RdpClampedMask;

                    int wrap = (s >> wrapThreshold) & 1;
                    s ^= -wrap;

                    wrap = (s1 >> wrapThreshold) & 1;
                    s1 ^= -wrap;

                    wrap = (s2 >> wrapThreshold) & 1;
                    s2 ^= -wrap;

                    wrap = (s3 >> wrapThreshold) & 1;
                    s3 ^= -wrap;
                }

                int maskBits = s_MaskTable[tile.S.Mask];

                s &= maskBits;
                s1 &= maskBits;
                s2 &= maskBits;
                s3 &= maskBits;
            }

            if (tile.T.Mask != 0) {
                if (tile.T.UseMirroring) {
                    int wrap = (t >> tile.T.RdpClampedMask) & 1;
                    t ^= -wrap;
                }

                t &= s_MaskTable[tile.T.Mask];
            }
        }

        public static void ShiftCopy(Tile tile, ref int s, ref int t) {
            static int SC(int shifter, int coord) {
                if (shifter < 11) {
                    coord = (short)coord;
                    coord >>= shifter;
                }
                else {
                    coord <<= 16 - shifter;
                    coord = (short)coord;
                }

                return coord;
            }

            s = SC(tile.S.Shift, s);
            t = SC(tile.T.Shift, t);
        }

        public static void ShiftCycle(Tile tile, ref int s, ref int t, ref bool maxS, ref bool maxT) {
            ShiftCopy(tile, ref s, ref t);

            maxS = (s >> 3) >= tile.S.CoordHigh;
            maxT = (t >> 3) >= tile.T.CoordHigh;
        }

        public static void ClampCycle(Tile tile, ref int s, ref int t, ref int fracS, ref int fracT, bool maxS, bool maxT) {

            static int Clamp(int coord, bool useClamping, int clampDiff, bool max, ref int fraction) {
                if (useClamping) {
                    if (max) {
                        fraction = 0;
                        return clampDiff;
                    }
                    else if (!coord.IsTrue(0x10000)) {
                        return coord >> 5;
                    }
                    else {
                        fraction = 0;
                        return 0;
                    }
                }
                else {
                    return coord >> 5;
                }
            }

            s = Clamp(s, tile.S.RdpUseClamping, tile.S.RdpClampDiff, maxS, ref fracS);
            t = Clamp(t, tile.T.RdpUseClamping, tile.T.RdpClampDiff, maxT, ref fracT);
        }

        public static void ClampCycle(Tile tile, ref int s, ref int t, bool maxS, bool maxT) {
            int unusedFractS = 0;
            int unusedFractT = 0;

            ClampCycle(tile, ref s, ref t, ref unusedFractS, ref unusedFractT, maxS, maxT);
        }

        public static void PipelineCopy(Tile tile, ref int sss0, ref int sss1, ref int sss2, ref int sss3, ref int sst) {
            int ss0 = sss0;
            int st = sst;

            ShiftCopy(tile, ref ss0, ref st);

            ss0 = Utils.TileRelative(ss0,tile.S.CoordLow);
            st = Utils.TileRelative(st,tile.T.CoordLow);

            ss0 >>= 5;
            st >>= 5;

            int ss1 = ss0 + 1;
            int ss2 = ss0 + 2;
            int ss3 = ss0 + 3;

            MaskCopy(tile, ref ss0, ref ss1, ref ss2, ref ss3, ref st);

            sss0 = ss0;
            sss1 = ss1;
            sss2 = ss2;
            sss3 = ss3;
            sst = st;
        }

        public static void PipelineLoad(Tile tile, ref int sss, ref int sst, bool isQuad) {
            int shift = isQuad ? 3 : 5;
            sss = Utils.TileRelative((short)(ushort)sss, tile.S.CoordLow) >> shift;
            sst = Utils.TileRelative((short)(ushort)sst, tile.T.CoordLow) >> shift;
        }

        public static void MaskCoupled(Tile tile, ref int s, ref int sdiff, ref int t, ref int tdiff)
        {
            static int MC(ref int coord, int mask, bool useMirroring, int clampedMask, int m = 0) {
                if (mask.IsTrue()) {
                    int maskBits = s_MaskTable[mask];

                    if (useMirroring) {
                        int wrapThreshold = clampedMask;
                        int wrap = (coord >> wrapThreshold) & 1;

                        coord ^= -wrap;
                        coord &= maskBits;

                        if (((coord - wrap) & maskBits) == maskBits)
                        {
                            return 0;
                        }
                        else
                        {
                            return 1 - (wrap << 1);
                        }
                    }
                    else {
                        coord &= maskBits;
                        return coord == maskBits ? -(coord & m) : 1;
                    }
                }
                else {
                    return 1;
                }
            }

            sdiff = MC(ref s, tile.S.Mask, tile.S.UseMirroring, tile.S.RdpClampedMask);
            tdiff = MC(ref t, tile.T.Mask, tile.T.UseMirroring, tile.T.RdpClampedMask, 0xFF);
        }
    }
}