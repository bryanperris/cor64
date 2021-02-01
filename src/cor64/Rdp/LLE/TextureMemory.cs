using System;
using cor64.IO;

namespace cor64.Rdp.LLE {
    public class TextureMemory {
        private readonly UnmanagedBuffer m_TMemory = new UnmanagedBuffer(0x1000);
        private readonly TextureMemReader m_TmemReader;
        private readonly State m_State;

        public TextureMemory(MadCatRdp rdp) {
            m_State = rdp.GraphicsState;
            m_TmemReader = new TextureMemReader(m_State, m_TMemory);
        }

        public int ReadTmem16Idx(int address) => m_TmemReader.ReadTmem16Idx(address);
        public void WriteTmem16Idx(int address, int value) => m_TmemReader.WriteTmem16Idx(address, value);

        public static void SortIndex(ref int index, int indexA, int indexB, int indexC, int indexD, int bankNumber) {
            if ((indexA & 3) == bankNumber) {
                index = indexA & 0x3FF;
            }
            else if ((indexB & 3) == bankNumber) {
                index = indexB & 0x3FF;
            }
            else if ((indexC & 3) == bankNumber) {
                index = indexC & 0x3FF;
            }
            else if ((indexD & 3) == bankNumber) {
                index = indexD & 0x3FF;
            }
            else {
                index = 0;
            }
        }

        public static void SortShortsLowHalf(ref int bindShort, int short0, int short1, int short2, int short3, int bankNumber) {
            bindShort = bankNumber switch {
                0 => short0,
                1 => short1,
                2 => short2,
                3 => short3,
                _ => throw new RdpException("invalid bank number for SortShortsLowHalf")
            };
        }

        private void ComputeColorIndex(ref int colorIndex, int readShort, int nibbleOffset, int tileIndex) {
            int lowNibble, highNibble;

            if ((ImageSizeType)m_State.Tiles[tileIndex].Size == ImageSizeType.SizeInBytes_4) {
                lowNibble = (nibbleOffset ^ 3) << 2;
                highNibble = m_State.Tiles[tileIndex].Palette;
            }
            else {
                lowNibble = ((nibbleOffset & 2) ^ 2) << 2;

                highNibble = lowNibble != 0 ?
                    ((readShort >> 12) & 0xF) :
                    ((readShort >> 4) & 0xF);
            }

            lowNibble = (readShort >> lowNibble) & 0xF;
            colorIndex = (highNibble << 4) | lowNibble;
        }

        public static void GetTmemIndex(Tile tile, int s, int t, ref int idx0, ref int idx1, ref int idx2, ref int idx3, ref bool bit3Flip, ref bool hiBit) {
            int tbase = (tile.Line * t) & 0x1FF;
            tbase += tile.TmemAddress;

            var tsize = (ImageSizeType)tile.Size;
            var tfomat = (ImageDataFormat)tile.Format;

            int sshorts;

            if (tsize == ImageSizeType.SizeInBytes_8 || tfomat == ImageDataFormat.YUV) {
                sshorts = s >> 1;
            }
            else if (tsize >= ImageSizeType.SizeInBytes_16) {
                sshorts = s;
            }
            else {
                sshorts = s >> 2;
            }

            sshorts &= 0x7FF;

            bit3Flip = sshorts.IsTrue(2) ^ t.IsTrue(1);

            int tidx_a = ((tbase << 2) + sshorts) & 0x7FD;
            int tidx_b = (tidx_a + 1) & 0x7FF;
            int tidx_c = (tidx_a + 2) & 0x7FF;
            int tidx_d = (tidx_a + 3) & 0x7FF;

            hiBit = tidx_a.IsTrue(0x400);

            if (t.IsTrue(1)) {
                tidx_a ^= 2;
                tidx_b ^= 2;
                tidx_c ^= 2;
                tidx_d ^= 2;
            }

            SortIndex(ref idx0, tidx_a, tidx_b, tidx_c, tidx_d, 0);
            SortIndex(ref idx1, tidx_a, tidx_b, tidx_c, tidx_d, 1);
            SortIndex(ref idx2, tidx_a, tidx_b, tidx_c, tidx_d, 2);
            SortIndex(ref idx3, tidx_a, tidx_b, tidx_c, tidx_d, 3);
        }

