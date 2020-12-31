using System;
using cor64.Rdp.Commands;
using NLog;

namespace cor64.Rdp.LLE
{
    public class TextureUnit
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly MadCatRdp m_Rdp;
        private State GS => m_Rdp.GraphicsState;
        private Rdram RDRAM => m_Rdp.RdramMemory;

        public const int FORMAT_RGBA = 0;
        public const int FORMAT_YUV = 1;
        public const int FORMAT_CI = 2;
        public const int FORMAT_IA = 3;
        public const int FORMAT_I = 4;

        public TextureUnit(MadCatRdp rdp)
        {
            m_Rdp = rdp;
        }

        public void GetTexel1_Cycle1(ref int s1, ref int t1, TextureCoordinate tcoord, TextureCoordinate dinc, int scanline, SpanSigs sigs)
        {
            int nextS, nextT, nextSw;

            if (!sigs.endSpan || !sigs.longspan || !GS.SpanBuffer[scanline + 1].ValidLine)
            {
                nextSw = (tcoord.w + dinc.w) >> 16;
                nextS = (tcoord.s + dinc.s) >> 16;
                nextT = (tcoord.t + dinc.t) >> 16;
            }
            else
            {
                int nextScan = scanline + 1;
                nextSw = GS.SpanBuffer[nextScan].w >> 16;
                nextS = GS.SpanBuffer[nextScan].s >> 16;
                nextT = GS.SpanBuffer[nextScan].t >> 16;
            }

            m_Rdp.TextureCoordDivision(nextS, nextT, nextSw, ref s1, ref t1);
        }

