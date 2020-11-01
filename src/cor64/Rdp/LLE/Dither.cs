namespace cor64.Rdp.LLE {
    public static class Dither {

        private static readonly byte[] s_BayerMatrix =
        {
            0, 4, 1, 5,
            4, 0, 5, 1,
            3, 7, 2, 6,
            7, 3, 6, 2
        };

        private static readonly byte[] s_MagicMatrix =
        {
            0, 6, 1, 7,
            4, 2, 5, 3,
            3, 5, 2, 4,
            7, 1, 6, 0
        };

        public static void RgbDither(int selection, ref int r, ref int g, ref int b, int dither) {
            int comp_r;
            int comp_g;
            int comp_b;

            int new_r = r > 247 ? 255 : (r & 0xF8) + 8;
            int new_g = g > 247 ? 255 : (g & 0xF8) + 8;
            int new_b = b > 247 ? 255 : (b & 0xF8) + 8;

            if (selection != 2) {
                comp_r = dither;
                comp_g = dither;
                comp_b = dither;
            }
            else {
                comp_r = dither & 7;
                comp_g = (dither >> 3) & 7;
                comp_b = (dither >> 6) & 7;
            }

            int replaceSign = (comp_r - (r & 7)) >> 31;
            int ditherDiff = new_r - r;
            r += ditherDiff & replaceSign;

            replaceSign = (comp_g - (g & 7)) >> 31;
            ditherDiff = new_g - g;
            g += ditherDiff & replaceSign;

            replaceSign = (comp_b - (b & 7)) >> 31;
            ditherDiff = new_b - b;
            b += ditherDiff & replaceSign;
        }

        public static void GetDitherNoise(State state, int x, int y, ref int colorDither, ref int alphaDither) {
            if (!state.OtherModes.Flags.GetDitherLevel.IsTrue()) {
                state.Noise = ((Utils.IRand(ref state.RandomSeed) & 7) << 6) | 0x20;
            }

            int ditherIndex =  (((y >> state.ScField) & 3) << 2) | (x & 3);

            switch (state.OtherModes.Flags.RgbaDither) {
                case 0: {
                    alphaDither = s_MagicMatrix[ditherIndex];
                    colorDither = s_MagicMatrix[ditherIndex];
                    break;
                }

                case 1: {
                    colorDither = s_MagicMatrix[ditherIndex];
                    alphaDither = ~colorDither & 7;
                    break;
                }

                case 2: {
                    colorDither = s_MagicMatrix[ditherIndex];
                    alphaDither = (state.Noise >> 6) & 7;
                    break;
                }

                case 3: {
                    colorDither = s_MagicMatrix[ditherIndex];
                    alphaDither = 0;
                    break;
                }

                case 4: {
                    colorDither = s_BayerMatrix[ditherIndex];
                    alphaDither = s_BayerMatrix[ditherIndex];
                    break;
                }

                case 5: {
                    colorDither = s_BayerMatrix[ditherIndex];
                    alphaDither = ~colorDither & 7;
                    break;
                }

                case 6: {
                    colorDither = s_BayerMatrix[ditherIndex];
                    alphaDither = (state.Noise >> 6) & 7;
                    break;
                }

                case 7: {
                    colorDither = s_BayerMatrix[ditherIndex];
                    alphaDither = 0;
                    break;
                }

                case 8: {
                    colorDither = Utils.IRand(ref state.RandomSeed);
                    alphaDither = s_MagicMatrix[ditherIndex];
                    break;
                }

                case 9: {
                    colorDither = Utils.IRand(ref state.RandomSeed);
                    alphaDither = ~s_MagicMatrix[ditherIndex] & 7;
                    break;
                }

                case 10: {
                    colorDither = Utils.IRand(ref state.RandomSeed);
                    alphaDither = (state.Noise >> 6) & 7;
                    break;
                }

                case 11: {
                    colorDither = Utils.IRand(ref state.RandomSeed);
                    alphaDither = 0;
                    break;
                }

                case 12: {
                    colorDither = 7;
                    alphaDither = s_BayerMatrix[ditherIndex];
                    break;
                }

                case 13: {
                    colorDither = 7;
                    alphaDither = ~s_BayerMatrix[ditherIndex] & 7;
                    break;
                }

                case 14: {
                    colorDither = 7;
                    alphaDither = (state.Noise >> 6) & 7;
                    break;
                }

                case 15: {
                    colorDither = 7;
                    alphaDither = 0;
                    break;
                }
            }
        }
    }
}