        public void Copy(State state, int s, int s1, int s2, int s3, int t, int tileIndex, int[] sortshort, bool[] hibits, int[] lowbits) {
            int tbase = (state.Tiles[tileIndex].Line * t) & 0x1FF;
            tbase += state.Tiles[tileIndex].TmemAddress;

            var tsize = (ImageSizeType)state.Tiles[tileIndex].Size;
            var tformat = (ImageDataFormat)state.Tiles[tileIndex].Format;

            int[] sortidx = new int[8];

            int shbytes;
            int shbytes1;
            int shbytes2;
            int shbytes3;

            if (tsize == ImageSizeType.SizeInBytes_8 || tformat == ImageDataFormat.YUV)
            {
                shbytes =  s  << 1;
                shbytes1 = s1 << 1;
                shbytes2 = s2 << 1;
                shbytes3 = s3 << 1;
            }
            else if (tsize >= ImageSizeType.SizeInBytes_16)
            {
                shbytes =  s  << 2;
                shbytes1 = s1 << 2;
                shbytes2 = s2 << 2;
                shbytes3 = s3 << 2;
            }
            else
            {
                shbytes =  s;
                shbytes1 = s1;
                shbytes2 = s2;
                shbytes3 = s3;
            }

            shbytes  &= 0x1FFF;
            shbytes1 &= 0x1FFF;
            shbytes2 &= 0x1FFF;
            shbytes3 &= 0x1FFF;

            tbase <<= 4;

            int tidx_a =   (tbase + shbytes)  & 0x1FFF;
            int tidx_bhi = (tbase + shbytes1) & 0x1FFF;
            int tidx_c =   (tbase + shbytes2) & 0x1FFF;
            int tidx_dhi = (tbase + shbytes3) & 0x1FFF;

            int tidx_blow;
            int tidx_dlow;

            if (tformat == ImageDataFormat.YUV) {
                int delta = shbytes1 - shbytes;
                tidx_blow = (tidx_a + (delta << 1)) & 0x1FFF;
                tidx_dlow = (tidx_blow + shbytes3 - shbytes) & 0x1FFF;
            }
            else {
                tidx_blow = tidx_bhi;
                tidx_dlow = tidx_dhi;
            }

            if (t.IsTrue(1)) {
                tidx_a    ^= 8;
                tidx_blow ^= 8;
                tidx_bhi  ^= 8;
                tidx_c    ^= 8;
                tidx_dlow ^= 8;
                tidx_dhi  ^= 8;
            }

            hibits[0] =  tidx_a.IsTrue(0x1000);
            hibits[1] =  tidx_blow.IsTrue(0x1000);
            hibits[2] =  tidx_bhi.IsTrue(0x1000);
            hibits[3] =  tidx_c.IsTrue(0x1000);
            hibits[4] =  tidx_dlow.IsTrue(0x1000);
            hibits[5] =  tidx_dhi.IsTrue(0x1000);

            lowbits[0] = tidx_a    & 0xF;
            lowbits[1] = tidx_blow & 0xF;
            lowbits[2] = tidx_bhi  & 0xF;
            lowbits[3] = tidx_c    & 0xF;
            lowbits[4] = tidx_dlow & 0xF;
            lowbits[5] = tidx_dhi  & 0xF;

            tidx_a    >>= 2;
            tidx_blow >>= 2;
            tidx_bhi  >>= 2;
            tidx_c    >>= 2;
            tidx_dlow >>= 2;
            tidx_dhi  >>= 2;

            SortIndex(ref sortidx[0], tidx_a, tidx_blow, tidx_c, tidx_dlow, 0);
            SortIndex(ref sortidx[1], tidx_a, tidx_blow, tidx_c, tidx_dlow, 1);
            SortIndex(ref sortidx[2], tidx_a, tidx_blow, tidx_c, tidx_dlow, 2);
            SortIndex(ref sortidx[3], tidx_a, tidx_blow, tidx_c, tidx_dlow, 3);

            int short0 = ReadTmem16Idx(sortidx[0]);
            int short1 = ReadTmem16Idx(sortidx[1]);
            int short2 = ReadTmem16Idx(sortidx[2]);
            int short3 = ReadTmem16Idx(sortidx[3]);

            SortShortsLowHalf(ref sortshort[0], short0, short1, short2, short3, lowbits[0] >> 2);
            SortShortsLowHalf(ref sortshort[1], short0, short1, short2, short3, lowbits[1] >> 2);
            SortShortsLowHalf(ref sortshort[2], short0, short1, short2, short3, lowbits[3] >> 2);
            SortShortsLowHalf(ref sortshort[3], short0, short1, short2, short3, lowbits[4] >> 2);

            int[] colorIndex = new int[4];

            if (state.OtherModes.EnableTextureLookupTable) {
                ComputeColorIndex(ref short0, sortshort[0], lowbits[0] & 3, tileIndex);
                ComputeColorIndex(ref short1, sortshort[1], lowbits[1] & 3, tileIndex);
                ComputeColorIndex(ref short2, sortshort[2], lowbits[3] & 3, tileIndex);
                ComputeColorIndex(ref short3, sortshort[3], lowbits[4] & 3, tileIndex);

                colorIndex[0] = short0 & 0xFF;
                colorIndex[1] = short1 & 0xFF;
                colorIndex[2] = short2 & 0xFF;
                colorIndex[3] = short3 & 0xFF;

                sortidx[4] = short0 << 2;
                sortidx[5] = (short1 << 2) | 1;
                sortidx[6] = (short2 << 2) | 2;
                sortidx[7] = (short3 << 2) | 3;
            }
            else {
                SortIndex(ref sortidx[4], tidx_a, tidx_bhi, tidx_c, tidx_dhi, 0);
                SortIndex(ref sortidx[5], tidx_a, tidx_bhi, tidx_c, tidx_dhi, 1);
                SortIndex(ref sortidx[6], tidx_a, tidx_bhi, tidx_c, tidx_dhi, 2);
                SortIndex(ref sortidx[7], tidx_a, tidx_bhi, tidx_c, tidx_dhi, 3);
            }

            short0 = ReadTmem16Idx(sortidx[4] | 0x400);
            short1 = ReadTmem16Idx(sortidx[5] | 0x400);
            short2 = ReadTmem16Idx(sortidx[6] | 0x400);
            short3 = ReadTmem16Idx(sortidx[7] | 0x400);

            // 8-bit internal palette support
            if ((ImageSizeType)state.FramebufferSize == ImageSizeType.SizeInBytes_8 && state.OtherModes.EnableTextureLookupTable) {
               short0 |= colorIndex[0];
               short1 |= colorIndex[1];
               short2 |= colorIndex[2];
               short3 |= colorIndex[3];
            }

            if (state.OtherModes.EnableTextureLookupTable) {
                SortShortsLowHalf(ref sortshort[4], short0, short1, short2, short3, 0);
                SortShortsLowHalf(ref sortshort[5], short0, short1, short2, short3, 1);
                SortShortsLowHalf(ref sortshort[6], short0, short1, short2, short3, 2);
                SortShortsLowHalf(ref sortshort[7], short0, short1, short2, short3, 3);
            }
            else {
                SortShortsLowHalf(ref sortshort[4], short0, short1, short2, short3, lowbits[0] >> 2);
                SortShortsLowHalf(ref sortshort[5], short0, short1, short2, short3, lowbits[2] >> 2);
                SortShortsLowHalf(ref sortshort[6], short0, short1, short2, short3, lowbits[3] >> 2);
                SortShortsLowHalf(ref sortshort[7], short0, short1, short2, short3, lowbits[5] >> 2);
            }
        }

