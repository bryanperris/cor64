using System;
using NLog;

namespace cor64.Rdp.LLE
{
    public class Blender
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly MadCatRdp m_Rdp;

        private State GS => m_Rdp.GraphicsState;

        private const int BLENDER_ONE = 0xFF;
        private static readonly byte[] s_BlendDivisionHwAccurateTable;

        public class BlendInput
        {
            private static readonly ColorRef s_Zero = new ColorRef(Color.Zero);
            private static readonly ColorRef s_One = new ColorRef(new Color(BLENDER_ONE, BLENDER_ONE, BLENDER_ONE, BLENDER_ONE));

            private ColorRef m_RGBRef;
            private ColorRef m_AlphaRef;


            public BlendInput() {
                m_RGBRef = s_Zero;
                m_AlphaRef = s_Zero;
            }

            public void SelectRgbOne() {
                m_RGBRef = s_One;
            }

            public void SelectAlphaOne() {
                m_AlphaRef = s_One;
            }

            public void SelectRgbZero() {
                m_RGBRef = s_Zero;
            }

            public void SelectAlphaZero() {
                m_AlphaRef = s_Zero;
            }

            public void SelectRgb(ColorRef colorRef) {
                m_RGBRef = colorRef;
            }

            public void SelectAlpha(ColorRef colorRef) {
                m_AlphaRef = colorRef;
            }

            public bool IsColorRefEqual(ColorRef cr) {
                return ReferenceEquals(m_RGBRef, cr);
            }

            public bool IsAlphaRefEqual(ColorRef cr) {
                return ReferenceEquals(m_AlphaRef, cr);
            }

            public int AR => m_RGBRef.R;
            public int AG => m_RGBRef.G;
            public int AB => m_RGBRef.B;
            public int BA => m_AlphaRef.A;
        }

        public class Inputs
        {
            public BlendInput Mode1 = new BlendInput();
            public BlendInput Mode2 = new BlendInput();

            internal Inputs()
            {

            }
        }

        public readonly Inputs S0 = new Inputs();
        public readonly Inputs S1 = new Inputs();

        private readonly Inputs[] States;

        public int ShiftA { get; set; }

        public int ShiftB { get; set; }

        public int PastShiftA { get; set; }

        public int PastShiftB { get; set; }

        public bool RealShifterNeeded { get; set; }

        public bool InterPixelShifterNeeded { get; set; }

        private readonly ColorRef m_ShadeAlpha = new ColorRef();

        public int ShadeAlpha {
            get => m_ShadeAlpha.A;
            set => m_ShadeAlpha.A = value;
        }
        

        public Color BlendedColor
        {
            get => m_BlendedColor.GetColor();
            set => m_BlendedColor.SetColor(value);
        }

        private readonly ColorRef m_BlendedColor = new ColorRef();

        public Blender(MadCatRdp rdp)
        {
            m_Rdp = rdp;

            States = new Inputs[] {
                S0,
                S1
            };
        }

        static Blender()
        {
            s_BlendDivisionHwAccurateTable = new byte[0x8000];

            int[] ps = new int[9];

            for (int i = 0; i < s_BlendDivisionHwAccurateTable.Length; i++)
            {
                int res = 0;
                int d = (i >> 11) & 0xF;
                int n = i & 0x7FF;
                int invd = (~d) & 0xF;
                int temp = invd + (n >> 8) + 1;

                ps[0] = temp & 7;

                for (int k = 0; k < 8; k++)
                {
                    int nbit = (n >> (7 - k)) & 1;

                    if (res.IsTrue(0x100 >> k))
                    {
                        temp = invd + (ps[k] << 1) + nbit + 1;
                    }
                    else
                    {
                        temp = d + (ps[k] << 1) + nbit;
                    }

                    ps[k + 1] = temp & 7;

                    if (temp.IsTrue(0x10))
                    {
                        res |= 1 << (7 - k);
                    }
                }

                s_BlendDivisionHwAccurateTable[i] = (byte)res;
            }
        }

        public struct BlendInputSelections
        {
            public readonly int A1;
            public readonly int A2;
            public readonly int B1;
            public readonly int B2;

            public BlendInputSelections(
                int a1,
                int a2,
                int b1,
                int b2
            )
            {
                A1 = a1;
                A2 = a2;
                B1 = b1;
                B2 = b2;
            }
        }

