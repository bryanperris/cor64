using System.Text;
using System;
using System.Linq;

namespace cor64.Rdp.Commands
{
    public class SetOtherModes : RdpCommand
    {
        public SetOtherModes(RdpCommandType type, byte[] data) : base(type, data)
        {
        }

        /// <summary>
        /// Conditional color write on alpha compare
        /// </summary>
        /// <returns></returns>
        public bool EnableAlphaCompare => ReadFlag(0);

        /// <summary>
        /// Use random noise in alpha compare, otherwise use blend alpha in alpha compare
        /// </summary>
        /// <returns></returns>
        public bool EnableDitherAlpha => ReadFlag(1);


        /// <summary>
        /// Choose between Primitive Z and pixel Z
        /// </summary>
        /// <returns></returns>
        public bool UsePixelZ => ReadFlag(2);

        /// <summary>
        /// If not force blend, allow blend enable - use cvg bits
        /// </summary>
        /// <returns></returns>
        public bool EnableAntiAliasing => ReadFlag(3);

        /// <summary>
        /// Conditional color write enable on depth comparison
        /// </summary>
        /// <returns></returns>
        public bool EnableZCompare => ReadFlag(4);

        /// <summary>
        /// Enable writing of Z if color write enabled
        /// </summary>
        /// <returns></returns>
        public bool EnableZUpdate => ReadFlag(5);

        /// <summary>
        /// Enable color/cvg read/modify/write memory access
        /// </summary>
        /// <returns></returns>
        public bool EnableImageRead => ReadFlag(6);

        /// <summary>
        /// Only update color on coverage overflow (transparent sufraces)
        /// </summary>
        /// <returns></returns>
        public bool UpdateColorOnlyOnCoverage => ReadFlag(7);

        public enum ConverageDestType : int
        {
            ///<summary>Normal</summary>
            Clamp = 0,
            ///<summary>Was assume full converage</summary>
            Wrap,
            ///<summary>Foce to full coverage</summary>
            Zap,
            ///<summary>Don't overwrite memory converage</summary>
            Save
        }

        public ConverageDestType CoverageDestination => (ConverageDestType)ReadField(8, 0b11);

        public enum ZModeType : int
        {
            Opaque = 0,
            Interpenetrating,
            Transparent,
            Decal
        }

        public ZModeType ZMode => (ZModeType)ReadField(10, 0b11);

        public bool UseConverageTimesAlpha => ReadFlag(12);

        /// <summary>
        /// Use cvg (or cvg*alpha) for pixel alpha
        /// </summary>
        /// <returns></returns>
        public bool UseConverageForAlpha => ReadFlag(13);

        public bool EnableForceBlend => ReadFlag(14);

        /// <summary>
        /// Blend modeword, multiply 2b input select, cycle 1
        /// </summary>
        /// <returns></returns>
        public int BlendMode2B1 => ReadField(16, 0b11);

        /// <summary>
        /// Blend modeword, multiply 2b input select, cycle 0
        /// </summary>
        /// <returns></returns>
        public int BlendMode2B0 => ReadField(18, 0b11);

        /// <summary>
        /// Blend modeword, multiply 2a input select, cycle 1
        /// </summary>
        /// <returns></returns>
        public int BlendMode2A1 => ReadField(20, 0b11);

        /// <summary>
        /// Blend modeword, multiply 2a input select, cycle 0
        /// </summary>
        /// <returns></returns>
        public int BlendMode2A0 => ReadField(22, 0b11);

        /// <summary>
        /// Blend modeword, multiply 1b input select, cycle 1
        /// </summary>
        /// <returns></returns>
        public int BlendMode1B1 => ReadField(24, 0b11);

        /// <summary>
        /// Blend modeword, multiply 1b input select, cycle 0
        /// </summary>
        /// <returns></returns>
        public int BlendMode1B0 => ReadField(26, 0b11);

        /// <summary>
        /// Blend modeword, multiply 1a input select, cycle 1
        /// </summary>
        /// <returns></returns>
        public int BlendMode1A1 => ReadField(28, 0b11);

        /// <summary>
        /// Blend modeword, multiply 1a input select, cycle 0
        /// </summary>
        /// <returns></returns>
        public int BlendMode1A0 => ReadField(30, 0b11);


        public enum AlphaDitherSelectionType : int
        {
            Pattern = 0,
            NegatePattern,
            Noise,
            NoDither
        }

        public AlphaDitherSelectionType AlphaDitherSelect => (AlphaDitherSelectionType)ReadField(36, 0b11);

        public enum RgbDitherSelectionType : int
        {
            MagicSquareMatrix = 0,
            StandardBayerMatrix,
            Noise,
            NoDither
        }

        public RgbDitherSelectionType RgbDitherSelection => (RgbDitherSelectionType)ReadField(38, 0b11);