        public void FetchQuadro(ref Color color0, ref Color color1, ref Color color2, ref Color color3, int s0, int sdiff, int t0, int tdiff, uint tileIndex, bool unequaluppers) {
            int t1 = (t0 & 0xFF) + tdiff;
            int s1 = s0 + sdiff;

            int tbase0 = (m_State.Tiles[tileIndex].Line * (t0 & 0xFF)) + m_State.Tiles[tileIndex].TmemAddress;
            int tbase2 = (m_State.Tiles[tileIndex].Line * t1) + m_State.Tiles[tileIndex].TmemAddress;

            switch ((TexelFormat)m_State.Tiles[tileIndex].RdpNonTlutSwitch) {
                case TexelFormat.RGBA4: {
                    m_TmemReader.Quadro_FetchTexel_RGBA4(tbase0, tbase2, s0, s1, t0, t1);
                    break;
                }

                case TexelFormat.RGBA8: {
                    m_TmemReader.Quadro_FetchTexel_RGBA8(tbase0, tbase2, s0, s1, t0, t1);
                    break;
                }

                case TexelFormat.RGBA16: {
                    m_TmemReader.Quadro_FetchTexel_RGBA16(tbase0, tbase2, s0, s1, t0, t1);
                    break;
                }

                case TexelFormat.RGBA32: {
                    m_TmemReader.Quadro_FetchTexel_RGBA32(tbase0, tbase2, s0, s1, t0, t1);
                    break;
                }

                case TexelFormat.YUV4: {
                    m_TmemReader.Quadro_FetchTexel_YUV4(tbase0, tbase2, s0, s1, t0, t1, sdiff, unequaluppers);
                    break;
                }

                case TexelFormat.YUV8: {
                    m_TmemReader.Quadro_FetchTexel_YUV8(tbase0, tbase2, s0, s1, t0, t1, sdiff, unequaluppers);
                    break;
                }

                case TexelFormat.YUV16: {
                    m_TmemReader.Quadro_FetchTexel_YUV16(tbase0, tbase2, s0, s1, t0, t1, sdiff);
                    break;
                }

                case TexelFormat.YUV32: {
                    m_TmemReader.Quadro_FetchTexel_YUV32(tbase0, tbase2, s0, s1, t0, t1, sdiff);
                    break;
                }

                case TexelFormat.CI4: {
                    m_TmemReader.Quadro_FetchTexel_CI4(tbase0, tbase2, s0, s1, t0, t1, m_State.Tiles[tileIndex].Palette);
                    break;
                }

                case TexelFormat.CI8: {
                    m_TmemReader.Quadro_FetchTexel_CI8(tbase0, tbase2, s0, s1, t0, t1);
                    break;
                }

                case TexelFormat.CI16:
                case TexelFormat.CI32: {
                    m_TmemReader.Quadro_FetchTexel_CI32(tbase0, tbase2, s0, s1, t0, t1);
                    break;
                }

                case TexelFormat.IA4: {
                    m_TmemReader.Quadro_FetchTexel_IA4(tbase0, tbase2, s0, s1, t0, t1);
                    break;
                }

                case TexelFormat.IA8: {
                    m_TmemReader.Quadro_FetchTexel_IA8(tbase0, tbase2, s0, s1, t0, t1);
                    break;
                }

                case TexelFormat.IA16: {
                    m_TmemReader.Quadro_FetchTexel_IA16(tbase0, tbase2, s0, s1, t0, t1);
                    break;
                }

                case TexelFormat.IA32: {
                    m_TmemReader.Quadro_FetchTexel_IA32(tbase0, tbase2, s0, s1, t0, t1);
                    break;
                }

                case TexelFormat.I4: {
                    m_TmemReader.Quadro_FetchTexel_I4(tbase0, tbase2, s0, s1, t0, t1);
                    break;
                }

                case TexelFormat.I8: {
                    m_TmemReader.Quadro_FetchTexel_I8(tbase0, tbase2, s0, s1, t0, t1);
                    break;
                }

                case TexelFormat.I16:
                case TexelFormat.I32: {
                    m_TmemReader.Quadro_FetchTexel_I32(tbase0, tbase2, s0, s1, t0, t1);
                    break;
                }

                default: throw new RdpException("Invalid texel type provided for Tmem_FetchQuadro");
            }

            color0 = m_TmemReader.ReadColor(0);
            color1 = m_TmemReader.ReadColor(1);
            color2 = m_TmemReader.ReadColor(2);
            color3 = m_TmemReader.ReadColor(3);
        }