        public void UpdateInputs(BlendInputSelections i0, BlendInputSelections i1)
        {
            SetInput(0, 0, S0.Mode1, i0.A1, i0.B1 );
            SetInput(0, 1, S0.Mode2, i0.A2, i0.B2 );
            SetInput(1, 0, S1.Mode1, i1.A1, i1.B1 );
            SetInput(1, 1, S1.Mode2, i1.A2, i1.B2 );
        }

        public void SetInput(int cycle, int which, BlendInput input, int a, int b)
        {
            //Log.Debug("Blender Input Selections: {0} {1} {2} {3}", cycle, which, a, b);
            
            switch (a & 0x3)
            {
                case 0:
                    {
                        if (cycle == 0)
                        {
                            input.SelectRgb(GS.PixelColor);
                            // Log.Debug("Blender RGB Input = Pixel Color");
                        }
                        else
                        {
                            input.SelectRgb(m_BlendedColor);
                            // Log.Debug("Blender RGB Input = Blended Color");
                        }

                        break;
                    }

                case 1:
                    {
                        input.SelectRgb(GS.MemoryColor);
                        // Log.Debug("Blender RGB Input = Memory Color");
                        break;
                    }

                case 2:
                    {
                        input.SelectRgb(GS.BlendColor);
                        // Log.Debug("Blender RGB Input = Blend Color");
                        break;
                    }

                case 3:
                    {
                        input.SelectRgb(GS.FogColor);
                        // Log.Debug("Blender RGB Input = Fog Color");
                        break;
                    }

                default: throw new RdpException("Unknown color selection for blend A input");
            }

            var sel = b & 03;

            if (which == 0)
            {
                switch (sel) {
                    case 0: {
                        input.SelectAlpha(GS.PixelColor);
                        // Log.Debug("Blender Alpha Input = Pixel Color");
                        break;
                    }

                    case 1: {
                        input.SelectAlpha(GS.FogColor);
                        // Log.Debug("Blender Alpha Input = Fog Color");
                        break;
                    }

                    case 2: {
                        input.SelectAlpha(m_ShadeAlpha);
                        // Log.Debug("Blender Alpha Input = Shade Alpha");
                        break;
                    }

                    case 3: {
                        input.SelectAlphaZero();
                        // Log.Debug("Blender Alpha Input = Zero");
                        break;
                    }

                    default: throw new RdpException("Invalid alpha selection for input B (which == 0");
                }
            }
            else
            {
                switch (sel) {
                    case 0: {
                        input.SelectAlpha(GS.InvPixelColor);
                        // Log.Debug("Blender Alpha Input = Inverted Pixel Color");
                        break;
                    }

                    case 1: {
                        input.SelectAlpha(GS.MemoryColor);
                        // Log.Debug("Blender Alpha Input = Memory Color");
                        break;
                    }

                    case 2: {
                        input.SelectAlphaOne();
                        // Log.Debug("Blender Alpha Input = One");
                        break;
                    }

                    case 3: {
                        input.SelectAlphaZero();
                        // Log.Debug("Blender Alpha Input = Zero");
                        break;
                    }

                    default: throw new RdpException("Invalid alpha selection for input B (which == 0");
                }
            }
        }

        public bool AlphaCompare(int combAlpha)
        {
            if (!GS.OtherModes.EnableAlphaCompare)
            {
                return true;
            }

            int threshold = !GS.OtherModes.EnableDitherAlpha ?
                    GS.BlendColor.A :
                    Utils.IRand(ref GS.RandomSeed) & 0xFF;

            return combAlpha >= threshold;
        }

        public void Equation_Cycle0_2(ref int r, ref int g, ref int b) {
            Equation(0, ref r, ref g, ref b, true);
        }

        public void Equation_Cycle0(ref int r, ref int g, ref int b) {
            Equation(0, ref r, ref g, ref b);
        }

        public void Equation_Cycle1(ref int r, ref int g, ref int b) {
            Equation(1, ref r, ref g, ref b);
        }

