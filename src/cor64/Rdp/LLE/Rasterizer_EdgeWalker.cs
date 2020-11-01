using System;
using cor64.Rdp.Commands;

namespace cor64.Rdp.LLE
{
    public partial class Rasterizer
    {
        public void EdgewalkerForPrims(RdpCommand command)
        {
            if (m_Disable) {
                return;
            }

            var cmd = (Triangle)command;

            int j = 0;
            int xleft = 0, xright = 0, xleft_inc = 0, xright_inc = 0;
            int r = 0, g = 0, b = 0, a = 0, z = 0, s = 0, t = 0, w = 0;
            int drdx = 0, dgdx = 0, dbdx = 0, dadx = 0, dzdx = 0, dsdx = 0, dtdx = 0, dwdx = 0;
            int drdy = 0, dgdy = 0, dbdy = 0, dady = 0, dzdy = 0, dsdy = 0, dtdy = 0, dwdy = 0;
            int drde = 0, dgde = 0, dbde = 0, dade = 0, dzde = 0, dsde = 0, dtde = 0, dwde = 0;

            if (GS.OtherModes.Flags.IsDerivativesStale)
            {
                m_Rdp.DeduceDerivatives();
                GS.OtherModes.Flags.IsDerivativesStale = false;
            }

            bool flip = cmd.IsRightMajor;
            GS.MaxLevel = cmd.MipMapCount;
            int tilenum = cmd.Tile;

            int yl = Utils.Sign(cmd.YL, 14);
            int ym = Utils.Sign(cmd.YM, 14);
            int yh = Utils.Sign(cmd.YH, 14);

            int xl = Utils.Sign(cmd.XL, 28);
            int xh = Utils.Sign(cmd.XH, 28);
            int xm = Utils.Sign(cmd.XM, 28);

            int dxldy = Utils.Sign(cmd.DxLDy, 30);
            int dxhdy = Utils.Sign(cmd.DxHDy, 30);
            int dxmdy = Utils.Sign(cmd.DxMDy, 30);

            static int Combine(int integer, int fraction) {
                return (integer << 16) | fraction;
            }

            if (cmd.IsShade) {
                var sh = cmd.Shade;

                r = Combine(sh.RedI, sh.RedF);
                g = Combine(sh.GreenI, sh.GreenF);
                b = Combine(sh.BlueI, sh.BlueF);
                a = Combine(sh.AlphaI, sh.AlphaF);

                drdx = Combine(sh.DrDxI, sh.DrDxF);
                dgdx = Combine(sh.DgDxI, sh.DgDxF);
                dbdx = Combine(sh.DbDxI, sh.DbDxF);
                dadx = Combine(sh.DaDxI, sh.DaDxF);

                drde = Combine(sh.DrDeI, sh.DrDeF);
                dgde = Combine(sh.DgDeI, sh.DgDeF);
                dbde = Combine(sh.DbDeI, sh.DbDeF);
                dade = Combine(sh.DaDeI, sh.DaDeF);

                drdy = Combine(sh.DrDyI, sh.DrDyF);
                dgdy = Combine(sh.DgDyI, sh.DgDyF);
                dbdy = Combine(sh.DbDyI, sh.DbDyF);
                dady = Combine(sh.DaDyI, sh.DaDyF);
            }

            if (cmd.UseTexture) {
                var tx = cmd.Texture;

                s = Combine(tx.SCoordI, tx.SCoordF);
                t = Combine(tx.TCoordI, tx.TCoordF);
                w = Combine(tx.WCoordI, tx.WCoordF);

                dsdx = Combine(tx.DsDxI, tx.DsDxF);
                dtdx = Combine(tx.DtDxI, tx.DtDxF);
                dwdx = Combine(tx.DwDxI, tx.DwDxF);

                dsde = Combine(tx.DsDeI, tx.DsDeF);
                dtde = Combine(tx.DtDeI, tx.DtDeF);
                dwde = Combine(tx.DwDeI, tx.DwDeF);

                dsdy = Combine(tx.DsDyI, tx.DsDyF);
                dtdy = Combine(tx.DtDyI, tx.DtDyF);
                dwdy = Combine(tx.DwDyI, tx.DwDyF);
            }

            if (cmd.UseZ) {
                var dp = cmd.Depth;

                z = Combine(dp.DepthI, dp.DepthF);

                dzdx = Combine(dp.DzDxI, dp.DzDxF);
                dzde = Combine(dp.DzDeI, dp.DzDeF);
                dzdy = Combine(dp.DzDyI, dp.DzDyF);
            }

            GS.SpansDs = dsdx & ~0x1F;
            GS.SpansDt = dtdx & ~0x1F;
            GS.SpansDw = dwdx & ~0x1F;
            GS.SpansDr = drdx & ~0x1F;
            GS.SpansDg = dgdx & ~0x1F;
            GS.SpansDb = dbdx & ~0x1F;
            GS.SpansDa = dadx & ~0x1F;
            GS.SpansDz = dzdx;

            GS.SpansDrDy = drdy >> 14;
            GS.SpansDgDy = dgdy >> 14;
            GS.SpansDbDy = dbdy >> 14;
            GS.SpansDaDy = dady >> 14;
            GS.SpansDzDy = dzdy >> 10;
            GS.SpansDrDy = Utils.Sign(GS.SpansDrDy, 13);
            GS.SpansDgDy = Utils.Sign(GS.SpansDgDy, 13);
            GS.SpansDbDy = Utils.Sign(GS.SpansDbDy, 13);
            GS.SpansDaDy = Utils.Sign(GS.SpansDaDy, 13);
            GS.SpansDzDy = Utils.Sign(GS.SpansDzDy, 22);
            GS.SpansCdr = GS.SpansDr >> 14;
            GS.SpansCdr = Utils.Sign(GS.SpansCdr, 13);
            GS.SpansCdg = GS.SpansDg >> 14;
            GS.SpansCdg = Utils.Sign(GS.SpansCdg, 13);
            GS.SpansCdb = GS.SpansDb >> 14;
            GS.SpansCdb = Utils.Sign(GS.SpansCdb, 13);
            GS.SpansCda = GS.SpansDa >> 14;
            GS.SpansCda = Utils.Sign(GS.SpansCda, 13);
            GS.SpansCdz = GS.SpansDz >> 10;
            GS.SpansCdz = Utils.Sign(GS.SpansCdz, 22);

            GS.SpansDsDy = dsdy & ~0x7FFF;
            GS.SpansDtDy = dtdy & ~0x7FFF;
            GS.SpansDwDy = dwdy & ~0x7FFF;

            int dzdy_dz = (dzdy >> 16) & 0xFFFF;
            int dzdx_dz = (dzdx >> 16) & 0xFFFF;

            GS.SpansDzPix =
                (dzdy_dz.IsTrue(0x8000) ? (~dzdy_dz & 0x7FFF) : dzdy_dz) +
                (dzdx_dz.IsTrue(0x8000) ? (~dzdx_dz & 0x7FFF) : dzdx_dz);

            GS.SpansDzPix = NormalizeDzpix(GS.SpansDzPix & 0xFFFF) & 0xFFFF;

            xleft_inc = (dxmdy >> 2) & ~0x1;
            xright_inc = (dxhdy >> 2) & ~0x1;

            xright = xh & ~0x1;
            xleft = xm & ~0x1;

            int dsdiff = 0;
            int dtdiff = 0;
            int dwdiff = 0;
            int drdiff = 0;
            int dgdiff = 0;
            int dbdiff = 0;
            int dadiff = 0;
            int dzdiff = 0;

            bool sign_dxhdy = ((uint)cmd.DxHDy).IsTrue(0x80000000U);

            int dsdeh = 0;
            int dtdeh = 0;
            int dwdeh = 0;
            int drdeh = 0;
            int dgdeh = 0;
            int dbdeh = 0;
            int dadeh = 0;
            int dzdeh = 0;
            int dsdyh = 0;
            int dtdyh = 0;
            int dwdyh = 0;
            int drdyh = 0;
            int dgdyh = 0;
            int dbdyh = 0;
            int dadyh = 0;
            int dzdyh = 0;

            bool do_offset = !(sign_dxhdy ^ flip);

            if (do_offset)
            {
                dsdeh = dsde & ~0x1FF;
                dtdeh = dtde & ~0x1FF;
                dwdeh = dwde & ~0x1FF;
                drdeh = drde & ~0x1FF;
                dgdeh = dgde & ~0x1FF;
                dbdeh = dbde & ~0x1FF;
                dadeh = dade & ~0x1FF;
                dzdeh = dzde & ~0x1FF;
                dsdyh = dsdy & ~0x1FF;
                dtdyh = dtdy & ~0x1FF;
                dwdyh = dwdy & ~0x1FF;
                drdyh = drdy & ~0x1FF;
                dgdyh = dgdy & ~0x1FF;
                dbdyh = dbdy & ~0x1FF;
                dadyh = dady & ~0x1FF;
                dzdyh = dzdy & ~0x1FF;
                dsdiff = dsdeh - (dsdeh >> 2) - dsdyh + (dsdyh >> 2);
                dtdiff = dtdeh - (dtdeh >> 2) - dtdyh + (dtdyh >> 2);
                dwdiff = dwdeh - (dwdeh >> 2) - dwdyh + (dwdyh >> 2);
                drdiff = drdeh - (drdeh >> 2) - drdyh + (drdyh >> 2);
                dgdiff = dgdeh - (dgdeh >> 2) - dgdyh + (dgdyh >> 2);
                dbdiff = dbdeh - (dbdeh >> 2) - dbdyh + (dbdyh >> 2);
                dadiff = dadeh - (dadeh >> 2) - dadyh + (dadyh >> 2);
                dzdiff = dzdeh - (dzdeh >> 2) - dzdyh + (dzdyh >> 2);
            }

            int xfrac = 0;
            int dsdxh = 0;
            int dtdxh = 0;
            int dwdxh = 0;
            int drdxh = 0;
            int dgdxh = 0;
            int dbdxh = 0;
            int dadxh = 0;
            int dzdxh = 0;

            if ((CycleType)GS.OtherModes.CycleType != CycleType.Copy)
            {
                dsdxh = (dsdx >> 8) & ~1;
                dtdxh = (dtdx >> 8) & ~1;
                dwdxh = (dwdx >> 8) & ~1;
                drdxh = (drdx >> 8) & ~1;
                dgdxh = (dgdx >> 8) & ~1;
                dbdxh = (dbdx >> 8) & ~1;
                dadxh = (dadx >> 8) & ~1;
                dzdxh = (dzdx >> 8) & ~1;
            }

            void AdjustAttrPrim()
            {
                GS.SpanBuffer[j].s = ((s & ~0x1FF) + dsdiff - (xfrac * dsdxh)) & ~0x3FF;
                GS.SpanBuffer[j].t = ((t & ~0x1FF) + dtdiff - (xfrac * dtdxh)) & ~0x3FF;
                GS.SpanBuffer[j].w = ((w & ~0x1FF) + dwdiff - (xfrac * dwdxh)) & ~0x3FF;
                GS.SpanBuffer[j].r = ((r & ~0x1FF) + drdiff - (xfrac * drdxh)) & ~0x3FF;
                GS.SpanBuffer[j].g = ((g & ~0x1FF) + dgdiff - (xfrac * dgdxh)) & ~0x3FF;
                GS.SpanBuffer[j].b = ((b & ~0x1FF) + dbdiff - (xfrac * dbdxh)) & ~0x3FF;
                GS.SpanBuffer[j].a = ((a & ~0x1FF) + dadiff - (xfrac * dadxh)) & ~0x3FF;
                GS.SpanBuffer[j].z = ((z & ~0x1FF) + dzdiff - (xfrac * dzdxh)) & ~0x3FF;
            }

            void AddValuesPrim()
            {
                s += dsde;
                t += dtde;
                w += dwde;
                r += drde;
                g += dgde;
                b += dbde;
                a += dade;
                z += dzde;
            }

            int maxxmx = 0;
            int minxmx = 0;
            int maxxhx = 0;
            int minxhx = 0;

            int spix = 0;
            int ycur = yh & ~3;
            int ldflag = (sign_dxhdy ^ flip) ? 0 : 3;
            bool invaly = true;
            int xrsc = 0;
            int xlsc = 0;

            bool temp = false;

            if (yl.IsTrue(0x2000))
            {
                temp = true;
            }
            else if (yl.IsTrue(0x1000))
            {
                temp = false;
            }
            else
            {
                temp = (yl & 0xFFF) < GS.Clip.yl;
            }

            int yllimit = temp ? yl : GS.Clip.yl;

            int ylfar = yllimit | 3;

            if ((yl >> 2) > (ylfar >> 2))
            {
                ylfar += 4;
            }
            else if ((yllimit >> 2) >= 0 && (yllimit >> 2) < 1023)
            {
                GS.SpanBuffer[(yllimit >> 2) + 1].ValidLine = false;
            }

            if (yh.IsTrue(0x2000))
            {
                temp = false;
            }
            else if (yh.IsTrue(0x1000))
            {
                temp = true;
            }
            else
            {
                temp = yh >= GS.Clip.yh;
            }

            int yhlimit = temp ? yh : GS.Clip.yh;

            int yhclose = yhlimit & ~3;

            int clipxlshift = GS.Clip.xl << 1;
            int clipxhshift = GS.Clip.xh << 1;
            bool allover = true;
            bool allunder = true;
            bool curover = false;
            bool curunder = false;
            bool allinval = true;
            bool curcross = false;

            xfrac = (xright >> 8) & 0xFF;

            if (flip)
            {
                for (int k = ycur; k <= ylfar; k++)
                {
                    if (k == ym)
                    {
                        xleft = xl & ~1;
                        xleft_inc = (dxldy >> 2) & ~1;
                    }

                    spix = k & 3;

                    if (k >= yhclose)
                    {
                        invaly = k < yhlimit || k >= yllimit;

                        j = k >> 2;

                        if (spix == 0)
                        {
                            maxxmx = 0;
                            minxhx = 0xFFF;
                            allover = allunder = true;
                            allinval = true;
                        }

                        int stickybit = (((xright >> 1) & 0x1FFF) > 0) ? 1 : 0;

                        xrsc = ((xright >> 13) & 0x1FFE) | stickybit;

                        curunder = xright.IsTrue(0x8000000) || (xrsc < clipxhshift && !xright.IsTrue(0x4000000));

                        xrsc = curunder ? clipxhshift : (((xright >> 13) & 0x3FFE) | stickybit);
                        curover = xrsc.IsTrue(0x2000) || (xrsc & 0x1FFF) >= clipxlshift;
                        xrsc = curover ? clipxlshift : xrsc;

                        unsafe
                        {
                            GS.SpanBuffer[j].majorx[spix] = xrsc & 0x1FFF;
                        }

                        allover &= curover;
                        allunder &= curunder;

                        stickybit = (((xleft >> 1) & 0x1FFF) > 0) ? 1 : 0;

                        xlsc = ((xleft >> 13) & 0x1FFE) | stickybit;
                        curunder = xleft.IsTrue(0x8000000) || (xlsc < clipxhshift && !xleft.IsTrue(0x4000000));
                        xlsc = curunder ? clipxhshift : (((xleft >> 13) & 0x3FFE) | stickybit);
                        curover = xlsc.IsTrue(0x2000) || (xlsc & 0x1FFF) >= clipxlshift;
                        xlsc = curover ? clipxlshift : xlsc;

                        unsafe
                        {
                            GS.SpanBuffer[j].minorx[spix] = xlsc & 0x1FFF;
                        }

                        allover &= curover;
                        allunder &= curunder;

                        curcross = ((xleft ^ (1 << 27)) & (0x3FFF << 14)) < ((xright ^ (1 << 27)) & (0x3FFF << 14));

                        invaly |= curcross;

                        unsafe
                        {
                            GS.SpanBuffer[j].InvalidYScan[spix] = invaly;
                        }

                        allinval &= invaly;

                        if (!invaly)
                        {
                            maxxmx = (((xlsc >> 3) & 0xFFF) > maxxmx) ? (xlsc >> 3) & 0xFFF : maxxmx;
                            minxhx = (((xrsc >> 3) & 0xFFF) < minxhx) ? (xrsc >> 3) & 0xFFF : minxhx;
                        }

                        if (spix == ldflag)
                        {
                            GS.SpanBuffer[j].unscrx = Utils.Sign(xright >> 16, 12);
                            xfrac = (xright >> 8) & 0xFF;
                            AdjustAttrPrim();
                        }

                        if (spix == 3)
                        {
                            GS.SpanBuffer[j].lx = maxxmx;
                            GS.SpanBuffer[j].rx = minxhx;
                            var scfield = GS.ScField.IsTrue();

                            GS.SpanBuffer[j].ValidLine =
                                !allinval &&
                                !allover &&
                                !allunder &&
                                (!scfield || (scfield && !(GS.ScKeepOdd.IsTrue() ^ j.IsTrue(1)))) &&
                                (!GS.Stride.IsTrue() || j % GS.Stride == GS.WorkerOffset);
                        }
                    }

                    if (spix == 3)
                    {
                        AddValuesPrim();
                    }

                    xleft += xleft_inc;
                    xright += xright_inc;
                }
            }
            else
            {
                for (int k = ycur; k <= ylfar; k++)
                {
                    if (k == ym)
                    {
                        xleft = xl & ~1;
                        xleft_inc = (dxldy >> 2) & ~1;
                    }

                    spix = k & 3;

                    if (k >= yhclose)
                    {
                        invaly = k < yhlimit || k >= yllimit;
                        j = k >> 2;

                        if (spix == 0)
                        {
                            maxxhx = 0;
                            minxmx = 0xFFF;
                            allover = allunder = true;
                            allinval = true;
                        }

                        int stickybit = (((xright >> 1) & 0x1FFF) > 0) ? 1 : 0;

                        xrsc = ((xright >> 13) & 0x1FFE) | stickybit;
                        curunder = xright.IsTrue(0x8000000) || (xrsc < clipxhshift && !xright.IsTrue(0x4000000));
                        xrsc = curunder ? clipxhshift : (((xright >> 13) & 0x3FFE) | stickybit);
                        curover = xrsc.IsTrue(0x2000) || (xrsc & 0x1FFF) >= clipxlshift;
                        xrsc = curover ? clipxlshift : xrsc;

                        unsafe
                        {
                            GS.SpanBuffer[j].majorx[spix] = xrsc & 0x1FFF;
                        }

                        allover &= curover;
                        allunder &= curunder;

                        stickybit = (((xleft >> 1) & 0x1FFF) > 0) ? 1 : 0;

                        xlsc = ((xleft >> 13) & 0x1FFE) | stickybit;
                        curunder = xleft.IsTrue(0x8000000) || (xlsc < clipxhshift && !xleft.IsTrue(0x4000000));
                        xlsc = curunder ? clipxhshift : (((xleft >> 13) & 0x3FFE) | stickybit);
                        curover = xlsc.IsTrue(0x2000) || (xlsc & 0x1FFF) >= clipxlshift;
                        xlsc = curover ? clipxlshift : xlsc;

                        unsafe
                        {
                            GS.SpanBuffer[j].minorx[spix] = xlsc & 0x1FFF;
                        }

                        allover &= curover;
                        allunder &= curunder;

                        curcross = ((xright ^ (1 << 27)) & (0x3FFF << 14)) < ((xleft ^ (1 << 27)) & (0x3FFF << 14));

                        invaly |= curcross;

                        unsafe
                        {
                            GS.SpanBuffer[j].InvalidYScan[spix] = invaly;
                        }

                        allinval &= invaly;

                        if (!invaly)
                        {
                            minxmx = (((xlsc >> 3) & 0xFFF) < minxmx) ? (xlsc >> 3) & 0xFFF : minxmx;
                            maxxhx = (((xrsc >> 3) & 0xFFF) > maxxhx) ? (xrsc >> 3) & 0xFFF : maxxhx;
                        }

                        if (spix == ldflag)
                        {
                            GS.SpanBuffer[j].unscrx = Utils.Sign(xright >> 16, 12);
                            xfrac = (xright >> 8) & 0xFF;
                            AdjustAttrPrim();
                        }

                        if (spix == 3)
                        {
                            GS.SpanBuffer[j].lx = minxmx;
                            GS.SpanBuffer[j].rx = maxxhx;
                            var scfield = GS.ScField.IsTrue();

                            GS.SpanBuffer[j].ValidLine =
                                !allinval &&
                                !allover &&
                                !allunder &&
                                (!scfield || (scfield && !(GS.ScKeepOdd.IsTrue() ^ j.IsTrue(1)))) &&
                                (!GS.Stride.IsTrue() || j % GS.Stride == GS.WorkerOffset);
                        }

                    }

                    if (spix == 3)
                    {
                        AddValuesPrim();
                    }

                    xleft += xleft_inc;
                    xright += xright_inc;
                }
            }

            switch ((CycleType)GS.OtherModes.CycleType)
            {
                case CycleType.Type1:
                    {
                        switch (GS.Cycle1RenderMode)
                        {
                            case 0:  RenderSpans_1Cycle(true, true, yhlimit >> 2, yllimit >> 2, tilenum, flip); break;
                            case 1:  RenderSpans_1Cycle(true, false, yhlimit >> 2, yllimit >> 2, tilenum, flip); break;
                            default: RenderSpans_1Cycle(false, false, yhlimit >> 2, yllimit >> 2, tilenum, flip); break;
                        }
                        break;
                    }

                case CycleType.Type2:
                    {
                        switch (GS.Cycle2RenderMode)
                        {
                            case 0:  RenderSpans_2Cycle(true, true, true, yhlimit >> 2, yllimit >> 2, tilenum, flip); break;
                            case 1:  RenderSpans_2Cycle(true, true, false, yhlimit >> 2, yllimit >> 2, tilenum, flip); break;
                            case 2:  RenderSpans_2Cycle(true, false, true, yhlimit >> 2, yllimit >> 2, tilenum, flip); break;
                            default: RenderSpans_2Cycle(false, false, false, yhlimit >> 2, yllimit >> 2, tilenum, flip); break;
                        }
                        break;
                    }

                case CycleType.Copy: RenderSpansCopy(yhlimit >> 2, yllimit >> 2, tilenum, flip); break;
                case CycleType.Fill: RenderSpansFill(yhlimit >> 2, yllimit >> 2, flip); break;

                default: throw new RdpException("Invalid cycle mode");
            }
        }
    }
}