        public void FetchTexel(ref Color color, int s, int t, uint tileIndex) {
            int tbase = (m_State.Tiles[tileIndex].Line * (t & 0xFF)) + m_State.Tiles[tileIndex].TmemAddress;

            switch ((TexelFormat)m_State.Tiles[tileIndex].RdpNonTlutSwitch) {
                case TexelFormat.RGBA4: {
                    m_TmemReader.FetchTexel_RGBA4(tbase, s, t);
                    break;
                }

                case TexelFormat.RGBA8: {
                    m_TmemReader.FetchTexel_RGBA8(tbase, s, t);
                    break;
                }

                case TexelFormat.RGBA16: {
                    m_TmemReader.FetchTexel_RGBA16(tbase, s, t);
                    break;
                }

                case TexelFormat.RGBA32: {
                    m_TmemReader.FetchTexel_RGBA32(tbase, s, t);
                    break;
                }

                case TexelFormat.YUV4: {
                    m_TmemReader.FetchTexel_YUV4(tbase, s, t);
                    break;
                }

                case TexelFormat.YUV8: {
                    m_TmemReader.FetchTexel_YUV8(tbase, s, t);
                    break;
                }

                case TexelFormat.YUV16: {
                    m_TmemReader.FetchTexel_YUV16(tbase, s, t);
                    break;
                }

                case TexelFormat.YUV32: {
                    m_TmemReader.FetchTexel_YUV32(tbase, s, t);
                    break;
                }

                case TexelFormat.CI4: {
                    m_TmemReader.FetchTexel_CI4(tbase, s, t, m_State.Tiles[tileIndex].Palette);
                    break;
                }

                case TexelFormat.CI8: {
                    m_TmemReader.FetchTexel_CI8(tbase, s, t);
                    break;
                }

                case TexelFormat.CI16:
                case TexelFormat.CI32: {
                    m_TmemReader.FetchTexel_CI32(tbase, s, t);
                    break;
                }

                case TexelFormat.IA4: {
                    m_TmemReader.FetchTexel_IA4(tbase, s, t);
                    break;
                }

                case TexelFormat.IA8: {
                    m_TmemReader.FetchTexel_IA8(tbase, s, t);
                    break;
                }

                case TexelFormat.IA16: {
                    m_TmemReader.FetchTexel_IA16(tbase, s, t);
                    break;
                }

                case TexelFormat.IA32: {
                    m_TmemReader.FetchTexel_IA32(tbase, s, t);
                    break;
                }

                case TexelFormat.I4: {
                    m_TmemReader.FetchTexel_I4(tbase, s, t);
                    break;
                }

                case TexelFormat.I8: {
                    m_TmemReader.FetchTexel_I8(tbase, s, t);
                    break;
                }

                case TexelFormat.I16:
                case TexelFormat.I32: {
                    m_TmemReader.FetchTexel_I32(tbase, s, t);
                    break;
                }

                default: throw new RdpException("Invalid texel type provided for Tmem_FetchTexel");
            }

            color = m_TmemReader.ReadColor(0);
        }

