using System;
using System.Drawing;
using System.Net.NetworkInformation;
using cor64.Rdp.Commands;

namespace cor64.Rdp.LLE
{
    public class Combiner
    {
        /// <summary>
        /// Special 9-Bit Clamp Table
        /// </summary>
        private static readonly int[] s_ClampTable = new int[512];

        /// <summary>
        /// Special 9-Bit Ext Table
        /// </summary>
        private static readonly int[] s_ExtTable = new int[512];

        private State GS => m_Rdp.GraphicsState;

        private readonly MadCatRdp m_Rdp;

        public readonly CombinerState S0 = new CombinerState();
        public readonly CombinerState S1 = new CombinerState();
        private readonly CombinerState[] States = new CombinerState[2];

        static Combiner()
        {
            for (int i = 0; i < 0x200; i++)
            {
                s_ClampTable[i] = ((i >> 7) & 3) switch
                {
                    0 => i & 0xFF,
                    1 => i & 0xFF,
                    2 => 0xFF,
                    _ => 0
                };

                if ((i & 0x180) == 0x180) {
                    s_ExtTable[i] = i | ~0x1FF;
                }
                else {
                    s_ExtTable[i] = i & 0x1FF;
                }
            }
        }

        public static int LookupClamp(int index) {
            return s_ClampTable[index];
        }

        public static int LookupExt(int index) {
            return s_ExtTable[index];
        }

        public Combiner(MadCatRdp rdp)
        {
            m_Rdp = rdp;
            States[0] = S0;
            States[1] = S1;
        }

        private class ValueRef : ColorRef {
            private readonly Func<int> m_Read;
            private readonly Action<int> m_Write;

            public ValueRef(Func<int> r, Action<int> w) {
                m_Read = r;
                m_Write = w;
            }

            public override int R {
                get => m_Read();
                set => m_Write(value);
            }

            public override int G {
                get => m_Read();
                set => m_Write(value);
            }

            public override int B {
                get => m_Read();
                set => m_Write(value);
            }

            public override int A {
                get => m_Read();
                set => m_Write(value);
            }
        }

        private class RgbFromAlphaRef : ColorRef {
            private readonly ColorRef m_Color;

            public RgbFromAlphaRef(ColorRef color) {
                m_Color = color;
            }

            public override int R {
                get =>  m_Color.A;
                set =>  m_Color.A = value;
            }

            public override int G {
                get =>  m_Color.A;
                set =>  m_Color.A = value;
            }

            public override int B {
                get =>  m_Color.A;
                set =>  m_Color.A = value;
            }

            public override int A {
                get =>  m_Color.A;
                set =>  m_Color.A = value;
            }

            public override ColorRef GetSource() {
                return m_Color;
            }
        }

        private ColorRef SelectColorSource(int code) {
            return code switch
            {
                0 => GS.CombinedColor,
                1 => GS.Texel0Color,
                2 => GS.Texel1Color,
                3 => GS.PrimColor,
                4 => GS.ShadeColor,
                5 => GS.EnvironmentColor,
                _ => CombinerInputRef.ZERO,
            };
        }

        public void SetRGBInput_SubtractA(CombinerInputRef input, int code)
        {
            switch (code & 0xF) {
                case 0:
                case 1:
                case 2:
                case 3:
                case 4:
                case 5: input.SelectRgb(SelectColorSource(code)); break;
                case 6: input.SetRgbOne(); break;
                case 7: input.SelectRgb(new ValueRef(() => GS.Noise, (v) => GS.Noise = v), nameof(GS.Noise)); break;
                default: input.SetRgbZero(); break;
            }
        }

        public void SetRGBInput_SubtractB(CombinerInputRef input, int code)
        {
            switch (code & 0xF) {
                case 0:
                case 1:
                case 2:
                case 3:
                case 4:
                case 5: input.SelectRgb(SelectColorSource(code)); break;
                case 6: input.SelectRgb(GS.KeyCenter); break;
                case 7: input.SelectRgb(new ValueRef(() => GS.YuvConversion_K4, (v) => GS.YuvConversion_K4 = v), nameof(GS.YuvConversion_K4)); break;
                default: input.SetRgbZero(); break;
            }
        }