        public void PipelineCycle(ref Color tex, ref Color prev, int sss, int sst, uint tileIndex, uint cycle)
        {
            bool biLerp = cycle.IsTrue() ? GS.OtherModes.BiLerp1 : GS.OtherModes.BiLerp0;
            bool convert = GS.OtherModes.ConvertOne && cycle != 0;

            bool maxs = false;
            bool maxt = false;

            int invt3r;
            int invt3g;
            int invt3b;
            int invt3a;

            int sfrac;
            int tfrac;
            int invsf;
            int invtf;
            int sfracrg;
            int invsfrg;
            int sdiff = 0;
            int tdiff = 0;

            Color t0 = new Color();
            Color t1 = new Color();
            Color t2 = new Color();
            Color t3 = new Color();

            int sss1 = sss;
            int sst1 = sst;

            TexCoord.ShiftCycle(GS.Tiles[tileIndex], ref sss1, ref sst1, ref maxs, ref maxt);

            sss1 = Utils.TileRelative(sss1, GS.Tiles[tileIndex].S.CoordLow);
            sst1 = Utils.TileRelative(sst1, GS.Tiles[tileIndex].T.CoordLow);

            if (GS.OtherModes.SampleType || GS.OtherModes.EnableTextureLookupTable)
            {
                sfrac = sss1 & 0x1F;
                tfrac = sst1 & 0x1F;

                TexCoord.ClampCycle(GS.Tiles[tileIndex], ref sss1, ref sst1, ref sfrac, ref tfrac, maxs, maxt);
                TexCoord.MaskCoupled(GS.Tiles[tileIndex], ref sss1, ref sdiff, ref sst1, ref tdiff);

                bool upper = (sfrac + tfrac).IsTrue(0x20);
                bool upperrg;

                if (GS.Tiles[tileIndex].Format == FORMAT_YUV)
                {
                    sfracrg = (sfrac >> 1) | ((sss1 & 1) << 4);
                    upperrg = (sfracrg + tfrac).IsTrue(0x20);

                    // YUV FIX: diff should be 0 if the mask is 0
                    sdiff = GS.Tiles[tileIndex].S.Mask == 0 ? 0 : sdiff;
                    tdiff = GS.Tiles[tileIndex].T.Mask == 0 ? 0 : tdiff;
                }
                else
                {
                    upperrg = upper;
                    sfracrg = sfrac;
                }

                if (biLerp)
                {
                    if (!GS.OtherModes.SampleType)
                    {
                        m_Rdp.RdpTextureMemory.FetchTexel_QuadroTlutNearest(ref t0, ref t1, ref t2, ref t3, sss1, sst1, tileIndex, upper, upperrg);
                    }
                    else if (GS.OtherModes.EnableTextureLookupTable)
                    {
                        m_Rdp.RdpTextureMemory.FetchTexel_QuadroTlut(ref t0, ref t1, ref t2, ref t3, sss1, sdiff, sst1, tdiff, tileIndex, upper, upperrg);
                    }
                    else
                    {
                        m_Rdp.RdpTextureMemory.FetchQuadro(ref t0, ref t1, ref t2, ref t3, sss1, sdiff, sst1, tdiff, tileIndex, upper != upperrg);
                    }

                    bool center;
                    bool centerrg;

                    if (!GS.OtherModes.MidTexel)
                    {
                        center = centerrg = false;
                    }
                    else
                    {
                        center = sfrac == 0x10 && tfrac == 0x10;
                        centerrg = sfracrg == 0x10 && tfrac == 0x10;
                    }

                    if (!convert)
                    {
                        invtf = 0x20 - tfrac;

                        if (!centerrg)
                        {
                            if (upperrg)
                            {
                                invsfrg = 0x20 - sfracrg;

                                tex.R = t3.R + (((invsfrg * (t2.R - t3.R)) + (invtf * (t1.R - t3.R)) + 0x10) >> 5);
                                tex.G = t3.G + (((invsfrg * (t2.G - t3.G)) + (invtf * (t1.G - t3.G)) + 0x10) >> 5);
                            }
                            else
                            {
                                tex.R = t0.R + (((sfracrg * (t1.R - t0.R)) + (tfrac * (t2.R - t0.R)) + 0x10) >> 5);
                                tex.G = t0.G + (((sfracrg * (t1.G - t0.G)) + (tfrac * (t2.G - t0.G)) + 0x10) >> 5);
                            }
                        }
                        else
                        {
                            invt3r = ~t3.R;
                            invt3g = ~t3.G;

                            tex.R = t3.R + ((((t1.R + t2.R) << 6) - (t3.R << 7) + ((invt3r + t0.R) << 6) + 0xC0) >> 8);
                            tex.G = t3.G + ((((t1.G + t2.G) << 6) - (t3.G << 7) + ((invt3g + t0.G) << 6) + 0xC0) >> 8);
                        }

                        if (!center)
                        {
                            if (upper)
                            {
                                invsf = 0x20 - sfrac;

                                tex.B = t3.B + (((invsf * (t2.B - t3.B)) + (invtf * (t1.B - t3.B)) + 0x10) >> 5);
                                tex.A = t3.A + (((invsf * (t2.A - t3.A)) + (invtf * (t1.A - t3.A)) + 0x10) >> 5);
                            }
                            else
                            {
                                tex.B = t0.B + (((sfrac * (t1.B - t0.B)) + (tfrac * (t2.B - t0.B)) + 0x10) >> 5);
                                tex.A = t0.A + (((sfrac * (t1.A - t0.A)) + (tfrac * (t2.A - t0.A)) + 0x10) >> 5);
                            }
                        }
                        else
                        {
                            invt3b = ~t3.B;
                            invt3a = ~t3.A;

                            tex.B = t3.B + ((((t1.B + t2.B) << 6) - (t3.B << 7) + ((invt3b + t0.B) << 6) + 0xC0) >> 8);
                            tex.A = t3.A + ((((t1.A + t2.A) << 6) - (t3.A << 7) + ((invt3a + t0.A) << 6) + 0xC0) >> 8);
                        }
                    }
                    else
                    {
                        int prevr = Utils.Sign(prev.R, 9);
                        int prevg = Utils.Sign(prev.G, 9);
                        int prevb = Utils.Sign(prev.B, 9);

                        if (!centerrg)
                        {
                            if (upperrg)
                            {
                                tex.R = prevb + (((prevr * (t2.R - t3.R)) + (prevg * (t1.R - t3.R)) + 0x80) >> 8);
                                tex.G = prevb + (((prevr * (t2.G - t3.G)) + (prevg * (t1.G - t3.G)) + 0x80) >> 8);
                            }
                            else
                            {
                                tex.R = prevb + (((prevr * (t1.R - t0.R)) + (prevg * (t2.R - t0.R)) + 0x80) >> 8);
                                tex.G = prevb + (((prevr * (t1.G - t0.G)) + (prevg * (t2.G - t0.G)) + 0x80) >> 8);
                            }
                        }
                        else
                        {
                            invt3r = ~t3.R;
                            invt3g = ~t3.G;

                            tex.R = prevb + (((prevr * (t2.R - t3.R)) + (prevg * (t1.R - t3.R)) + ((invt3r + t0.R) << 6) + 0xC0) >> 8);
                            tex.G = prevb + (((prevr * (t2.G - t3.G)) + (prevg * (t1.G - t3.G)) + ((invt3g + t0.G) << 6) + 0xC0) >> 8);
                        }

                        if (!center)
                        {
                            if (upper)
                            {
                                tex.B = prevb + (((prevr * (t2.B - t3.B)) + (prevg * (t1.B - t3.B)) + 0x80) >> 8);
                                tex.A = prevb + (((prevr * (t2.A - t3.A)) + (prevg * (t1.A - t3.A)) + 0x80) >> 8);
                            }
                            else
                            {
                                tex.B = prevb + (((prevr * (t1.B - t0.B)) + (prevg * (t2.B - t0.B)) + 0x80) >> 8);
                                tex.A = prevb + (((prevr * (t1.A - t0.A)) + (prevg * (t2.A - t0.A)) + 0x80) >> 8);
                            }
                        }
                        else
                        {
                            invt3b = ~t3.B;
                            invt3a = ~t3.A;

                            tex.B = prevb + (((prevr * (t2.B - t3.B)) + (prevg * (t1.B - t3.B)) + ((invt3b + t0.B) << 6) + 0xC0) >> 8);
                            tex.A = prevb + (((prevr * (t2.A - t3.A)) + (prevg * (t1.A - t3.A)) + ((invt3a + t0.A) << 6) + 0xC0) >> 8);
                        }
                    }
                }
                else
                {
                    if (convert)
                    {
                        t0 = t3 = prev;

                        t0.R = Utils.Sign(t0.R, 9);
                        t0.G = Utils.Sign(t0.G, 9);
                        t0.B = Utils.Sign(t0.B, 9);
                        t3.R = Utils.Sign(t3.R, 9);
                        t3.G = Utils.Sign(t3.G, 9);
                        t3.B = Utils.Sign(t3.B, 9);
                    }
                    else
                    {
                        if (!GS.OtherModes.SampleType)
                        {
                            m_Rdp.RdpTextureMemory.FetchTexel_QuadroTlutNearest(ref t0, ref t1, ref t2, ref t3, sss1, sst1, tileIndex, upper, upperrg);
                        }
                        else if (GS.OtherModes.EnableTextureLookupTable)
                        {
                            m_Rdp.RdpTextureMemory.FetchTexel_QuadroTlut(ref t0, ref t1, ref t2, ref t3, sss1, sdiff, sst1, tdiff, tileIndex, upper, upperrg);
                        }
                        else
                        {
                            m_Rdp.RdpTextureMemory.FetchQuadro(ref t0, ref t1, ref t2, ref t3, sss1, sdiff, sst1, tdiff, tileIndex, upper != upperrg);
                        }
                    }

                    if (upperrg)
                    {
                        if (upper)
                        {
                            tex.R = t3.B + (((GS.TextureFilter_K0 * t3.G) + 0x80) >> 8);
                            tex.G = t3.B + (((GS.TextureFilter_K1 * t3.R) + (GS.TextureFilter_K2 * t3.G) + 0x80) >> 8);
                            tex.B = t3.B + (((GS.TextureFilter_K3 * t3.R) + 0x80) >> 8);
                            tex.A = t3.B;
                        }
                        else
                        {
                            tex.R = t0.B + (((GS.TextureFilter_K0 * t3.G) + 0x80) >> 8);
                            tex.G = t0.B + (((GS.TextureFilter_K1 * t3.R) + (GS.TextureFilter_K2 * t3.G) + 0x80) >> 8);
                            tex.B = t0.B + (((GS.TextureFilter_K3 * t3.R) + 0x80) >> 8);
                            tex.A = t0.B;
                        }
                    }
                    else
                    {
                        if (upper)
                        {
                            tex.R = t3.B + (((GS.TextureFilter_K0 * t0.G) + 0x80) >> 8);
                            tex.G = t3.B + (((GS.TextureFilter_K1 * t0.R) + (GS.TextureFilter_K2 * t0.G) + 0x80) >> 8);
                            tex.B = t3.B + (((GS.TextureFilter_K3 * t0.R) + 0x80) >> 8);
                            tex.A = t3.B;
                        }
                        else
                        {
                            tex.R = t0.B + (((GS.TextureFilter_K0 * t0.G) + 0x80) >> 8);
                            tex.G = t0.B + (((GS.TextureFilter_K1 * t0.R) + (GS.TextureFilter_K2 * t0.G) + 0x80) >> 8);
                            tex.B = t0.B + (((GS.TextureFilter_K3 * t0.R) + 0x80) >> 8);
                            tex.A = t0.B;
                        }
                    }
                }

                tex.R &= 0x1FF;
                tex.G &= 0x1FF;
                tex.B &= 0x1FF;
                tex.A &= 0x1FF;
            }
            else
            {
                TexCoord.ClampCycle(GS.Tiles[tileIndex], ref sss1, ref sst1, maxs, maxt);

                GS.Tiles[tileIndex].Mask(ref sss1, ref sst1);

                if (biLerp)
                {
                    if (!convert)
                    {
                        m_Rdp.RdpTextureMemory.FetchTexel(ref t0, sss1, sst1, tileIndex);

                        tex.R = t0.R & 0x1FF;
                        tex.G = t0.G & 0x1FF;
                        tex.B = t0.B;
                        tex.A = t0.A;
                    }
                    else
                    {
                        tex.R = tex.G = tex.B = tex.A = prev.B;
                    }
                }
                else
                {
                    if (convert)
                    {
                        t0 = prev;
                        t0.R = Utils.Sign(t0.R, 9);
                        t0.G = Utils.Sign(t0.G, 9);
                        t0.B = Utils.Sign(t0.B, 9);
                    }
                    else
                    {
                        m_Rdp.RdpTextureMemory.FetchTexel(ref t0, sss1, sst1, tileIndex);
                    }

                    tex.R = t0.B + (((GS.TextureFilter_K0 * t0.G) + 0x80) >> 8);
                    tex.G = t0.B + (((GS.TextureFilter_K1 * t0.R) + (GS.TextureFilter_K2 * t0.G) + 0x80) >> 8);
                    tex.B = t0.B + (((GS.TextureFilter_K3 * t0.R) + 0x80) >> 8);
                    tex.A = t0.B & 0x1FF;
                    tex.R &= 0x1FF;
                    tex.G &= 0x1FF;
                    tex.B &= 0x1FF;
                }
            }
        }