        public void FetchTexel_QuadroTlut(ref Color color0, ref Color color1, ref Color color2, ref Color color3, int s0, int sdiff, int t0, int tdiff, uint tileIndex, bool isUpper, bool isUpperRg) {
            int t1 = (t0 & 0xFF) + tdiff;
            int tbase0 = (m_State.Tiles[tileIndex].Line * (t0 & 0xFF)) + m_State.Tiles[tileIndex].TmemAddress;
            int tbase2 = (m_State.Tiles[tileIndex].Line * t1) + m_State.Tiles[tileIndex].TmemAddress;

            int palette = m_State.Tiles[tileIndex].Palette << 4;

            switch (m_State.Tiles[tileIndex].RdpTlutSwitch) {
                case 0:
                case 1:
                case 2: {
                    m_TmemReader.QuadroTlut_FetchTexel_TypeA(tbase0, tbase2, s0, t0, t1, sdiff, palette, isUpper, isUpperRg);
                    break;
                }

                case 3: {
                    m_TmemReader.QuadroTlut_FetchTexel_TypeB(tbase0, tbase2, s0, t0, t1, sdiff, palette, isUpper, isUpperRg);
                    break;
                }

                case 4:
                case 5:
                case 6: {
                    m_TmemReader.QuadroTlut_FetchTexel_TypeC(tbase0, tbase2, s0, t0, t1, sdiff, isUpper, isUpperRg);
                    break;
                }

                case 7: {
                    m_TmemReader.QuadroTlut_FetchTexel_TypeD(tbase0, tbase2, s0, t0, t1, sdiff, isUpper, isUpperRg);
                    break;
                }

                case 8:
                case 9:
                case 10: {
                    m_TmemReader.QuadroTlut_FetchTexel_TypeE(tbase0, tbase2, s0, t0, t1, sdiff, isUpper, isUpperRg);
                    break;
                }

                case 11: {
                    m_TmemReader.QuadroTlut_FetchTexel_TypeF(tbase0, tbase2, s0, t0, t1, sdiff, isUpper, isUpperRg);
                    break;
                }

                case 12:
                case 13:
                case 14: {
                    m_TmemReader.QuadroTlut_FetchTexel_TypeG(tbase0, tbase2, s0, t0, t1, sdiff, isUpper, isUpperRg);
                    break;
                }

                default: {
                    m_TmemReader.QuadroTlut_FetchTexel_TypeH(tbase0, tbase2, s0, t0, t1, sdiff, isUpper, isUpperRg);
                    break;
                }
            }

            color0 = m_TmemReader.ReadColor(0);
            color1 = m_TmemReader.ReadColor(1);
            color2 = m_TmemReader.ReadColor(2);
            color3 = m_TmemReader.ReadColor(3);
        }

