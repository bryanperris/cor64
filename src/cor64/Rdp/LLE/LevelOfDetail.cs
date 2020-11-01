namespace cor64.Rdp.LLE
{
    public static class LevelOfDetail
    {
        private static readonly int[] s_Log2Table = new int[256];

        static LevelOfDetail()
        {
            for (int i = 2; i < s_Log2Table.Length; i++)
            {
                for (int k = 7; k > 0; k--)
                {
                    if ((i >> k).IsTrue(1))
                    {
                        s_Log2Table[i] = k;
                        break;
                    }
                }
            }
        }

        public static void Convert4X17To15(int s, int nextS, int t, int nextT, int previous, ref int level)
        {
            int dels = Utils.Sign(nextS, 17) - Utils.Sign(s, 17);
            int delt = Utils.Sign(nextT, 17) - Utils.Sign(t, 17);

            if (dels.IsTrue(0x20000))
            {
                dels = ~dels & 0x1FFFF;
            }

            if (delt.IsTrue(0x20000))
            {
                delt = ~delt & 0x1FFFF;
            }

            dels = (dels > delt) ? dels : delt;
            dels = (previous > dels) ? previous : dels;

            level = dels & 0x7FFF;

            if (dels.IsTrue(0x1C000))
            {
                level |= 0x4000;
            }
        }

        public static void Clamp(ref int sss, ref int sst)
        {
            static int C(int coord)
            {
                if (!coord.IsTrue(0x40000))
                {
                    if (!coord.IsTrue(0x20000))
                    {
                        int tempanded = coord & 0x18000;

                        if (tempanded != 0x8000)
                        {
                            return tempanded != 0x10000 ? coord & 0xFFFF : 0x8000;
                        }
                        else
                        {
                            return 0x7FFF;
                        }
                    }
                    else
                    {
                        return 0x8000;
                    }
                }
                else
                {
                    return 0x7FFF;
                }
            }

            sss = C(sss);
            sst = C(sst);
        }

        public static void Signals(State state, bool clamped, int level, ref int lodTile, ref bool magnify, ref bool distant, ref int levelFraction)
        {
            if (level.IsTrue(0x4000) || clamped)
            {
                magnify = false;
                lodTile = 0;
                distant = true;
                levelFraction = 0xFF;
            }
            else if (level < state.MinLevel)
            {
                magnify = true;
                lodTile = 0;
                distant = state.MaxLevel == 0;

                if (!state.OtherModes.EnableSharpenTextures && !state.OtherModes.EnableDetailTextures)
                {
                    levelFraction = distant ? 0xFF : 0;
                }
                else
                {
                    levelFraction = state.MinLevel << 3;

                    if (state.OtherModes.EnableSharpenTextures)
                    {
                        levelFraction |= 0x100;
                    }
                }
            }
            else if (level < 32)
            {
                magnify = true;
                lodTile = 0;
                distant = state.MaxLevel == 0;

                if (!state.OtherModes.EnableSharpenTextures && !state.OtherModes.EnableDetailTextures)
                {
                    levelFraction = distant ? 0xFF : 0;
                }
                else
                {
                    levelFraction = level << 3;

                    if (state.OtherModes.EnableSharpenTextures)
                    {
                        levelFraction |= 0x100;
                    }
                }
            }
            else
            {
                magnify = false;

                lodTile = s_Log2Table[(level >> 5) & 0xFF];

                if (state.MaxLevel != 0)
                {
                    distant = level.IsTrue(0x6000) || lodTile >= state.MaxLevel;
                }
                else
                {
                    distant = true;
                }

                if (!state.OtherModes.EnableSharpenTextures && !state.OtherModes.EnableDetailTextures && distant) {
                    levelFraction = 0xFF;
                }
                else {
                    levelFraction = ((level << 3) >> lodTile) & 0xFF;
                }
            }
        }

        public static void Pass(MadCatRdp rdp, ref int sss, ref int sst, TextureCoordinate tcoord, TextureCoordinate dinc, int prim_tile, ref int t1, ref int t2, ref int lf)
        {
            int inits = sss;
            int initt = sst;
            int lod = 0;
            int lodTile = 0;
            bool magnify = false;
            bool distant = false;
            State state = rdp.GraphicsState;

            Clamp(ref sss, ref sst);

            if (state.Pipeline_UseLod)
            {
                int nextsw = (tcoord.w + dinc.w) >> 16;
                int nexts = (tcoord.s + dinc.s) >> 16;
                int nextt = (tcoord.t + dinc.t) >> 16;
                int nextys = (tcoord.s + state.SpansDsDy) >> 16;
                int nextyt = (tcoord.t + state.SpansDtDy) >> 16;
                int nextysw = (tcoord.w + state.SpansDwDy) >> 16;

                rdp.TextureCoordDivision(nexts, nextt, nextsw, ref nexts, ref nextt);
                rdp.TextureCoordDivision(nextys, nextyt, nextysw, ref nextys, ref nextyt);

                bool lodclamp = initt.IsTrue(0x60000) || nextt.IsTrue(0x60000) || inits.IsTrue(0x60000) || nexts.IsTrue(0x60000) || nextys.IsTrue(0x60000) || nextyt.IsTrue(0x60000);

                if (!lodclamp)
                {
                    Convert4X17To15(inits, nexts, initt, nextt, 0, ref lod);
                    Convert4X17To15(inits, nextys, initt, nextyt, lod, ref lod);
                }

                Signals(state, lodclamp, lod, ref lodTile, ref magnify, ref distant, ref lf);

                if (state.OtherModes.EnableTextureLevelOfDetail)
                {
                    if (distant)
                    {
                        lodTile = state.MaxLevel;
                    }

                    if (!state.OtherModes.EnableDetailTextures)
                    {
                        t1 = prim_tile + lodTile & 7;
                        if (!distant || (!state.OtherModes.EnableSharpenTextures && magnify))
                        {
                            t2 = (t1 + 1) & 7;
                        }
                        else
                        {
                            t2 = t1;
                        }
                    }
                    else
                    {
                        if (!magnify)
                        {
                            t1 = (prim_tile + lodTile + 1) & 7;
                        }
                        else
                        {
                            t1 = (prim_tile + lodTile) & 7;
                        }

                        if (!distant && !magnify)
                        {
                            t2 = (prim_tile + lodTile + 2) & 7;
                        }
                        else
                        {
                            t2 = (prim_tile + lodTile + 1) & 7;
                        }
                    }
                }
            }
        }

        public static void PassWithNext(MadCatRdp rdp, ref int sss, ref int sst, ref int sss2, ref int sst2, TextureCoordinate dinc, int prim_tile, ref int t1, ref int t2, ref int lf, int scanline)
        {
            int inits2 = sss2;
            int initt2 = sst2;
            int lodTile = 0;
            bool magnify = false;
            bool distant = false;
            int lod = 0;
            var state = rdp.GraphicsState;

            Clamp(ref sss, ref sst);
            Clamp(ref sss2, ref sst2);

            if (state.Pipeline_UseLod)
            {
                int nextScan = scanline + 1;

                int nextys = (state.SpanBuffer[nextScan].s + state.SpansDsDy) >> 16;
                int nextyt = (state.SpanBuffer[nextScan].t + state.SpansDtDy) >> 16;
                int nextysw = (state.SpanBuffer[nextScan].w + state.SpansDwDy) >> 16;

                rdp.TextureCoordDivision(nextys, nextyt, nextysw, ref nextys, ref nextyt);

                bool lodclamp = inits2.IsTrue(0x60000) || inits2.IsTrue(0x60000) || nextys.IsTrue(0x60000) || nextyt.IsTrue(0x60000);

                if (!lodclamp)
                {
                    Convert4X17To15(inits2, nextys, initt2, nextyt, 0, ref lod);
                }

                Signals(state, lodclamp, lod, ref lodTile, ref magnify, ref distant, ref lf);

                if (state.OtherModes.EnableTextureLevelOfDetail)
                {
                    if (distant)
                    {
                        lodTile = state.MaxLevel;
                    }

                    if (!state.OtherModes.EnableDetailTextures)
                    {
                        t1 = (prim_tile + lodTile) & 7;
                    }
                    else
                    {
                        if (!magnify)
                        {
                            t1 = prim_tile + lodTile + 1;
                        }
                        else
                        {
                            t1 = prim_tile + lodTile;
                        }

                        t1 &= 7;
                    }

                    int nexts = (state.SpanBuffer[nextScan].s + dinc.s) >> 16;
                    int nextt = (state.SpanBuffer[nextScan].t + dinc.t) >> 16;
                    int nextsw = (state.SpanBuffer[nextScan].w + dinc.w) >> 16;

                    rdp.TextureCoordDivision(nexts, nextt, nextsw, ref nexts, ref nextt);

                    lodclamp = lodclamp || nextt.IsTrue(0x60000) || nexts.IsTrue(0x60000);

                    if (!lodclamp)
                    {
                        Convert4X17To15(inits2, nexts, initt2, nextt, lod, ref lod);
                    }

                    int unused = 0;
                    Signals(state, lodclamp, lod, ref lodTile, ref magnify, ref distant, ref unused);

                    if (distant)
                    {
                        lodTile = state.MaxLevel;
                    }

                    if (!state.OtherModes.EnableDetailTextures)
                    {
                        t2 = (prim_tile + lodTile) & 7;
                    }
                    else
                    {
                        if (!magnify)
                        {
                            t2 = prim_tile + lodTile + 1;
                        }
                        else
                        {
                            t2 = prim_tile + lodTile;
                        }

                        t2 &= 7;
                    }
                }
            }
        }

        public static void Pass_NoTexel1(MadCatRdp rdp, ref int sss, ref int sst, TextureCoordinate tcoord, TextureCoordinate dinc, int prim_tile, ref int t1)
        {
            int inits = sss;
            int initt = sst;
            int lod = 0;
            int lodTile = 0;
            bool magnify = false;
            bool distant = false;
            var state = rdp.GraphicsState;

            Clamp(ref sss, ref sst);

            if (state.Pipeline_UseLod)
            {
                int nextsw = (tcoord.w + dinc.w) >> 16;
                int nexts = (tcoord.s + dinc.s) >> 16;
                int nextt = (tcoord.t + dinc.t) >> 16;
                int nextys = (tcoord.s + state.SpansDsDy) >> 16;
                int nextyt = (tcoord.t + state.SpansDtDy) >> 16;
                int nextysw = (tcoord.w + state.SpansDwDy) >> 16;

                rdp.TextureCoordDivision(nexts, nextt, nextsw, ref nexts, ref nextt);
                rdp.TextureCoordDivision(nextys, nextyt, nextysw, ref nextys, ref nextyt);

                bool lodclamp = initt.IsTrue(0x60000) || nextt.IsTrue(0x60000) || inits.IsTrue(0x60000) || nexts.IsTrue(0x60000) || nextys.IsTrue(0x60000) || nextyt.IsTrue(0x60000);

                if (!lodclamp)
                {
                    Convert4X17To15(inits, nexts, initt, nextt, 0, ref lod);
                    Convert4X17To15(inits, nextys, initt, nextyt, lod, ref lod);
                }

                Signals(state, lodclamp, lod, ref lodTile, ref magnify, ref distant, ref state.LodFraction);

                if (state.OtherModes.EnableTextureLevelOfDetail)
                {
                    if (distant)
                    {
                        lodTile = state.MaxLevel;
                    }

                    if (!distant || (!state.OtherModes.EnableSharpenTextures && magnify))
                    {
                        t1 = (prim_tile + lodTile) & 7;
                    }
                    else
                    {
                        t1 = (prim_tile + lodTile + 1) & 7;
                    }
                }
            }
        }

        public static void PassCurrent_Cycle1(MadCatRdp rdp, ref int sss, ref int sst, int nexts, int nextt, TextureCoordinate tcoord, TextureCoordinate dinc, int scanline, int prim_tile, ref int t1, ref SpanSigs sigs)
        {
            int fars;
            int fart;
            int farsw;
            bool lodclamp;
            int lod = 0;
            int lodTile = 0;
            bool magnify = false;
            bool distant = false;
            var state = rdp.GraphicsState;

            Clamp(ref sss, ref sst);

            if (state.Pipeline_UseLod)
            {
                int nextScan = scanline + 1;

                if (state.SpanBuffer[nextScan].ValidLine)
                {
                    if (!sigs.endSpan || !sigs.longspan)
                    {
                        if (!(sigs.preEndSpan && sigs.longspan) && !(sigs.endSpan && sigs.midSpan))
                        {
                            farsw = (tcoord.w + (dinc.w << 1)) >> 16;
                            fars = (tcoord.s + (dinc.s << 1)) >> 16;
                            fart = (tcoord.t + (dinc.t << 1)) >> 16;
                        }
                        else
                        {
                            farsw = (tcoord.w - dinc.w) >> 16;
                            fars = (tcoord.s - dinc.s) >> 16;
                            fart = (tcoord.t - dinc.t) >> 16;
                        }
                    }
                    else
                    {
                        farsw = (state.SpanBuffer[nextScan].w + dinc.w) >> 16;
                        fars = (state.SpanBuffer[nextScan].s + dinc.s) >> 16;
                        fart = (state.SpanBuffer[nextScan].t + dinc.t) >> 16;
                    }
                }
                else
                {
                    farsw = (tcoord.w + (dinc.w << 1)) >> 16;
                    fars = (tcoord.s + (dinc.s << 1)) >> 16;
                    fart = (tcoord.t + (dinc.t << 1)) >> 16;
                }

                rdp.TextureCoordDivision(fars, fart, farsw, ref fars, ref fart);

                lodclamp = fart.IsTrue(0x60000) || nextt.IsTrue(0x60000) || fars.IsTrue(0x60000) || nexts.IsTrue(0x60000);

                if (!lodclamp)
                {
                    Convert4X17To15(nexts, fars, nextt, fart, 0, ref lod);
                }

                Signals(state, lodclamp, lod, ref lodTile, ref magnify, ref distant, ref state.LodFraction);

                if (state.OtherModes.EnableTextureLevelOfDetail)
                {
                    if (distant)
                    {
                        lodTile = state.MaxLevel;
                    }

                    if (!state.OtherModes.EnableDetailTextures || magnify)
                    {
                        t1 = (prim_tile + lodTile) & 7;
                    }
                    else
                    {
                        t1 = (prim_tile + lodTile + 1) & 7;
                    }
                }
            }
        }

        public static void PassCurrent_Cycle1_Simple(MadCatRdp rdp, ref int sss, ref int sst, TextureCoordinate tcoord, TextureCoordinate dinc, int scanline, int prim_tile, ref int t1, ref SpanSigs sigs)
        {
            int fars;
            int fart;
            int farsw;
            int lod = 0;
            int lodTile = 0;
            bool magnify = false;
            bool distant = false;
            int nextsw;
            int nexts;
            int nextt;
            var state = rdp.GraphicsState;

            Clamp(ref sss, ref sst);

            if (state.Pipeline_UseLod)
            {
                int nextScan = scanline + 1;

                if (state.SpanBuffer[nextScan].ValidLine)
                {
                    if (!sigs.endSpan || !sigs.longspan)
                    {
                        nextsw = (tcoord.w + dinc.w) >> 16;
                        nexts = (tcoord.s + dinc.s) >> 16;
                        nextt = (tcoord.t + dinc.t) >> 16;

                        if (!(sigs.preEndSpan && sigs.longspan) && !(sigs.endSpan && sigs.midSpan))
                        {
                            farsw = (tcoord.w + (dinc.w << 1)) >> 16;
                            fars = (tcoord.s + (dinc.s << 1)) >> 16;
                            fart = (tcoord.t + (dinc.t << 1)) >> 16;
                        }
                        else
                        {
                            farsw = (tcoord.w - dinc.w) >> 16;
                            fars = (tcoord.s - dinc.s) >> 16;
                            fart = (tcoord.t - dinc.t) >> 16;
                        }
                    }
                    else
                    {
                        nextsw = state.SpanBuffer[nextScan].w >> 16;
                        nexts = state.SpanBuffer[nextScan].s >> 16;
                        nextt = state.SpanBuffer[nextScan].t >> 16;
                        farsw = (state.SpanBuffer[nextScan].w + dinc.w) >> 16;
                        fars = (state.SpanBuffer[nextScan].s + dinc.s) >> 16;
                        fart = (state.SpanBuffer[nextScan].t + dinc.t) >> 16;
                    }
                }
                else
                {
                    nextsw = (tcoord.w + dinc.w) >> 16;
                    nexts = (tcoord.s + dinc.s) >> 16;
                    nextt = (tcoord.t + dinc.t) >> 16;
                    farsw = (tcoord.w + (dinc.w << 1)) >> 16;
                    fars = (tcoord.s + (dinc.s << 1)) >> 16;
                    fart = (tcoord.t + (dinc.t << 1)) >> 16;
                }

                rdp.TextureCoordDivision(nexts, nextt, nextsw, ref nexts, ref nextt);
                rdp.TextureCoordDivision(fars, fart, farsw, ref fars, ref fart);

                bool lodclamp = fart.IsTrue(0x60000) || nextt.IsTrue(0x60000) || fars.IsTrue(0x60000) || nexts.IsTrue(0x60000);

                if (!lodclamp)
                {
                    Convert4X17To15(nexts, fars, nextt, fart, 0, ref lod);
                }

                Signals(state, lodclamp, lod, ref lodTile, ref magnify, ref distant, ref state.LodFraction);

                if (state.OtherModes.EnableTextureLevelOfDetail)
                {
                    if (distant)
                    {
                        lodTile = state.MaxLevel;
                    }

                    if (!state.OtherModes.EnableDetailTextures || magnify)
                    {
                        t1 = (prim_tile + lodTile) & 7;
                    }
                    else
                    {
                        t1 = (prim_tile + lodTile + 1) & 7;
                    }
                }
            }
        }

        public static void PassWithNext_1Cycle(MadCatRdp rdp, ref int sss, ref int sst, TextureCoordinate tcoord, TextureCoordinate dinc, int scanline, int prim_tile, ref int t1, ref SpanSigs sigs, ref int prelodfrac)
        {
            int fars;
            int fart;
            int farsw;
            int lod = 0;
            int lodTile = 0;
            bool magnify = false;
            bool distant = false;
            int nextsw;
            int nexts;
            int nextt;
            var state = rdp.GraphicsState;

            Clamp(ref sss, ref sst);

            if (state.Pipeline_UseLod)
            {
                int nextScan = scanline + 1;

                if (state.SpanBuffer[nextScan].ValidLine)
                {
                    if (!sigs.nextSpan)
                    {
                        if (!sigs.endSpan || !sigs.longspan)
                        {
                            nextsw = (tcoord.w + dinc.w) >> 16;
                            nexts = (tcoord.s + dinc.s) >> 16;
                            nextt = (tcoord.t + dinc.t) >> 16;

                            if (!sigs.preEndSpan && sigs.longspan && !(sigs.endSpan && sigs.midSpan))
                            {
                                farsw = (tcoord.w + (dinc.w << 1)) >> 16;
                                fars = (tcoord.s + (dinc.s << 1)) >> 16;
                                fart = (tcoord.t + (dinc.t << 1)) >> 16;
                            }
                            else
                            {
                                farsw = (tcoord.w - dinc.w) >> 16;
                                fars = (tcoord.s - dinc.s) >> 16;
                                fart = (tcoord.t - dinc.t) >> 16;
                            }
                        }
                        else
                        {
                            nextsw = state.SpanBuffer[nextScan].w >> 16;
                            nexts = state.SpanBuffer[nextScan].s >> 16;
                            nextt = state.SpanBuffer[nextScan].t >> 16;
                            farsw = (state.SpanBuffer[nextScan].w + dinc.w) >> 16;
                            fars = (state.SpanBuffer[nextScan].s + dinc.s) >> 16;
                            fart = (state.SpanBuffer[nextScan].t + dinc.t) >> 16;
                            nextt >>= 16;
                            nexts >>= 16;
                            nextsw >>= 16;
                        }
                    }
                    else
                    {
                        if (sigs.longspan)
                        {
                            nextsw = (state.SpanBuffer[nextScan].w + dinc.w) >> 16;
                            nexts = (state.SpanBuffer[nextScan].s + dinc.s) >> 16;
                            nextt = (state.SpanBuffer[nextScan].t + dinc.w) >> 16;
                            farsw = (state.SpanBuffer[nextScan].w + (dinc.w << 1)) >> 16;
                            fars = (state.SpanBuffer[nextScan].s + (dinc.s << 1)) >> 16;
                            fart = (state.SpanBuffer[nextScan].t + (dinc.t << 1)) >> 16;
                        }
                        else if (sigs.midSpan)
                        {
                            nextsw = state.SpanBuffer[nextScan].w >> 16;
                            nexts = state.SpanBuffer[nextScan].s >> 16;
                            nextt = state.SpanBuffer[nextScan].t >> 16;
                            farsw = (state.SpanBuffer[nextScan].w + dinc.w) >> 16;
                            fars = (state.SpanBuffer[nextScan].s + dinc.s) >> 16;
                            fart = (state.SpanBuffer[nextScan].t + dinc.t) >> 16;
                        }
                        else if (sigs.oneLessThanMid)
                        {
                            nextsw = (state.SpanBuffer[nextScan].w + dinc.w) >> 16;
                            nexts = (state.SpanBuffer[nextScan].s + dinc.s) >> 16;
                            nextt = (state.SpanBuffer[nextScan].t + dinc.w) >> 16;
                            farsw = (state.SpanBuffer[nextScan].w - dinc.w) >> 16;
                            fars = (state.SpanBuffer[nextScan].s - dinc.s) >> 16;
                            fart = (state.SpanBuffer[nextScan].t - dinc.t) >> 16;
                        }
                        else
                        {
                            nextsw = (tcoord.w + dinc.w) >> 16;
                            nexts = (tcoord.s + dinc.s) >> 16;
                            nextt = (tcoord.t + dinc.t) >> 16;
                            farsw = (tcoord.w + (dinc.w << 1)) >> 16;
                            fars = (tcoord.s + (dinc.s << 1)) >> 16;
                            fart = (tcoord.t + (dinc.t << 1)) >> 16;
                        }
                    }
                }
                else
                {
                    nextsw = (tcoord.w + dinc.w) >> 16;
                    nexts = (tcoord.s + dinc.s) >> 16;
                    nextt = (tcoord.t + dinc.t) >> 16;
                    farsw = (tcoord.w + (dinc.w << 1)) >> 16;
                    fars = (tcoord.s + (dinc.s << 1)) >> 16;
                    fart = (tcoord.t + (dinc.t << 1)) >> 16;
                }

                rdp.TextureCoordDivision(nexts, nextt, nextsw, ref nexts, ref nextt);
                rdp.TextureCoordDivision(fars, fart, farsw, ref fars, ref fart);

                bool lodclamp = fart.IsTrue(0x60000) || nextt.IsTrue(0x60000) || fars.IsTrue(0x60000) || nexts.IsTrue(0x60000);

                if (!lodclamp)
                {
                    Convert4X17To15(nexts, fars, nextt, fart, 0, ref lod);
                }

                Signals(state, lodclamp, lod, ref lodTile, ref magnify, ref distant, ref prelodfrac);

                if (state.OtherModes.EnableTextureLevelOfDetail)
                {
                    if (distant)
                    {
                        lodTile = state.MaxLevel;
                    }

                    if (state.OtherModes.EnableDetailTextures || magnify)
                    {
                        t1 = (prim_tile + lodTile) & 7;
                    }
                    else
                    {
                        t1 = (prim_tile + lodTile + 1) & 7;
                    }
                }
            }
        }

        public static void Copy(MadCatRdp rdp, ref int sss, ref int sst, TextureCoordinate tcoord, TextureCoordinate dinc, int prim_tile, ref int t1)
        {
            int fars;
            int fart;
            int farsw;
            int lod = 0;
            int lodTile;
            bool magnify;
            bool distant;
            int nextsw;
            int nexts;
            int nextt;
            var state = rdp.GraphicsState;

            Clamp(ref sss, ref sst);

            if (state.OtherModes.EnableTextureLevelOfDetail)
            {
                nextsw = (tcoord.w + dinc.w) >> 16;
                nexts = (tcoord.s + dinc.s) >> 16;
                nextt = (tcoord.t + dinc.t) >> 16;
                farsw = (tcoord.w + (dinc.w << 1)) >> 16;
                fars = (tcoord.s + (dinc.s << 1)) >> 16;
                fart = (tcoord.t + (dinc.t << 1)) >> 16;

                rdp.TextureCoordDivision(nexts, nextt, nextsw, ref nexts, ref nextt);
                rdp.TextureCoordDivision(fars, fart, farsw, ref fars, ref fart);

                bool lodclamp = fart.IsTrue(0x60000) || nextt.IsTrue(0x60000) || fars.IsTrue(0x60000) || nexts.IsTrue(0x60000);

                if (!lodclamp)
                {
                    Convert4X17To15(nexts, fars, nextt, fart, 0, ref lod);
                }

                if ((lod & 0x4000) != 0 || lodclamp)
                {
                    magnify = false;
                    lodTile = state.MaxLevel;
                }
                else if (lod < 32)
                {
                    magnify = true;
                    lodTile = 0;
                }
                else
                {
                    magnify = false;
                    lodTile = s_Log2Table[(lod >> 5) & 0xFF];

                    if (state.MaxLevel != 0)
                    {
                        distant = (lod & 0x6000) != 0 || (lodTile >= state.MaxLevel);
                    }
                    else
                    {
                        distant = true;
                    }

                    if (distant)
                    {
                        lodTile = state.MaxLevel;
                    }
                }

                if (!state.OtherModes.EnableDetailTextures || magnify)
                {
                    t1 = prim_tile + lodTile & 7;
                }
                else
                {
                    t1 = (prim_tile + lodTile + 1) & 7;
                }
            }
        }
    }
}