        private void Equation(int cycle, ref int r, ref int g, ref int b, bool alternative = false)
        {
            var s = States[cycle];

            int blend1a = s.Mode1.BA >> 3;
            int blend2a = s.Mode2.BA >> 3;

            int shiftA = alternative ? PastShiftA : ShiftA;
            int shiftB = alternative ? PastShiftB : ShiftB;

            if (s.Mode2.IsAlphaRefEqual(GS.MemoryColor))
            {
                blend1a = (blend1a >> shiftA) & 0x3C;
                blend2a = (blend2a >> shiftB) | 3;
            }

            int mulb = blend2a + 1;

            int blr = (s.Mode1.AR * blend1a) + (s.Mode2.AR * mulb);
            int blg = (s.Mode1.AG * blend1a) + (s.Mode2.AG * mulb);
            int blb = (s.Mode1.AB * blend1a) + (s.Mode2.AB * mulb);

            if (!GS.OtherModes.EnableForceBlend && !alternative)
            {
                int sum = ((blend1a & ~3) + (blend2a & ~3) + 4) << 9;
                r = s_BlendDivisionHwAccurateTable[sum | ((blr >> 2) & 0x7FF)];
                g = s_BlendDivisionHwAccurateTable[sum | ((blg >> 2) & 0x7FF)];
                b = s_BlendDivisionHwAccurateTable[sum | ((blb >> 2) & 0x7FF)];
            }
            else
            {
                r = (blr >> 5) & 0xFF;
                g = (blg >> 5) & 0xFF;
                b = (blb >> 5) & 0xFF;
            }
        }

        /// <summary>
        /// Performs a framebuffer blend pass
        /// </summary>
        /// <param name="r"></param>
        /// <param name="g"></param>
        /// <param name="b"></param>
        /// <param name="dither"></param>
        /// <param name="enableBlend"></param>
        /// <param name="prewrap"></param>
        /// <param name="coverage"></param>
        /// <param name="coverageBit"></param>
        /// <returns>Whether to write the color value to the framebuffer</returns>
        public bool FramebufferPass(int cycle, ref int r, ref int g, ref int b, int dither, bool enableBlend, int prewrap, uint coverage = 0, bool coverageBit = false)
        {
            var s = States[cycle];

            if (cycle == 0)
            {
                if (!AlphaCompare(GS.PixelColor.A))
                {
                    return false;
                }

                if (!(GS.OtherModes.EnableAntiAliasing ? coverage.IsTrue() : coverageBit))
                {
                    return false;
                }
            }

            if (!GS.OtherModes.ColorOnCoverage || prewrap != 0)
            {
                var reject = cycle == 0 ? GS.OtherModes.Flags.UsePartialReject_1Cycle : GS.OtherModes.Flags.UsePartialReject_2Cycle;
                bool noBlend = reject && GS.PixelColor.A >= 0xFF;

                if (!enableBlend || noBlend)
                {
                    r = s.Mode1.AR;
                    g = s.Mode1.AG;
                    b = s.Mode1.AB;
                }
                else
                {
                    GS.InvPixelColor.A = ~s.Mode1.BA & 0xFF;

                    if (cycle == 0) {
                        Equation_Cycle0(ref r, ref g, ref b);
                    }
                    else {
                        Equation_Cycle1(ref r, ref g, ref b);
                    }
                }
            }
            else
            {
                r = s.Mode2.AR;
                g = s.Mode2.AG;
                b = s.Mode2.AB;
            }

            if (GS.OtherModes.RgbDitherSelection != 3)
            {
                Dither.RgbDither(GS.OtherModes.RgbDitherSelection, ref r, ref g, ref b, dither);
            }

            return true;

        }

        /// <summary>
        /// Blender Pass Cycle0 (2-Cycle pass)
        /// </summary>
        /// <param name="hasCoverage"></param>
        /// <param name="coverageBit"></param>
        /// <returns>Whether write is enabled</returns>
        public bool Pass_2Cycle_Cycle0(bool hasCoverage, bool coverageBit)
        {
            var result = GS.OtherModes.EnableAntiAliasing ? hasCoverage : coverageBit;

            if (result)
            {
                GS.InvPixelColor.A = ~S0.Mode1.BA & 0xFF;

                int r = 0;
                int g = 0;
                int b = 0;

                Equation_Cycle0_2(ref r, ref g, ref b);

                m_BlendedColor.R = r;
                m_BlendedColor.G = g;
                m_BlendedColor.B = b;
            }

            GS.MemoryColor.SetColor(GS.PreMemoryColor);

            return result;
        }
    }
}