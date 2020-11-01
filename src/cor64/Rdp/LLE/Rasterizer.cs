using System;
using NLog;

namespace cor64.Rdp.LLE
{
    public partial class Rasterizer
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly MadCatRdp m_Rdp;
        private State GS => m_Rdp.GraphicsState;
        private TextureUnit TU => m_Rdp.RdpTextureUnit;
        private bool m_Disable;

        public Rasterizer(MadCatRdp rdp)
        {
            m_Rdp = rdp;
            GS.Clip.xh = 0x2000;
            GS.Clip.yh = 0x2000;
        }

        public void Disable() {
            m_Disable = true;
        }

        public void Enable() {
            m_Disable = false;
        }

        public static int NormalizeDzpix(int sum)
        {
            if (sum.IsTrue(0xC000))
            {
                return 0x8000;
            }

            if (!sum.IsTrue(0xFFFF))
            {
                return 1;
            }

            if (sum == 1)
            {
                return 3;
            }

            for (int count = 0x2000; count > 0; count >>= 1)
            {
                if (sum.IsTrue(count))
                {
                    return count << 1;
                }
            }

            throw new RdpException("Rasterizer_NormalizeDzpix: Invalid codepath");
        }

        public void ReplicateForCopy(ref int outbyte, int inshort, int nibbleOffset, int tileIndex, int tformat, int tsize)
        {
            int lownib, hinib;

            var texFmt = (ImageDataFormat)tformat;

            switch ((ImageSizeType)tsize)
            {
                case ImageSizeType.SizeInBytes_4:
                    {
                        lownib = (nibbleOffset ^ 3) << 2;
                        lownib = (inshort >> lownib) & 0xF;

                        if (texFmt == ImageDataFormat.ColorIndex)
                        {
                            outbyte = (GS.Tiles[tileIndex].Palette << 4) | lownib;
                        }
                        else if (texFmt == ImageDataFormat.IntensityWithAlpha)
                        {
                            lownib = (lownib << 4) | lownib;
                            outbyte = (lownib & 0xE0) | ((lownib & 0xE0) >> 3) | ((lownib & 0xC0) >> 6);
                        }
                        else
                        {
                            outbyte = (lownib << 4) | lownib;
                        }

                        break;
                    }

                case ImageSizeType.SizeInBytes_8:
                    {
                        hinib = ((nibbleOffset ^ 3) | 1) << 2;

                        if (texFmt == ImageDataFormat.IntensityWithAlpha)
                        {
                            lownib = (inshort >> hinib) & 0xF;
                            outbyte = (lownib << 4) | lownib;
                        }
                        else
                        {
                            lownib = (inshort >> (hinib & ~4)) & 0xF;
                            hinib = (inshort >> hinib) & 0xF;
                            outbyte = (hinib << 4) | lownib;
                        }

                        break;
                    }

                default:
                    {
                        outbyte = (inshort >> 8) | 0xFF;
                        break;
                    }
            }
        }

        public void FetchQwordCopy(ref uint hidword, ref uint lowdword, int ssss, int ssst, int tileIndex)
        {
            int shorta, shortb, shortc, shortd;
            int[] sortshort = new int[8];
            bool[] hibits = new bool[6];
            int[] lowbits = new int[6];
            Tile tile = GS.Tiles[tileIndex];

            int sss = ssss;
            int sst = ssst;
            int sss1 = 0;
            int sss2 = 0;
            int sss3 = 0;
            ImageDataFormat tformat;
            ImageSizeType tsize;

            if (GS.OtherModes.EnableTextureLookupTable)
            {
                tsize = ImageSizeType.SizeInBytes_16;
                tformat = GS.OtherModes.TextureLookupTableType ? ImageDataFormat.IntensityWithAlpha : ImageDataFormat.RGBA;
            }
            else
            {
                tsize = (ImageSizeType)GS.Tiles[tileIndex].Size;
                tformat = (ImageDataFormat)GS.Tiles[tileIndex].Format;
            }

            TexCoord.PipelineCopy(tile, ref sss, ref sss1, ref sss2, ref sss3, ref sst);
            m_Rdp.RdpTextureMemory.Copy(GS, sss, sss1, sss2, sss3, sst, tileIndex, sortshort, hibits, lowbits);

            bool largeTex =
            tformat == ImageDataFormat.YUV ||
            (tformat == ImageDataFormat.RGBA && tsize == ImageSizeType.SizeInBytes_32);

            if (GS.OtherModes.EnableTextureLookupTable)
            {
                shorta = sortshort[4];
                shortb = sortshort[5];
                shortc = sortshort[6];
                shortd = sortshort[7];
            }
            else if (largeTex)
            {
                shorta = sortshort[0];
                shortb = sortshort[1];
                shortc = sortshort[2];
                shortd = sortshort[3];
            }
            else
            {
                shorta = hibits[0] ? sortshort[4] : sortshort[0];
                shortb = hibits[1] ? sortshort[5] : sortshort[1];
                shortc = hibits[3] ? sortshort[6] : sortshort[2];
                shortd = hibits[4] ? sortshort[7] : sortshort[3];
            }

            lowdword = ((uint)shortc << 16) | (uint)shortd;

            if (tsize == ImageSizeType.SizeInBytes_16)
            {
                hidword = ((uint)shorta << 16) | (uint)shortb;
            }
            else
            {
                ReplicateForCopy(ref shorta, shorta, lowbits[0] & 3, tileIndex, (int)tformat, (int)tsize);
                ReplicateForCopy(ref shortb, shortb, lowbits[1] & 3, tileIndex, (int)tformat, (int)tsize);
                ReplicateForCopy(ref shortc, shortc, lowbits[3] & 3, tileIndex, (int)tformat, (int)tsize);
                ReplicateForCopy(ref shortd, shortd, lowbits[4] & 3, tileIndex, (int)tformat, (int)tsize);

                hidword = ((uint)shorta << 24) | ((uint)shortb << 16) | ((uint)shortc << 8) | (uint)shortd;
            }
        }