        public void SetRGBInput_Multiply(CombinerInputRef input, int code)
        {
            switch (code & 0x1F) {
                case 0:
                case 1:
                case 2:
                case 3:
                case 4:
                case 5: input.SelectRgb(SelectColorSource(code)); break;
                case 6: input.SelectRgb(GS.KeyCenter); break;
                case 7:
                case 8:
                case 9:
                case 10:
                case 11:
                case 12: input.SelectRgb(new RgbFromAlphaRef(SelectColorSource(code - 7))); break;
                case 13: input.SelectRgb(new ValueRef(() => GS.LodFraction, (v) => GS.LodFraction = v), nameof(GS.LodFraction)); break;
                case 14: input.SelectRgb(new ValueRef(() => GS.PrimitiveLODFactor, (v) => GS.PrimitiveLODFactor = v), nameof(GS.PrimitiveLODFactor)); break;
                case 15: input.SelectRgb(new ValueRef(() => GS.YuvConversion_K5, (v) => GS.YuvConversion_K5 = v), nameof(GS.YuvConversion_K5)); break;
                default: input.SetRgbZero(); break;
            }
        }

        public void SetRGBInput_Add(CombinerInputRef input, int code)
        {
            switch (code & 0x7) {
                case 0:
                case 1:
                case 2:
                case 3:
                case 4:
                case 5: input.SelectRgb(SelectColorSource(code)); break;
                case 6: input.SetRgbOne(); break;
                default: input.SetRgbZero(); break;
            }
        }

        public void SetAlphaInput_Subtract(CombinerInputRef input, int code)
        {
            switch (code & 7) {
                case 0:
                case 1:
                case 2:
                case 3:
                case 4:
                case 5: input.SelectAlpha(SelectColorSource(code)); break;
                case 6: input.SetAlphaOne(); break;
                case 7: input.SetAlphaZero(); break;
            }
        }

        public void SetAlphaInput_Multiply(CombinerInputRef input, int code)
        {
            switch (code & 7) {
                case 0: input.SelectAlpha(new ValueRef(() => GS.LodFraction, (v) => GS.LodFraction = v), nameof(GS.LodFraction)); break;
                case 1: input.SelectAlpha(GS.Texel0Color); break;
                case 2: input.SelectAlpha(GS.Texel1Color); break;
                case 3: input.SelectAlpha(GS.PrimColor); break;
                case 4: input.SelectAlpha(GS.ShadeColor); break;
                case 5: input.SelectAlpha(GS.EnvironmentColor); break;
                case 6: input.SelectAlpha(new ValueRef(() => GS.PrimitiveLODFactor, (v) => GS.PrimitiveLODFactor = v), nameof(GS.PrimitiveLODFactor)); break;
                case 7: input.SetAlphaZero(); break;
            }
        }

        public int ColorEquation(int a, int b, int c, int d)
        {
            a = s_ExtTable[a];
            b = s_ExtTable[b];
            c = Utils.SignF(c, 9);
            d = s_ExtTable[d];
            a = ((a - b) * c) + (d << 8) + 0x80;

            return a & 0x1FFFF;
        }

        public int AlphaEquation(int a, int b, int c, int d)
        {
            a = s_ExtTable[a];
            b = s_ExtTable[b];
            c = Utils.SignF(c, 9);
            d = s_ExtTable[d];
            a = (((a - b) * c) + (d << 8) + 0x80) >> 8;
            return a & 0x1FF;
        }

