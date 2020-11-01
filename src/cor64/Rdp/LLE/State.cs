using System;
namespace cor64.Rdp.LLE
{
    public class State
    {
        public uint Stride;
        public uint WorkerOffset;

        public readonly Span[] SpanBuffer = new Span[1024];

        /* Span States */
        public int SpansDs;
        public int SpansDt;
        public int SpansDw;
        public int SpansDr;
        public int SpansDg;
        public int SpansDb;
        public int SpansDa;
        public int SpansDz;
        public int SpansDzPix;


        public int SpansDrDy;
        public int SpansDgDy;
        public int SpansDbDy;
        public int SpansDaDy;
        public int SpansDzDy;
        public int SpansCdr;
        public int SpansCdg;
        public int SpansCdb;
        public int SpansCda;
        public int SpansCdz;


        public int SpansDsDy;
        public int SpansDtDy;
        public int SpansDwDy;

        public readonly OtherModesState OtherModes = new OtherModesState();

        public readonly ColorRef CombinedColor = new ColorRef();
        public readonly ColorRef Texel0Color = new ColorRef();
        public readonly ColorRef Texel1Color = new ColorRef();
        public Color NextTexelColor;
        public Color NextTexel1Color;
        public readonly ColorRef ShadeColor = new ColorRef();

        public int Noise;
        public int PrimitiveLODFactor;

        public readonly ColorRef PixelColor = new ColorRef();
        public readonly ColorRef MemoryColor = new ColorRef();
        public Color PreMemoryColor;

        public readonly Tile[] Tiles = new Tile[8];

        public int TextureFilter_K0;
        public int TextureFilter_K1;
        public int TextureFilter_K2;
        public int TextureFilter_K3;
        public int YuvConversion_K4;
        public int YuvConversion_K5;
        public int LodFraction;

        public int MaxLevel;
        public int MinLevel;

        // irand
        public uint RandomSeed;

        public readonly ColorRef BlendColor = new ColorRef();
        public readonly ColorRef FogColor = new ColorRef();
        public readonly ColorRef InvPixelColor = new ColorRef();
        public readonly ColorRef PrimColor = new ColorRef();
        public readonly ColorRef EnvironmentColor = new ColorRef();
        public Color KeyScale;
        public readonly ColorRef KeyCenter = new ColorRef();
        public Color KeyWidth;


        public int FramebufferFormat;
        public int FramebufferSize;
        public int FramebufferWidth;
        public uint FramebufferAddress;
        public uint FillColor;

        // Rasterizer
        public Rectangle Clip;
        public int ScField;
        public int ScKeepOdd;

        public uint PrimitiveZ;
        public ushort PrimitiveDeltaZ;

        // Texture
        public int TextureInputFormat;
        public int TextureInputSize;
        public int TextureInputWidth;
        public uint TextureInputAddress;

        public int Cycle1RenderMode;
        public int Cycle2RenderMode;

        // Coverage
        public readonly byte[] ConverageBuffer = new byte[1024];


        // ZBuffer
        public uint ZBufferAddress;
        public int PastRawDZmem;

        public bool PipelineCrashed = true;

        public bool Pipeline_UseLod;
    }
}