        public void RgbaCorrect(int offx, int offy, int r, int g, int b, int a, uint cvg)
        {
            if (cvg == 8)
            {
                r >>= 2;
                g >>= 2;
                b >>= 2;
                a >>= 2;
            }
            else
            {
                int summand_r = (offx * GS.SpansCdr) + (offy * GS.SpansDrDy);
                int summand_g = (offx * GS.SpansCdg) + (offy * GS.SpansDgDy);
                int summand_b = (offx * GS.SpansCdb) + (offy * GS.SpansDbDy);
                int summand_a = (offx * GS.SpansCda) + (offy * GS.SpansDaDy);
                r = ((r << 2) + summand_r) >> 4;
                g = ((g << 2) + summand_g) >> 4;
                b = ((b << 2) + summand_b) >> 4;
                a = ((a << 2) + summand_a) >> 4;
            }

            GS.ShadeColor.R = Combiner.LookupClamp(r & 0x1FF);
            GS.ShadeColor.G = Combiner.LookupClamp(g & 0x1FF);
            GS.ShadeColor.B = Combiner.LookupClamp(b & 0x1FF);
            GS.ShadeColor.A = Combiner.LookupClamp(a & 0x1FF);
        }

        public void ZCorrect(int offx, int offy, ref int z, uint cvg)
        {
            int sz = z;

            if (cvg == 8)
            {
                sz >>= 3;
            }
            else
            {
                int summand_z = (offx * GS.SpansCdz) + (offy * GS.SpansDzDy);
                sz = ((sz << 2) + summand_z) >> 5;
            }

            int zanded = (sz & 0x60000) >> 17;

            switch (zanded)
            {
                case 0:
                case 1: z = sz & 0x3FFFF; break;
                case 2: z = 0x3FFFF; break;
                case 3: z = 0; break;
            }
        }