        public int ChromaKeyMin(Color color)
        {
            int keyRed, keyGreen, keyBlue, keyAlpha;

            static int ComputeKey(int color, int keyColorWidth)
            {
                int keyChannel = Utils.Sign(color, 17);

                if (keyChannel > 0)
                {
                    if ((keyChannel & 0xF) == 8)
                    {
                        keyChannel = -keyChannel + 0x10;
                    }
                    else
                    {
                        keyChannel = -keyChannel;
                    }
                }

                return (keyColorWidth << 4) + keyChannel;
            }

            keyRed = ComputeKey(color.R, GS.KeyWidth.R);
            keyGreen = ComputeKey(color.G, GS.KeyWidth.G);
            keyBlue = ComputeKey(color.B, GS.KeyWidth.B);

            keyAlpha = (keyRed < keyGreen) ? keyRed : keyGreen;
            keyAlpha = (keyBlue < keyAlpha) ? keyBlue : keyAlpha;
            keyAlpha = keyAlpha.Clamp(0, 0xFF);

            return keyAlpha;
        }

        private void ComputeCombinedColor(int index)
        {
            if (!States[index].Multiply.IsColorRefEqual(CombinerInputRef.ZERO))
            {
                GS.CombinedColor.R = ColorEquation(
                    States[index].SubtractA.R,
                    States[index].SubtractB.R,
                    States[index].Multiply.R,
                    States[index].Add.R
                );

                GS.CombinedColor.G = ColorEquation(
                    States[index].SubtractA.G,
                    States[index].SubtractB.G,
                    States[index].Multiply.G,
                    States[index].Add.G
                );

                GS.CombinedColor.B = ColorEquation(
                    States[index].SubtractA.B,
                    States[index].SubtractB.B,
                    States[index].Multiply.B,
                    States[index].Add.B
                );
            }
            else
            {
                GS.CombinedColor.R = ((s_ExtTable[States[index].Add.R] << 8) + 0x80) & 0x1FFFF;
                GS.CombinedColor.G = ((s_ExtTable[States[index].Add.G] << 8) + 0x80) & 0x1FFFF;
                GS.CombinedColor.B = ((s_ExtTable[States[index].Add.B] << 8) + 0x80) & 0x1FFFF;
            }
        }

        private void ComputeCombinedAlpha(int index)
        {
            if (!States[index].Multiply.IsAlphaRefEqual(CombinerInputRef.ZERO))
            {
                GS.CombinedColor.A = AlphaEquation(
                    States[index].SubtractA.A,
                    States[index].SubtractB.A,
                    States[index].Multiply.A,
                    States[index].Add.A
                );
            }
            else
            {
                GS.CombinedColor.A = s_ExtTable[States[index].Add.A] & 0x1FF;
            }
        }