        public bool EnableChromaKeyring => ReadFlag(40);

        /// <summary>
        /// Color convert texel that was the output of the texture filter on cycle 0, used to qualify bi_lerp_1
        /// </summary>
        /// <returns></returns>
        public bool UseTexelColorConversion => ReadFlag(41);

        /// <summary>
        /// 1 = bi_lerp, 0 = color convert operation in texture filter. Used in cycle 1
        /// </summary>
        /// <returns></returns>
        public bool BiLerp1 => ReadFlag(42);

        /// <summary>
        /// 1 = bi_lerp, 0 = color convert operation in texture filter. Used in cycle 0
        /// </summary>
        /// <returns></returns>
        public bool BiLerp0 => ReadFlag(43);

        /// <summary>
        /// Indicates texture filter should do a 2x2 half texel interpolation, primarily used for MPEG motion compensation processing.
        /// </summary>
        /// <returns></returns>
        public bool UseMidTexel => ReadFlag(44);

        public enum TextureSampleType : int
        {
            Sample1x1 = 0,
            Sample2x2
        }

        public TextureSampleType SampleType => (TextureSampleType)ReadField(45, 0b1);

        public enum TextureLookupTableType : int
        {
            RGBA_16 = 0,
            IA
        }

        public TextureLookupTableType TlutType => (TextureLookupTableType)ReadField(46, 0b1);

        /// <summary>
        /// Enable lookup of texel values from TLUT. Meaningful if texture type is index, tile is in low Tmem, TLUT is in high Tmem, and color image is RGB.
        /// </summary>
        /// <returns></returns>
        public bool EnableTlut => ReadFlag(47);

        /// <summary>
        /// Enable texture Level of Detail (LOD)
        /// </summary>
        /// <returns></returns>
        public bool EnableTextureLOD => ReadFlag(48);

        public bool EnableSharpendTexture => ReadFlag(49);

        public bool EnableDetailTexture => ReadFlag(50);

        public bool EnableTexturePerspectiveCorrection => ReadFlag(51);

        public enum PipelineModeType : int
        {
            OneCycle = 0,
            TwoCycle,
            Copy,
            Fill
        }

        public PipelineModeType PipelineCycleMode => (PipelineModeType)ReadField(52, 0b11);

        /// <summary>
        ///  Force primitive to be written to frame buffer before read of following primitive.
        /// </summary>
        /// <returns></returns>
        public bool AtomicPrimitives => ReadFlag(54);