        public void RenderSpans_1Cycle(bool enableTexture, bool enableTexel1, int start, int end, int tileIndex, bool flip)
        {
            //Log.Debug("Spans 1Cycle start={0:x8} end={1:X8}", start, end);

            int zBufferAddr = (int)(GS.ZBufferAddress >> 1);
            int zBufferCurrent;
            byte offx = 0;
            byte offy = 0;
            SpanSigs sigs = new SpanSigs();
            bool enableBlend = false;
            int preWrap = 0;
            uint pixelCoverage = 0;
            bool pixelCvgBit = false;
            uint pixelMemCoverage = 0;
            int primTile = tileIndex;
            int tile1 = tileIndex;
            int newTile = tileIndex;
            int news = 0;
            int newt = 0;

            int drinc;
            int dginc;
            int dbinc;
            int dainc;
            int dzinc;
            int dsinc;
            int dtinc;
            int dwinc;
            int xinc;

            if (flip)
            {
                drinc = GS.SpansDr;
                dginc = GS.SpansDg;
                dbinc = GS.SpansDb;
                dainc = GS.SpansDa;
                dzinc = GS.SpansDz;
                dsinc = GS.SpansDs;
                dtinc = GS.SpansDt;
                dwinc = GS.SpansDw;
                xinc = 1;
            }
            else
            {
                drinc = -GS.SpansDr;
                dginc = -GS.SpansDg;
                dbinc = -GS.SpansDb;
                dainc = -GS.SpansDa;
                dzinc = -GS.SpansDz;
                dsinc = -GS.SpansDs;
                dtinc = -GS.SpansDt;
                dwinc = -GS.SpansDw;
                xinc = -1;
            }

            int dzpix;

            if (!GS.OtherModes.ZSourceSelection)
            {
                dzpix = GS.SpansDzPix;
            }
            else
            {
                dzpix = GS.PrimitiveDeltaZ;
                dzinc = GS.SpansCdz = GS.SpansDzDy = 0;
            }

            int dzpixenc = (int)ZBuffer.DzCompress((uint)dzpix);

            int cdith = 7;
            int adith = 0;
            int sss = 0;
            int sst = 0;

            int preloadfrac = 0;
            int fir = 0;
            int fig = 0;
            int fib = 0;

            TextureCoordinate tcoordInc = new TextureCoordinate {
                s = dsinc,
                t = dtinc,
                w = dwinc
            };

            for (int i = start; i <= end; i++)
            {
                if (GS.SpanBuffer[i].ValidLine)
                {
                    int xstart = GS.SpanBuffer[i].lx;
                    int xend = GS.SpanBuffer[i].unscrx;
                    int xendsc = GS.SpanBuffer[i].rx;
                    int r = GS.SpanBuffer[i].r;
                    int g = GS.SpanBuffer[i].g;
                    int b = GS.SpanBuffer[i].b;
                    int a = GS.SpanBuffer[i].a;
                    int z = GS.OtherModes.ZSourceSelection ? (int)GS.PrimitiveZ : GS.SpanBuffer[i].z;
                    int s = GS.SpanBuffer[i].s;
                    int t = GS.SpanBuffer[i].t;
                    int w = GS.SpanBuffer[i].w;
                    int x = xendsc;

                    int currentPixel = (GS.FramebufferWidth * i) + x;
                    zBufferCurrent = zBufferAddr + currentPixel;

                    int scdiff;
                    int length;

                    if (!flip)
                    {
                        length = xendsc - xstart;
                        scdiff = xend - xendsc;
                        Coverage.Compute(GS, i);
                    }
                    else
                    {
                        length = xstart - xendsc;
                        scdiff = xendsc - xend;
                        Coverage.Compute(GS, i, true);
                    }

                    if (scdiff.IsTrue())
                    {
                        scdiff &= 0xFFF;
                        r += drinc * scdiff;
                        g += dginc * scdiff;
                        b += dbinc * scdiff;
                        a += dainc * scdiff;
                        z += dzinc * scdiff;
                        s += dsinc * scdiff;
                        t += dtinc * scdiff;
                        w += dwinc * scdiff;
                    }

                    if (enableTexture)
                    {
                        int lodlength = length + scdiff;

                        sigs.longspan = lodlength > 7;
                        sigs.midSpan = lodlength == 7;

                        if (enableTexel1) {
                            sigs.oneLessThanMid = lodlength == 6;
                        }
                    }

                    TextureCoordinate tcoord = new TextureCoordinate
                    {
                        s = s,
                        t = t,
                        w = w
                    };

                    for (int j = 0; j <= length; j++)
                    {
                        int sr = r >> 14;
                        int sg = g >> 14;
                        int sb = b >> 14;
                        int sa = a >> 14;

                        int ss = tcoord.s >> 16;
                        int st = tcoord.t >> 16;
                        int sw = tcoord.w >> 16;

                        int sz = (z >> 10) & 0x3FFFFF;

                        if (enableTexture)
                        {
                            sigs.endSpan = j == length;
                            sigs.preEndSpan = j == (length - 1);
                        }

                        Coverage.LookupMaskDerivatives(GS.ConverageBuffer[x], ref offx, ref offy, ref pixelCoverage, ref pixelCvgBit);


                        if (enableTexel1)
                            TU.GetTexel1_Cycle1(ref news, ref newt, tcoord, tcoordInc, i, sigs);

                        if (enableTexture)
                        {
                            if (j.IsTrue() && enableTexel1)
                            {
                                GS.Texel0Color.SetColor(GS.Texel1Color);
                                GS.LodFraction = preloadfrac;
                            }
                            else
                            {
                                m_Rdp.TextureCoordDivision(ss, st, sw, ref sss, ref sst);

                                if (enableTexel1)
                                    LevelOfDetail.PassCurrent_Cycle1(this.m_Rdp, ref sss, ref sst, news, newt, tcoord, tcoordInc, i, primTile, ref tile1, ref sigs);
                                else
                                    LevelOfDetail.PassCurrent_Cycle1_Simple(this.m_Rdp, ref sss, ref sst, tcoord, tcoordInc, i, primTile, ref tile1, ref sigs);

                                TU.PipelineCycle(ref GS.Texel0Color.color, ref GS.Texel0Color.color, sss, sst, (uint)tile1, 0);
                            }

                            if (enableTexel1)
                            {
                                tcoord += tcoordInc;

                                sigs.nextSpan = sigs.endSpan;
                                sigs.endSpan = sigs.preEndSpan;
                                sigs.preEndSpan = j == (length - 2);

                                LevelOfDetail.PassWithNext_1Cycle(this.m_Rdp, ref news, ref newt, tcoord, tcoordInc, i, primTile, ref newTile, ref sigs, ref preloadfrac);
                                TU.PipelineCycle(ref GS.Texel1Color.color, ref GS.Texel1Color.color, news, newt, (uint)newTile, 0);
                            }
                        }

                        RgbaCorrect(offx, offy, sr, sg, sb, sa, pixelCoverage);
                        ZCorrect(offx, offy, ref sz, pixelCoverage);

                        if (GS.OtherModes.Flags.GetDitherLevel < 2)
                        {
                            Dither.GetDitherNoise(GS, x, i, ref cdith, ref adith);
                        }

                        m_Rdp.RdpCombiner.Pass_1Cycle(adith, ref pixelCoverage);

                        m_Rdp.N64Framebuffer.ReadCycle1((uint)currentPixel, ref pixelMemCoverage);

                        if (ZBuffer.ZCompare(m_Rdp, (uint)zBufferCurrent, (uint)sz, (ushort)dzpix, dzpixenc, ref enableBlend, ref preWrap, ref pixelCoverage, pixelMemCoverage))
                        {
                            if (m_Rdp.RdpBlender.FramebufferPass(0, ref fir, ref fig, ref fib, cdith, enableBlend, preWrap, pixelCoverage, pixelCvgBit))
                            {
                                m_Rdp.N64Framebuffer.Write((uint)currentPixel, fir, fig, fib, enableBlend, pixelCoverage, pixelMemCoverage);

                                if (GS.OtherModes.EnableZUpdate)
                                {
                                    ZBuffer.ZStore(m_Rdp.RdramMemory, (uint)zBufferCurrent, (uint)sz, dzpixenc);
                                }
                            }
                        }

                        if (enableTexture && !enableTexel1) {
                            tcoord += tcoordInc;
                        }

                        r += drinc;
                        g += dginc;
                        b += dbinc;
                        a += dainc;
                        z += dzinc;

                        x += xinc;
                        currentPixel += xinc;
                        zBufferCurrent += xinc;
                    }
                }
            }
        }