        private enum TMEM_FORMAT
        {
            YUV = 0,
            RGBA32,
            Direct
        }

        public void LoadingPipeline(int start, int end, uint tileIndex, bool isQuad, bool ltlut)
        {
            if (end > start && ltlut)
            {
                GS.PipelineCrashed = true;
                return;
            }

            TMEM_FORMAT tmemFormatting;

            var tile = GS.Tiles[tileIndex];
            var tileFormat = (ImageDataFormat)tile.Format;
            var tileSize = (ImageSizeType)tile.Size;

            if (tileFormat == ImageDataFormat.YUV)
            {
                tmemFormatting = TMEM_FORMAT.YUV;
                // Log.Debug("TMEM Load Format: YUV");
            }
            else if (tileFormat == ImageDataFormat.RGBA && tileSize == ImageSizeType.SizeInBytes_32)
            {
                tmemFormatting = TMEM_FORMAT.RGBA32;
                // Log.Debug("TMEM Load Format: RGBA32");
            }
            else
            {
                tmemFormatting = TMEM_FORMAT.Direct;
                // Log.Debug("TMEM Load Format: Direct");
            }

            int tiadvance = 0;
            int spanadvance = 0;

            switch ((ImageSizeType)GS.TextureInputSize)
            {
                case ImageSizeType.SizeInBytes_4:
                    {
                        GS.PipelineCrashed = true;
                        return;
                    }

                case ImageSizeType.SizeInBytes_8:
                    {
                        tiadvance = 8;
                        spanadvance = 8;
                        break;
                    }

                case ImageSizeType.SizeInBytes_16:
                    {
                        if (!ltlut)
                        {
                            tiadvance = 8;
                            spanadvance = 4;
                        }
                        else
                        {
                            tiadvance = 2;
                            spanadvance = 1;
                        }

                        break;
                    }

                case ImageSizeType.SizeInBytes_32:
                    {
                        tiadvance = 8;
                        spanadvance = 2;
                        break;
                    }
            }

            for (int i = start; i <= end; i++)
            {
                int xtart = GS.SpanBuffer[i].lx;
                int xend = GS.SpanBuffer[i].unscrx;
                int length = (xtart - xend + 1) & 0xFFF;

                int s = GS.SpanBuffer[i].s;
                int t = GS.SpanBuffer[i].t;

                int ti_index = (GS.TextureInputWidth * i) + xend;
                int tiptr = (int)(GS.TextureInputAddress + Utils.PixelsToBytes(ti_index, GS.TextureInputSize));

                int dsinc = GS.SpansDs;
                int dtinc = GS.SpansDt;

                for (int j = 0; j < length; j += spanadvance)
                {
                    int ss = s >> 16;
                    int st = t >> 16;

                    int sss = ss & 0xFFFF;
                    int sst = st & 0xFFFF;

                    TexCoord.PipelineLoad(GS.Tiles[tileIndex], ref sss, ref sst, isQuad);

                    bool dswap = sst.IsTrue(1);

                    int tmemidx0 = 0, tmemidx1 = 0, tmemidx2 = 0, tmemidx3 = 0;
                    bool bit3f = false;
                    bool hibit = false;

                    TextureMemory.GetTmemIndex(tile, sss, sst, ref tmemidx0, ref tmemidx1, ref tmemidx2, ref tmemidx3, ref bit3f, ref hibit);

                    /* XXX: Convert the tex pointer into a full address */

                    uint readidx32 = (uint)((tiptr >> 2) & ~1);
                    uint readval0 = RDRAM.ReadIdx32(readidx32);

                    readidx32++;
                    uint readval1 = RDRAM.ReadIdx32(readidx32);

                    readidx32++;
                    uint readval2 = RDRAM.ReadIdx32(readidx32);

                    readidx32++;
                    uint readval3 = RDRAM.ReadIdx32(readidx32);

                    ulong loadqword = LoadQword(tiptr, ltlut, readval0, readval1, readval2, readval3);

                    switch (tmemFormatting)
                    {
                        case TMEM_FORMAT.YUV:
                            {
                                readval0 = (uint)((((loadqword >> 56) & 0xFF) << 24) | (((loadqword >> 40) & 0xFF) << 16) | (((loadqword >> 24) & 0xFF) << 8) | (((loadqword >> 8) & 0xFF) << 0));
                                readval1 = (uint)((((loadqword >> 48) & 0xFF) << 24) | (((loadqword >> 32) & 0xFF) << 16) | (((loadqword >> 16) & 0xFF) << 8) | (((loadqword >> 0) & 0xFF) << 0));

                                if (bit3f)
                                {
                                    m_Rdp.RdpTextureMemory.WriteTmem16Idx(tmemidx2, (ushort)(readval0 >> 16));
                                    m_Rdp.RdpTextureMemory.WriteTmem16Idx(tmemidx3, (ushort)readval0);
                                    m_Rdp.RdpTextureMemory.WriteTmem16Idx(tmemidx2 | 0x400, (ushort)(readval1 >> 16));
                                    m_Rdp.RdpTextureMemory.WriteTmem16Idx(tmemidx3 | 0x400, (ushort)readval1);
                                }
                                else
                                {
                                    m_Rdp.RdpTextureMemory.WriteTmem16Idx(tmemidx0, (ushort)(readval0 >> 16));
                                    m_Rdp.RdpTextureMemory.WriteTmem16Idx(tmemidx1, (ushort)readval0);
                                    m_Rdp.RdpTextureMemory.WriteTmem16Idx(tmemidx0 | 0x400, (ushort)(readval1 >> 16));
                                    m_Rdp.RdpTextureMemory.WriteTmem16Idx(tmemidx1 | 0x400, (ushort)readval1);
                                }

                                break;
                            }

                        case TMEM_FORMAT.RGBA32:
                            {
                                readval0 = (uint)(((loadqword >> 48) << 16) | ((loadqword >> 16) & 0xFFFF));
                                readval1 = (uint)((((loadqword >> 32) & 0xFFFF) << 16) | (loadqword & 0xFFFF));

                                if (bit3f)
                                {
                                    m_Rdp.RdpTextureMemory.WriteTmem16Idx(tmemidx2,         (ushort)(readval0 >> 16));
                                    m_Rdp.RdpTextureMemory.WriteTmem16Idx(tmemidx3,         (ushort)readval0);
                                    m_Rdp.RdpTextureMemory.WriteTmem16Idx(tmemidx2 | 0x400, (ushort)(readval1 >> 16));
                                    m_Rdp.RdpTextureMemory.WriteTmem16Idx(tmemidx3 | 0x400, (ushort)readval1);
                                }
                                else
                                {
                                    m_Rdp.RdpTextureMemory.WriteTmem16Idx(tmemidx0,         (ushort)(readval0 >> 16));
                                    m_Rdp.RdpTextureMemory.WriteTmem16Idx(tmemidx1,         (ushort)readval0);
                                    m_Rdp.RdpTextureMemory.WriteTmem16Idx(tmemidx0 | 0x400, (ushort)(readval1 >> 16));
                                    m_Rdp.RdpTextureMemory.WriteTmem16Idx(tmemidx1 | 0x400, (ushort)readval1);
                                }

                                break;
                            }

                        case TMEM_FORMAT.Direct:
                            {
                                if (!dswap)
                                {
                                    if (!hibit)
                                    {
                                        m_Rdp.RdpTextureMemory.WriteTmem16Idx(tmemidx0, (ushort)(loadqword >> 48));
                                        m_Rdp.RdpTextureMemory.WriteTmem16Idx(tmemidx1, (ushort)(loadqword >> 32));
                                        m_Rdp.RdpTextureMemory.WriteTmem16Idx(tmemidx2, (ushort)(loadqword >> 16));
                                        m_Rdp.RdpTextureMemory.WriteTmem16Idx(tmemidx3, (ushort)loadqword);
                                    }
                                    else
                                    {
                                        m_Rdp.RdpTextureMemory.WriteTmem16Idx(tmemidx0 | 0x400, (ushort)(loadqword >> 48));
                                        m_Rdp.RdpTextureMemory.WriteTmem16Idx(tmemidx1 | 0x400, (ushort)(loadqword >> 32));
                                        m_Rdp.RdpTextureMemory.WriteTmem16Idx(tmemidx2 | 0x400, (ushort)(loadqword >> 16));
                                        m_Rdp.RdpTextureMemory.WriteTmem16Idx(tmemidx3 | 0x400, (ushort)loadqword);
                                    }
                                }
                                else
                                {
                                    if (!hibit)
                                    {
                                        m_Rdp.RdpTextureMemory.WriteTmem16Idx(tmemidx0, (ushort)(loadqword >> 16));
                                        m_Rdp.RdpTextureMemory.WriteTmem16Idx(tmemidx1, (ushort)loadqword);
                                        m_Rdp.RdpTextureMemory.WriteTmem16Idx(tmemidx2, (ushort)(loadqword >> 48));
                                        m_Rdp.RdpTextureMemory.WriteTmem16Idx(tmemidx3, (ushort)(loadqword >> 32));
                                    }
                                    else
                                    {
                                        m_Rdp.RdpTextureMemory.WriteTmem16Idx(tmemidx0 | 0x400, (ushort)(loadqword >> 16));
                                        m_Rdp.RdpTextureMemory.WriteTmem16Idx(tmemidx1 | 0x400, (ushort)loadqword);
                                        m_Rdp.RdpTextureMemory.WriteTmem16Idx(tmemidx2 | 0x400, (ushort)(loadqword >> 48));
                                        m_Rdp.RdpTextureMemory.WriteTmem16Idx(tmemidx3 | 0x400, (ushort)(loadqword >> 32));
                                    }
                                }

                                break;
                            }

                        default: throw new RdpException("invalid tmem format");
                    }

                    s = (s + dsinc) & ~0x1F;
                    t = (t + dtinc) & ~0x1F;
                    tiptr += tiadvance;
                }
            }
        }