        protected override String Params() {
            AsmParams_Start();

            if (EnableAlphaCompare)
                AsmParams_AppendFlag("ALPHA_COMPARE_EN");

            if (EnableDitherAlpha)
                AsmParams_AppendFlag("DITHER_ALPHA_EN");

            if (UsePixelZ)
                AsmParams_AppendFlag("Z_SOURCE_SEL");

            if (EnableAntiAliasing)
                AsmParams_AppendFlag("ANTIALIAS_EN");

            if (EnableZCompare)
                AsmParams_AppendFlag("Z_COMPARE_EN");

            if (EnableZUpdate)
                AsmParams_AppendFlag("Z_UPDATE_EN");

            if (EnableImageRead)
                AsmParams_AppendFlag("IMAGE_READ_EN");

            if (UpdateColorOnlyOnCoverage)
                AsmParams_AppendFlag("COLOR_ON_CVG");

            switch (CoverageDestination) {
                case ConverageDestType.Wrap: AsmParams_AppendFlag("CVG_DEST_WRAP"); break;
                case ConverageDestType.Zap: AsmParams_AppendFlag("CVG_DEST_ZAP"); break;
                case ConverageDestType.Save: AsmParams_AppendFlag("CVG_DEST_SAVE"); break;
            }

            switch(ZMode) {
                case ZModeType.Interpenetrating: AsmParams_AppendFlag("Z_MODE_INTERPENETRATING"); break;
                case ZModeType.Transparent: AsmParams_AppendFlag("Z_MODE_TRANSPARENT"); break;
                case ZModeType.Decal: AsmParams_AppendFlag("Z_MODE_DECAL"); break;
            }

            if (UseConverageTimesAlpha)
                AsmParams_AppendFlag("CVG_TIMES_ALPHA");

            if (UseConverageForAlpha)
                AsmParams_AppendFlag("ALPHA_CVG_SELECT");

            if (EnableForceBlend)
                AsmParams_AppendFlag("FORCE_BLEND");

            switch (BlendMode2B1) {
                case 1: AsmParams_AppendFlag("B_M2B_1_1"); break;
                case 2: AsmParams_AppendFlag("B_M2B_1_2"); break;
                case 3: AsmParams_AppendFlag("B_M2B_1_3"); break;
            }
            
            switch (BlendMode2B0) {
                case 1: AsmParams_AppendFlag("B_M2B_0_1"); break;
                case 2: AsmParams_AppendFlag("B_M2B_0_2"); break;
                case 3: AsmParams_AppendFlag("B_M2B_0_3"); break;
            }

            switch (BlendMode2A1) {
                case 1: AsmParams_AppendFlag("B_M2A_1_1"); break;
                case 2: AsmParams_AppendFlag("B_M2A_1_2"); break;
                case 3: AsmParams_AppendFlag("B_M2A_1_3"); break;
            }

            switch (BlendMode2A0) {
                case 1: AsmParams_AppendFlag("B_M2A_0_1"); break;
                case 2: AsmParams_AppendFlag("B_M2A_0_2"); break;
                case 3: AsmParams_AppendFlag("B_M2A_0_3"); break;
            }

            switch (BlendMode1B1) {
                case 1: AsmParams_AppendFlag("B_M1B_1_1"); break;
                case 2: AsmParams_AppendFlag("B_M1B_1_2"); break;
                case 3: AsmParams_AppendFlag("B_M1B_1_3"); break;
            }

            switch (BlendMode1B0) {
                case 1: AsmParams_AppendFlag("B_M1B_0_1"); break;
                case 2: AsmParams_AppendFlag("B_M1B_0_2"); break;
                case 3: AsmParams_AppendFlag("B_M1B_0_3"); break;
            }

            switch (BlendMode1A1) {
                case 1: AsmParams_AppendFlag("B_M1A_1_1"); break;
                case 2: AsmParams_AppendFlag("B_M1A_1_2"); break;
                case 3: AsmParams_AppendFlag("B_M1A_1_3"); break;
            }

            switch (BlendMode1A0) {
                case 1: AsmParams_AppendFlag("B_M1A_0_1"); break;
                case 2: AsmParams_AppendFlag("B_M1A_0_2"); break;
                case 3: AsmParams_AppendFlag("B_M1A_0_3"); break;
            }

            switch(AlphaDitherSelect) {
                case AlphaDitherSelectionType.NegatePattern: AsmParams_AppendFlag("ALPHA_DITHER_SEL_PATTERNB"); break;
                case AlphaDitherSelectionType.Noise: AsmParams_AppendFlag("ALPHA_DITHER_SEL_NOISE"); break;
                case AlphaDitherSelectionType.NoDither: AsmParams_AppendFlag("ALPHA_DITHER_SEL_NO_DITHER"); break;
            }

            switch (RgbDitherSelection) {
                case RgbDitherSelectionType.StandardBayerMatrix: AsmParams_AppendFlag("RGB_DITHER_SEL_STANDARD_BAYER_MATRIX"); break;
                case RgbDitherSelectionType.Noise: AsmParams_AppendFlag("RGB_DITHER_SEL_NOISE"); break;
                case RgbDitherSelectionType.NoDither: AsmParams_AppendFlag("RGB_DITHER_SEL_NO_DITHER"); break;
            }

            if (EnableChromaKeyring)
                AsmParams_AppendFlag("KEY_EN");

            if (UseTexelColorConversion)
                AsmParams_AppendFlag("CONVERT_ONE");

            if (BiLerp1)
                AsmParams_AppendFlag("BI_LERP_1");

            if (BiLerp0)
                AsmParams_AppendFlag("BI_LERP_0");

            if (UseMidTexel)
                AsmParams_AppendFlag("MID_TEXEL");

            if (SampleType == TextureSampleType.Sample2x2)
                AsmParams_AppendFlag("SAMPLE_TYPE");

            if (TlutType == TextureLookupTableType.IA)
                AsmParams_AppendFlag("TLUT_TYPE");

            if (EnableTlut)
                AsmParams_AppendFlag("EN_TLUT");

            if (EnableTextureLOD)
                AsmParams_AppendFlag("TEX_LOD_EN");

            if (EnableSharpendTexture)
                AsmParams_AppendFlag("SHARPEN_TEX_EN");

            if (EnableDetailTexture)
                AsmParams_AppendFlag("DETAIL_TEX_EN");

            if (EnableTexturePerspectiveCorrection)
                AsmParams_AppendFlag("PERSP_TEX_EN");

            switch (PipelineCycleMode) {
                case PipelineModeType.TwoCycle: AsmParams_AppendFlag("CYCLE_TYPE_2_CYCLE"); break;
                case PipelineModeType.Copy: AsmParams_AppendFlag("CYCLE_TYPE_COPY"); break;
                case PipelineModeType.Fill: AsmParams_AppendFlag("CYCLE_TYPE_FILL"); break;
            }

            if (AtomicPrimitives)
                AsmParams_AppendFlag("ATOMIC_PRIM");

            return AsmParams_End();
        }
    }
}