        public void RenderSpans_2Cycle(bool enableTexture, bool enableTexel1, bool enableTexelNext, int start, int end, int tileIndex, bool flip)
        {
            int zb = (int)(GS.ZBufferAddress >> 1);
            byte offx = 0;
            byte offy = 0;
            int preloadfrac = 0;
            Color nextTexel1Color = new Color();
            bool enableBlend = false;
            int preWrap = 0;
            uint curpixel_cvg = 0;
            bool curpixel_cvbit = false;
            uint curpixel_memcvg = 0;
            uint nextpixel_cvg = 0;
            uint acalpha = 0;

            int tile2 = (tileIndex + 1) & 7;
            int tile1 = tileIndex;
            int primTile = tileIndex;
            int tile3 = tileIndex;
            int dwinc;
            int dginc;
            int dzinc;
            int xinc;
            int drinc;
            int dainc;
            int dsinc;
            int dtinc;
            int dbinc;

            if (flip)
            {
                drinc = GS.SpansDr;
                dginc = GS.SpansDg;
                dbinc = GS.SpansDb;
                dainc = GS.SpansDa;
                dzinc = GS.SpansDz;
                dsinc = GS.SpansDs;
                dtinc = GS.SpansDt;
                dwinc = GS.SpansDw;
                xinc = 1;
            }
            else
            {
                drinc = -GS.SpansDr;
                dginc = -GS.SpansDg;
                dbinc = -GS.SpansDb;
                dainc = -GS.SpansDa;
                dzinc = -GS.SpansDz;
                dsinc = -GS.SpansDs;
                dtinc = -GS.SpansDt;
                dwinc = -GS.SpansDw;
                xinc = -1;
            }

            var dinc = new TextureCoordinate(dsinc, dtinc, dwinc);

            ushort dzpix;
            if (!GS.OtherModes.ZSourceSelection)
            {
                dzpix = (ushort)GS.SpansDzPix;
            }
            else
            {
                dzpix = (ushort)GS.PrimitiveDeltaZ;
                dzinc = GS.SpansCdz = GS.SpansDzDy = 0;
            }

            int dzpixenc = (int)ZBuffer.DzCompress(dzpix);

            int cdith = 0;
            int adith = 0;
            int sz = 0;
            int sss = 0;
            int sst = 0;
            int fir = 0;
            int fig = 0;
            int fib = 0;

            for (int i = start; i <= end; i++)
            {
                if (GS.SpanBuffer[i].ValidLine)
                {
                    var curspan = GS.SpanBuffer[i];

                    int xstart = curspan.lx;
                    int xend = curspan.unscrx;
                    int xendsc = curspan.rx;
                    int r = curspan.r;
                    int g = curspan.g;
                    int b = curspan.b;
                    int a = curspan.a;
                    int z = GS.OtherModes.ZSourceSelection ? (int)GS.PrimitiveZ : curspan.z;
                    int s = curspan.s;
                    int t = curspan.t;
                    int w = curspan.w;
                    int x = xendsc;
                    uint curpixel = (uint)((GS.FramebufferWidth * i) + x);
                    int zbcur = (int)(zb + curpixel);

                    int length;
                    int scdiff;

                    if (!flip)
                    {
                        length = xendsc - xstart;
                        scdiff = xend - xendsc;
                        Coverage.Compute(GS, i);
                    }
                    else
                    {
                        length = xstart - xendsc;
                        scdiff = xendsc - xend;
                        Coverage.Compute(GS, i, true);
                    }

                    if (scdiff.IsTrue())
                    {
                        scdiff &= 0xFFF;
                        r += drinc * scdiff;
                        g += dginc * scdiff;
                        b += dbinc * scdiff;
                        a += dainc * scdiff;
                        z += dzinc * scdiff;
                        s += dsinc * scdiff;
                        t += dtinc * scdiff;
                        w += dwinc * scdiff;
                    }

                    int lodlength = length + scdiff;

                    for (int j = 0; j <= length; j++)
                    {
                        sz = (z >> 10) & 0x3FFFFF;

                        int sr;
                        int sg;
                        int sb;
                        int sa;
                        int ss;
                        int st;
                        int sw;

                        if (!j.IsTrue())
                        {
                            sr = r >> 14;
                            sg = g >> 14;
                            sb = b >> 14;
                            sa = a >> 14;
                            ss = s >> 16;
                            st = t >> 16;
                            sw = w >> 16;

                            if (enableTexture)
                            {
                                m_Rdp.TextureCoordDivision(ss, st, sw, ref sss, ref sst);

                                var tcoord = new TextureCoordinate(s, t, w);

                                if (enableTexel1)
                                    LevelOfDetail.Pass(m_Rdp, ref sss, ref sst, tcoord, dinc, primTile, ref tile1, ref tile2, ref GS.LodFraction);
                                else
                                    LevelOfDetail.Pass_NoTexel1(m_Rdp, ref sss, ref sst, tcoord, dinc, primTile, ref tile1);

                                TU.PipelineCycle(ref GS.Texel0Color.color, ref GS.Texel0Color.color, sss, sst, (uint)tile1, 0);

                                if (enableTexel1)
                                    TU.PipelineCycle(ref GS.Texel1Color.color, ref GS.Texel1Color.color, sss, sst, (uint)tile2, 1);
                            }

                            Coverage.LookupMaskDerivatives(GS.ConverageBuffer[x], ref offx, ref offy, ref curpixel_cvg, ref curpixel_cvbit);

                            RgbaCorrect(offx, offy, sr, sg, sb, sa, curpixel_cvg);

                            if (GS.OtherModes.Flags.GetDitherLevel < 2)
                            {
                                Dither.GetDitherNoise(GS, x, i, ref cdith, ref adith);
                            }

                            m_Rdp.RdpCombiner.Pass_2Cycle_Cycle0(adith, (int)curpixel_cvg, ref acalpha);
                        }

                        if (enableTexelNext && enableTexel1 && enableTexture)
                        {
                            s += dsinc;
                            t += dtinc;
                            w += dwinc;

                            ss = s >> 16;
                            st = t >> 16;
                            sw = w >> 16;

                            m_Rdp.TextureCoordDivision(ss, st, sw, ref sss, ref sst);

                            var tcoord = new TextureCoordinate(s, t, w);

                            if (j < length || !GS.SpanBuffer[i + i].ValidLine || lodlength < 3)
                            {
                                LevelOfDetail.Pass(m_Rdp, ref sss, ref sst, tcoord, dinc, primTile, ref tile1, ref tile2, ref preloadfrac);
                                TU.PipelineCycle(ref GS.NextTexelColor, ref GS.NextTexelColor, sss, sst, (uint)tile1, 0);
                                TU.PipelineCycle(ref GS.NextTexel1Color, ref GS.NextTexelColor, sss, sst, (uint)tile2, 1);
                            }
                            else
                            {
                                int sss2 = 0;
                                int sst2 = 0;

                                ss = GS.SpanBuffer[i + 1].s >> 16;
                                st = GS.SpanBuffer[i + 1].t >> 16;
                                sw = GS.SpanBuffer[i + 1].w >> 16;

                                m_Rdp.TextureCoordDivision(ss, st, sw, ref sss2, ref sst2);

                                LevelOfDetail.PassWithNext(m_Rdp, ref sss, ref sst, ref sss2, ref sst2, dinc, primTile, ref tile1, ref tile3, ref preloadfrac, i);

                                TU.PipelineCycle(ref GS.NextTexelColor, ref GS.NextTexelColor, sss, sst, (uint)tile1, 0);
                                TU.PipelineCycle(ref GS.NextTexel1Color, ref GS.NextTexelColor, sss2, sst2, (uint)tile3, 1);
                            }
                        }

                        ZCorrect(offx, offy, ref sz, curpixel_cvg);

                        m_Rdp.RdpCombiner.Pass_2Cycle_Cycle1(adith, ref curpixel_cvg);

                        m_Rdp.N64Framebuffer.ReadCycle2(curpixel, ref curpixel_memcvg);

                        bool enableWrite = ZBuffer.ZCompare(m_Rdp, (uint)zbcur, (uint)sz, dzpix, dzpixenc, ref enableBlend, ref preWrap, ref curpixel_cvg, curpixel_memcvg);
                        if (enableWrite)
                        {
                            enableWrite &= m_Rdp.RdpBlender.Pass_2Cycle_Cycle0(curpixel_cvg != 0, curpixel_cvbit);
                        }
                        else
                        {
                            GS.MemoryColor.SetColor(GS.PreMemoryColor);
                        }

                        x += xinc;

                        r += drinc;
                        g += dginc;
                        b += dbinc;
                        a += dainc;

                        sr = r >> 14;
                        sg = g >> 14;
                        sb = b >> 14;
                        sa = a >> 14;

                        Coverage.LookupMaskDerivatives(
                            j < length ? GS.ConverageBuffer[x] : (byte)0,
                            ref offx,
                            ref offy,
                            ref nextpixel_cvg,
                            ref curpixel_cvbit
                        );

                        RgbaCorrect(offx, offy, sr, sg, sb, sa, nextpixel_cvg);

                        if (enableTexture)
                        {
                            if (enableTexelNext)
                            {
                                GS.LodFraction = preloadfrac;
                                GS.Texel0Color.SetColor(GS.NextTexelColor);
                                GS.Texel1Color.SetColor(nextTexel1Color);
                            }
                            else
                            {
                                s += dsinc;
                                t += dtinc;
                                w += dwinc;
                                ss = s >> 16;
                                st = t >> 16;
                                sw = w >> 16;

                                m_Rdp.TextureCoordDivision(
                                    ss,
                                    st,
                                    sw,
                                    ref sss,
                                    ref sst
                                );

                                if (enableTexel1)
                                {
                                    LevelOfDetail.Pass(
                                        m_Rdp,
                                        ref sss,
                                        ref sst,
                                        new TextureCoordinate(s, t, w),
                                        dinc,
                                        primTile,
                                        ref tile1,
                                        ref tile2,
                                        ref GS.LodFraction
                                    );

                                    TU.PipelineCycle(ref GS.Texel0Color.color, ref GS.Texel0Color.color, sss, sst, (uint)tile1, 0);
                                    TU.PipelineCycle(ref GS.Texel1Color.color, ref GS.Texel0Color.color, sss, sst, (uint)tile2, 1);
                                }
                                else
                                {
                                    LevelOfDetail.Pass_NoTexel1(
                                        m_Rdp,
                                        ref sss,
                                        ref sst,
                                        new TextureCoordinate(s, t, w),
                                        dinc,
                                        primTile,
                                        ref tile1
                                    );
                                }
                            }
                        }

                        m_Rdp.RdpCombiner.Pass_2Cycle_Cycle0(adith, (int)nextpixel_cvg, ref acalpha);

                        if (enableWrite)
                        {
                            enableWrite &= m_Rdp.RdpBlender.AlphaCompare((int)acalpha);

                            if (enableWrite)
                            {
                                m_Rdp.RdpBlender.FramebufferPass(1, ref fir, ref fig, ref fib, cdith, enableBlend, preWrap);
                                m_Rdp.N64Framebuffer.Write(curpixel, fir, fig, fib, enableBlend, curpixel_cvg, curpixel_memcvg);

                                if (GS.OtherModes.EnableZUpdate)
                                {
                                    ZBuffer.ZStore(m_Rdp.RdramMemory, (uint)zbcur, (uint)sz, dzpixenc);
                                }
                            }
                        }

                        if (GS.OtherModes.Flags.GetDitherLevel < 2)
                        {
                            Dither.GetDitherNoise(GS, x, i, ref cdith, ref adith);
                        }

                        curpixel_cvg = nextpixel_cvg;
                        z += dzinc;
                        curpixel += (uint)xinc;
                        zbcur += xinc;
                    }
                }
            }
        }