        public void Pass_1Cycle(int adseed, ref uint currentPixelCvg, bool is2Cycle = false) {
            int keyAlpha = 0;
            int alphaTimeCvg = 0;
            Color chromaBypass = new Color();

            if (is2Cycle) {
                GS.Texel0Color.SetColor(GS.Texel1Color);
                GS.Texel1Color.SetColor(GS.NextTexelColor);
            }

            if (GS.OtherModes.EnableKey)
            {
                chromaBypass = S1.SubtractA.GetColor();
            }

            ComputeCombinedColor(1);
            ComputeCombinedAlpha(1);

            if (!is2Cycle) {
                GS.PixelColor.A = s_ClampTable[GS.CombinedColor.A];

                if (GS.PixelColor.A == 0xFF)
                    GS.PixelColor.A = 0x100;
            }

            if (!GS.OtherModes.EnableKey)
            {
                GS.CombinedColor.R >>= 8;
                GS.CombinedColor.G >>= 8;
                GS.CombinedColor.B >>= 8;
                GS.PixelColor.R = s_ClampTable[GS.CombinedColor.R];
                GS.PixelColor.G = s_ClampTable[GS.CombinedColor.G];
                GS.PixelColor.B = s_ClampTable[GS.CombinedColor.B];
            }
            else
            {
                keyAlpha = ChromaKeyMin(GS.CombinedColor.GetColor());

                GS.PixelColor.R = s_ClampTable[chromaBypass.R];
                GS.PixelColor.G = s_ClampTable[chromaBypass.G];
                GS.PixelColor.B = s_ClampTable[chromaBypass.B];

                GS.CombinedColor.R >>= 8;
                GS.CombinedColor.G >>= 8;
                GS.CombinedColor.B >>= 8;
            }

            if (is2Cycle) {
                GS.PixelColor.A = s_ClampTable[GS.CombinedColor.A];

                if (GS.PixelColor.A == 0xFF)
                    GS.PixelColor.A = 0x100;
            }

            if (GS.OtherModes.CoverageTimesAlpha)
            {
                alphaTimeCvg = ((GS.PixelColor.A * (int)currentPixelCvg) + 4) >> 3;
                currentPixelCvg = (uint)(alphaTimeCvg >> 5) & 0xF;
            }

            if (!GS.OtherModes.AlphaCoverageSelect)
            {
                if (!GS.OtherModes.EnableKey)
                {
                    GS.PixelColor.A += adseed;

                    if (GS.PixelColor.A.IsTrue(0x100))
                    {
                        GS.PixelColor.A = 0xFF;
                    }
                }
                else
                {
                    GS.PixelColor.A = keyAlpha;
                }
            }
            else
            {
                if (GS.OtherModes.CoverageTimesAlpha)
                {
                    GS.PixelColor.A = alphaTimeCvg;
                }
                else
                {
                    GS.PixelColor.A = (int)currentPixelCvg << 5;

                    if (GS.PixelColor.A > 0xFF)
                    {
                        GS.PixelColor.A = 0xFF;
                    }
                }
            }

            m_Rdp.RdpBlender.ShadeAlpha = GS.ShadeColor.A + adseed;

            if (m_Rdp.RdpBlender.ShadeAlpha.IsTrue(0x100))
            {
                m_Rdp.RdpBlender.ShadeAlpha = 0xFF;
            }
        }

        public void Pass_2Cycle_Cycle0(int alphaDitherSeed, int coverage, ref uint combinedAlpha)
        {
            ComputeCombinedColor(0);
            ComputeCombinedAlpha(0);

            if (GS.OtherModes.EnableAlphaCompare)
            {
               int preCombinedAlpha = s_ClampTable[GS.CombinedColor.A];

                if (preCombinedAlpha == 0xFF) preCombinedAlpha = 0x100;

                if (!GS.OtherModes.AlphaCoverageSelect)
                {
                    preCombinedAlpha += alphaDitherSeed;

                    if (preCombinedAlpha.IsTrue(0x100))
                    {
                        preCombinedAlpha = 0xFF;
                    }
                }
                else
                {
                    if (GS.OtherModes.CoverageTimesAlpha)
                    {
                        preCombinedAlpha = ((preCombinedAlpha * coverage) + 4) >> 3;
                    }
                    else {
                        preCombinedAlpha = coverage << 5;
                    }

                    if (preCombinedAlpha > 0xFF)
                        preCombinedAlpha = 0xFF;
                }

                combinedAlpha = (uint)preCombinedAlpha;
            }

            GS.CombinedColor.R >>= 8;
            GS.CombinedColor.G >>= 8;
            GS.CombinedColor.B >>= 8;

            m_Rdp.RdpBlender.ShadeAlpha = GS.ShadeColor.A + alphaDitherSeed;

            if (m_Rdp.RdpBlender.ShadeAlpha.IsTrue(0x100))
            {
                m_Rdp.RdpBlender.ShadeAlpha = 0xFF;
            }
        }