        public void FetchTexel_QuadroTlutNearest(ref Color color0, ref Color color1, ref Color color2, ref Color color3, int s0, int t0, uint tileIndex, bool isUpper, bool isUpperRg) {
            int tbase0 = (m_State.Tiles[tileIndex].Line * (t0 & 0xFF)) + m_State.Tiles[tileIndex].TmemAddress;

            int palette = m_State.Tiles[tileIndex].Palette << 4;

            switch (m_State.Tiles[tileIndex].RdpTlutSwitch) {
                case 0:
                case 1:
                case 2: {
                    m_TmemReader.QuadroTlut_FetchTexel_TypeA(tbase0, 0, s0, t0, 0, 0, palette, isUpper, isUpperRg, true);
                    break;
                }

                case 3: {
                    m_TmemReader.QuadroTlut_FetchTexel_TypeB(tbase0, 0, s0, t0, 0, 0, palette, isUpper, isUpperRg, true);
                    break;
                }

                case 4:
                case 5:
                case 6: {
                    m_TmemReader.QuadroTlut_FetchTexel_TypeC(tbase0, 0, s0, t0, 0, 0, isUpper, isUpperRg, true);
                    break;
                }

                case 7: {
                    m_TmemReader.QuadroTlut_FetchTexel_TypeD(tbase0, 0, s0, t0, 0, 0, isUpper, isUpperRg, true);
                    break;
                }

                case 8:
                case 9:
                case 10: {
                    m_TmemReader.QuadroTlut_FetchTexel_TypeE(tbase0, 0, s0, t0, 0, 0, isUpper, isUpperRg, true);
                    break;
                }

                case 11: {
                    m_TmemReader.QuadroTlut_FetchTexel_TypeF(tbase0, 0, s0, t0, 0, 0, isUpper, isUpperRg, true);
                    break;
                }

                case 12:
                case 13:
                case 14: {
                    m_TmemReader.QuadroTlut_FetchTexel_TypeG(tbase0, 0, s0, t0, 0, 0, isUpper, isUpperRg, true);
                    break;
                }

                default: {
                    m_TmemReader.QuadroTlut_FetchTexel_TypeH(tbase0, 0, s0, t0, 0, 0, isUpper, isUpperRg, true);
                    break;
                }
            }

            color0 = m_TmemReader.ReadColor(0);
            color1 = m_TmemReader.ReadColor(1);
            color2 = m_TmemReader.ReadColor(2);
            color3 = m_TmemReader.ReadColor(3);
        }
    }
}