        public void RenderSpansFill(int start, int end, bool flip)
        {
            if ((ImageSizeType)GS.FramebufferSize == ImageSizeType.SizeInBytes_4)
            {
                GS.PipelineCrashed = true;
                return;
            }

            //Log.Debug("Span Fill: start={0:X8} end={1:X8}", start, end);

            bool fastKillBits = GS.OtherModes.EnableImageRead || GS.OtherModes.EnableZCompare;
            bool slowKillBits = GS.OtherModes.EnableZUpdate && !GS.OtherModes.ZSourceSelection && !fastKillBits;

            int xinc = flip ? 1 : -1;

            for (int i = start; i <= end; i++)
            {
                int xstart = GS.SpanBuffer[i].lx;
                int xendsc = GS.SpanBuffer[i].rx;
                int x = xendsc;
                int curpixel = (GS.FramebufferWidth * i) + x;
                int length = flip ? xstart - xendsc : xendsc - xstart;

                if (GS.SpanBuffer[i].ValidLine)
                {
                    if (fastKillBits && length >= 0)
                    {
                        GS.PipelineCrashed = true;
                        return;
                    }

                    for (int j = 0; j <= length; j++)
                    {
                        switch ((ImageSizeType)GS.FramebufferSize)
                        {
                            case ImageSizeType.SizeInBytes_4:
                                {
                                    m_Rdp.N64Framebuffer.Fill_4((uint)curpixel);
                                    break;
                                }

                            case ImageSizeType.SizeInBytes_8:
                                {
                                    m_Rdp.N64Framebuffer.Fill_8((uint)curpixel);
                                    break;
                                }

                            case ImageSizeType.SizeInBytes_16:
                                {
                                    m_Rdp.N64Framebuffer.Fill_16((uint)curpixel);
                                    break;
                                }

                            case ImageSizeType.SizeInBytes_32:
                                {
                                    m_Rdp.N64Framebuffer.Fill_32((uint)curpixel);
                                    break;
                                }
                        }

                        x += xinc;
                        curpixel += xinc;
                    }

                    if (slowKillBits && length >= 0)
                    {
                        GS.PipelineCrashed = true;
                        return;
                    }
                }
            }
        }