        private static ulong LoadQword(int texturePointer, bool ltlut, uint readval0, uint readval1, uint readval2, uint readval3)
        {
            ushort tempshort;

            switch (texturePointer & 7)
            {
                case 0:
                    {
                        if (!ltlut)
                        {
                            return ((ulong)readval0 << 32) | readval1;
                        }
                        else
                        {
                            tempshort = (ushort)(readval0 >> 16);
                            return ((ulong)tempshort << 48) | ((ulong)tempshort << 32) | ((ulong)tempshort << 16) | tempshort;
                        }
                    }
                case 1:
                    {
                        return ((ulong)readval0 << 40) | ((ulong)readval1 << 8) | (readval2 >> 24);
                    }
                case 2:
                    {
                        if (!ltlut)
                        {
                            return ((ulong)readval0 << 48) | ((ulong)readval1 << 16) | (readval2 >> 16);
                        }
                        else
                        {
                            tempshort = (ushort)readval0;
                            return ((ulong)tempshort << 48) | ((ulong)tempshort << 32) | ((ulong)tempshort << 16) | tempshort;
                        }
                    }
                case 3:
                    {
                        return ((ulong)readval0 << 56) | ((ulong)readval1 << 24) | (readval2 >> 8);
                    }
                case 4:
                    {
                        if (!ltlut)
                        {
                            return ((ulong)readval1 << 32) | readval2;
                        }
                        else
                        {
                            tempshort = (ushort)(readval1 >> 16);
                            return ((ulong)tempshort << 48) | ((ulong)tempshort << 32) | ((ulong)tempshort << 16) | tempshort;
                        }
                    }
                case 5:
                    {
                        return ((ulong)readval1 << 40) | ((ulong)readval2 << 8) | (readval3 >> 24);
                    }
                case 6:
                    {
                        if (!ltlut)
                        {
                            return ((ulong)readval1 << 48) | ((ulong)readval2 << 16) | (readval3 >> 16);
                        }
                        else
                        {
                            tempshort = (ushort)readval1;
                            return ((ulong)tempshort << 48) | ((ulong)tempshort << 32) | ((ulong)tempshort << 16) | tempshort;
                        }
                    }
                case 7:
                    {
                        return ((ulong)readval1 << 56) | ((ulong)readval2 << 24) | (readval3 >> 8);
                    }

                default: return 0;
            }
        }

