namespace cor64.Rdp.LLE
{
    public class OtherModesState
    {
        public int CycleType;
        public bool EnablePerspectiveTextures;
        public bool EnableDetailTextures;
        public bool EnableSharpenTextures;
        public bool EnableTextureLevelOfDetail;
        public bool EnableTextureLookupTable;
        public bool TextureLookupTableType;
        public bool SampleType;
        public bool MidTexel;
        public bool BiLerp0;
        public bool BiLerp1;
        public bool ConvertOne;
        public bool EnableKey;
        public int RgbDitherSelection;
        public int AlphaDitherSelection;
        public int BlendM1A_0;
        public int BlendM1A_1;
        public int BlendM1B_0;
        public int BlendM1B_1;
        public int BlendM2A_0;
        public int BlendM2A_1;
        public int BlendM2B_0;
        public int BlendM2B_1;
        public bool EnableForceBlend;
        public bool AlphaCoverageSelect;
        public bool CoverageTimesAlpha;
        public int ZMode;
        public int CoverageDestination;
        public bool ColorOnCoverage;
        public bool EnableImageRead;
        public bool EnableZUpdate;
        public bool EnableZCompare;
        public bool EnableAntiAliasing;
        public bool ZSourceSelection;
        public bool EnableDitherAlpha;
        public bool EnableAlphaCompare;

        public InternalFlags Flags;

        public OtherModesState() {
            Flags.IsDerivativesStale = true;
        }

        public struct InternalFlags
        {
            public bool IsDerivativesStale;
            public bool UsePartialReject_1Cycle;
            public bool UsePartialReject_2Cycle;
            public int RgbaDither;
            public int GetDitherLevel;
        }
    }
}