        public void RenderSpansCopy(int start, int end, int tileIndex, bool flip)
        {
            var fbSize = (ImageSizeType)GS.FramebufferSize;

            if (fbSize == ImageSizeType.SizeInBytes_32)
            {
                GS.PipelineCrashed = true;
                return;
            }

            int tile1 = tileIndex;
            int primTile = tileIndex;

            int dsinc;
            int dtinc;
            int dwinc;

            int xinc;

            if (flip)
            {
                dsinc = GS.SpansDs;
                dtinc = GS.SpansDt;
                dwinc = GS.SpansDw;
                xinc = 1;
            }
            else
            {
                dsinc = -GS.SpansDs;
                dtinc = -GS.SpansDt;
                dwinc = -GS.SpansDw;
                xinc = -1;
            }

            var dinc = new TextureCoordinate(dsinc, dtinc, dwinc);

            int sss = 0;
            int sst = 0;

            uint hidword = 0;
            uint lowdword = 0;

            int fbptrAdvance = 8 * xinc;

            int fbadvance = (fbSize == ImageSizeType.SizeInBytes_4) ? 8 : 16 >> GS.FramebufferSize;
            int byteperpixel = (fbSize == ImageSizeType.SizeInBytes_4) ? 1 : (1 << (GS.FramebufferSize - 1));

            for (int i = start; i <= end; i++)
            {
                if (GS.SpanBuffer[i].ValidLine)
                {
                    int s = GS.SpanBuffer[i].s;
                    int t = GS.SpanBuffer[i].t;
                    int w = GS.SpanBuffer[i].w;

                    int xstart = GS.SpanBuffer[i].lx;
                    int xendsc = GS.SpanBuffer[i].rx;

                    int fbIndex = (GS.FramebufferWidth * i) + xendsc;
                    int fbptr = (int)GS.FramebufferAddress + Utils.PixelsToBytesSpecial4(fbIndex, GS.FramebufferSize);
                    int fbendptr = (int)(GS.FramebufferAddress + Utils.PixelsToBytesSpecial4((GS.FramebufferWidth * i) + xstart, GS.FramebufferSize));
                    int length = flip ? xstart - xendsc : xendsc - xstart;

                    var tcoord = new TextureCoordinate(s, t, w);

                    for (int j = 0; j <= length; j += fbadvance)
                    {
                        int ss = tcoord.s >> 16;
                        int st = tcoord.t >> 16;
                        int sw = tcoord.w >> 16;

                        m_Rdp.TextureCoordDivision(ss, st, sw, ref sss, ref sst);

                        LevelOfDetail.Copy(m_Rdp, ref sss, ref sst, tcoord, dinc, primTile, ref tile1);

                        FetchQwordCopy(ref hidword, ref lowdword, sss, sst, tile1);

                        ulong copyqword;

                        if (fbSize == ImageSizeType.SizeInBytes_16 ||
                            fbSize == ImageSizeType.SizeInBytes_8)
                        {
                            copyqword = ((ulong)hidword << 32) | lowdword;
                        }
                        else
                        {
                            copyqword = 0;
                        }

                        int alphamask;

                        if (!GS.OtherModes.EnableAlphaCompare)
                        {
                            alphamask = 0xFF;
                        }
                        else if (fbSize == ImageSizeType.SizeInBytes_16)
                        {
                            alphamask = 0;
                            alphamask |= (copyqword >> 48).IsTrue(1) ? 0xC0 : 0;
                            alphamask |= (copyqword >> 32).IsTrue(1) ? 0x30 : 0;
                            alphamask |= (copyqword >> 16).IsTrue(1) ? 0xC : 0;
                            alphamask |= copyqword.IsTrue(1) ? 0x3 : 0;
                        }
                        else if (fbSize == ImageSizeType.SizeInBytes_8)
                        {
                            alphamask = 0;

                            int threshold = GS.OtherModes.EnableDitherAlpha ?
                                (Utils.IRand(ref GS.RandomSeed) & 0xFF) :
                                GS.BlendColor.A;

                            if (GS.OtherModes.EnableDitherAlpha)
                            {
                                int currthreshold = threshold;
                                alphamask |= ((int)(copyqword >> 24) & 0xFF) >= currthreshold ? 0xC0 : 0;
                                currthreshold = ((threshold & 3) << 6) | (threshold >> 2);
                                alphamask |= ((int)(copyqword >> 16) & 0xFF) >= currthreshold ? 0x30 : 0;
                                currthreshold = ((threshold & 0xF) << 4) | (threshold >> 4);
                                alphamask |= ((int)(copyqword >> 8) & 0xFF) >= currthreshold ? 0xC : 0;
                                currthreshold = ((threshold & 0x3F) << 2) | (threshold >> 6);
                                alphamask |= (int)(copyqword & 0xFF) >= currthreshold ? 0x3 : 0;
                            }
                            else
                            {
                                /// XXX: Decided to leave the alphamask at 0 if the entire copyqword is 0 too
                                if (copyqword != 0) {
                                    alphamask |= ((int)(copyqword >> 24) & 0xFF) >= threshold ? 0xC0 : 0;
                                    alphamask |= ((int)(copyqword >> 16) & 0xFF) >= threshold ? 0x30 : 0;
                                    alphamask |= ((int)(copyqword >> 8) & 0xFF) >= threshold ? 0xC : 0;
                                    alphamask |= (int)(copyqword & 0xFF) >= threshold ? 0x3 : 0;
                                }
                            }
                        }
                        else
                        {
                            alphamask = 0;
                        }

                        int copymask = flip ?
                        fbendptr - fbptr + byteperpixel :
                        fbptr - fbendptr + byteperpixel;

                        if (copymask > 8)
                        {
                            copymask = 8;
                        }

                        // XXX: Subtract 1 fixes alignment
                        int tempdword = fbptr - 1;

                        int k = 7;
                        while (copymask > 0)
                        {
                            uint tempbyte = (uint)((copyqword >> (k << 3)) & 0xFF);

                            if (alphamask.IsTrue(1 << k))
                            {
                                m_Rdp.RdramMemory.WritePair8(
                                    (uint)tempdword,
                                    (byte)tempbyte,
                                    (byte)(tempbyte.IsTrue(1) ? 3 : 0));
                            }

                            k--;
                            tempdword += xinc;
                            copymask--;
                        }

                        tcoord += dinc;
                        fbptr += fbptrAdvance;
                    }
                }
            }
        }
    }
}