        public void EdgeWalkerForLoads(EdgeWalkerLoadData data)
        {
            bool ltlut = data.commandType.Id == RdpCommandTypes.LoadTLUT.Id;
            bool coord_quad = ltlut || (data.commandType.Id == RdpCommandTypes.LoadBlock.Id);

            GS.MaxLevel = 0;

            GS.SpansDs = data.dsdx & ~0x1F;
            GS.SpansDt = data.dtdx & ~0x1F;
            GS.SpansDw = 0;

            int xright = data.xh & ~1;
            int xleft = data.xm & ~1;

            int j;

            void AdjustAttrLoad()
            {
                GS.SpanBuffer[j].s = data.s & ~0x3FF;
                GS.SpanBuffer[j].t = data.t & ~0x3FF;
            }

            void AddValuesLoad()
            {
                data.t += data.dtde;
            }

            int maxxmx = 0;
            int minxhx = 0;
            int ycur = data.yh & ~3;
            int ylfar = data.yl | 3;
            int yllimit = data.yl;
            int yhlimit = data.yh;

            int xend = xright >> 16;

            for (int k = ycur; k <= ylfar; k++)
            {
                if (k == data.ym)
                {
                    xleft = data.xl & ~1;
                }

                int spix = k & 3;

                if (!k.IsTrue(~0xFFF))
                {
                    j = k >> 2;

                    bool valid_y = !(k < yhlimit || k >= yllimit);

                    if (spix == 0)
                    {
                        maxxmx = 0;
                        minxhx = 0xFFF;
                    }

                    int xrsc = (xright >> 13) & 0x7FFE;
                    int xlsc = (xleft >> 13) & 0x7FFE;

                    if (valid_y)
                    {
                        maxxmx = (((xlsc >> 3) & 0xFFF) > maxxmx) ? (xlsc >> 3) & 0xFFF : maxxmx;
                        minxhx = (((xrsc >> 3) & 0xFFF) < minxhx) ? (xrsc >> 3) & 0xFFF : minxhx;
                    }

                    if (spix == 0)
                    {
                        GS.SpanBuffer[j].unscrx = xend;
                        AdjustAttrLoad();
                    }

                    if (spix == 3)
                    {
                        GS.SpanBuffer[j].lx = maxxmx;
                        GS.SpanBuffer[j].rx = minxhx;
                    }
                }

                if (spix == 3)
                {
                    AddValuesLoad();
                }
            }

            LoadingPipeline(yhlimit >> 2, yllimit >> 2, (uint)data.tileIndex, coord_quad, ltlut);
        }
    }
}