        public void Pass_2Cycle_Cycle0(int adseed, ref uint currentPixelCvg, bool check = false)
        {
            int keyAlpha = 0;
            int alphaTimeCvg = 0;
            Color chromaBypass = new Color();

            GS.Texel0Color.SetColor(GS.Texel1Color);
            GS.Texel1Color.SetColor(GS.NextTexelColor);

            if (GS.OtherModes.EnableKey)
            {
                chromaBypass = S1.SubtractA.GetColor();
            }

            ComputeCombinedColor(1);
            ComputeCombinedAlpha(1);

            GS.PixelColor.A = s_ClampTable[GS.CombinedColor.A];

            if (check && GS.PixelColor.A == 0xFF)
                GS.PixelColor.A = 0x100;

            if (!GS.OtherModes.EnableKey)
            {
                GS.CombinedColor.R >>= 8;
                GS.CombinedColor.G >>= 8;
                GS.CombinedColor.B >>= 8;
                GS.PixelColor.R = s_ClampTable[GS.CombinedColor.R];
                GS.PixelColor.G = s_ClampTable[GS.CombinedColor.G];
                GS.PixelColor.B = s_ClampTable[GS.CombinedColor.B];
            }
            else
            {
                keyAlpha = ChromaKeyMin(GS.CombinedColor.GetColor());

                GS.PixelColor.R = s_ClampTable[chromaBypass.R];
                GS.PixelColor.G = s_ClampTable[chromaBypass.G];
                GS.PixelColor.B = s_ClampTable[chromaBypass.B];

                GS.CombinedColor.R >>= 8;
                GS.CombinedColor.G >>= 8;
                GS.CombinedColor.B >>= 8;
            }

            if (GS.OtherModes.CoverageTimesAlpha)
            {
                alphaTimeCvg = ((GS.PixelColor.A * (int)currentPixelCvg) + 4) >> 3;
                currentPixelCvg = (uint)(alphaTimeCvg >> 5) & 0xF;
            }

            if (!GS.OtherModes.AlphaCoverageSelect)
            {
                if (!GS.OtherModes.EnableKey)
                {
                    GS.PixelColor.A += adseed;

                    if (GS.PixelColor.A.IsTrue(0x100))
                    {
                        GS.PixelColor.A = 0xFF;
                    }
                }
                else
                {
                    GS.PixelColor.A = keyAlpha;
                }
            }
            else
            {
                if (GS.OtherModes.CoverageTimesAlpha)
                {
                    GS.PixelColor.A = alphaTimeCvg;
                }
                else
                {
                    GS.PixelColor.A = (int)currentPixelCvg << 5;

                    if (GS.PixelColor.A > 0xFF)
                    {
                        GS.PixelColor.A = 0xFF;
                    }
                }
            }

            m_Rdp.RdpBlender.ShadeAlpha = GS.ShadeColor.A + adseed;

            if (m_Rdp.RdpBlender.ShadeAlpha.IsTrue(0x100))
            {
                m_Rdp.RdpBlender.ShadeAlpha = 0xFF;
            }
        }

        public void Pass_2Cycle_Cycle1(int adseed, ref uint coverage) {
            Pass_1Cycle(adseed, ref coverage, true);
        }

        private void UpdateCombineMode(CombinerState combinerState)
        {
            SetRGBInput_SubtractA(
                combinerState.SubtractA,
                combinerState.InputSubtractA.RGBSelection
            );

            SetAlphaInput_Subtract(
                combinerState.SubtractA,
                combinerState.InputSubtractA.AlphaSelection
            );

            SetRGBInput_SubtractB(
                combinerState.SubtractB,
                combinerState.InputSubtractB.RGBSelection
            );

            SetAlphaInput_Subtract(
                 combinerState.SubtractB,
                 combinerState.InputSubtractB.AlphaSelection
            );

            SetRGBInput_Multiply(
                combinerState.Multiply,
                combinerState.InputMultiply.RGBSelection
            );

            SetAlphaInput_Multiply(
                 combinerState.Multiply,
                 combinerState.InputMultiply.AlphaSelection
            );

            SetRGBInput_Add(
                combinerState.Add,
                combinerState.InputAdd.RGBSelection
            );

            SetAlphaInput_Subtract(
                 combinerState.Add,
                 combinerState.InputAdd.AlphaSelection
            );
        }

        public void UpdateCombineMode()
        {
            UpdateCombineMode(S0);
            UpdateCombineMode(S1);
        }
    }
}