namespace cor64.Rdp.LLE {
    public class Framebuffer {
        private MadCatRdp m_Rdp;
        private State GS => m_Rdp.GraphicsState;

        public Framebuffer(MadCatRdp rdpBase) {
            m_Rdp = rdpBase;
            GS.FramebufferFormat = (int)ImageDataFormat.RGBA;
            GS.FramebufferSize = (int)ImageSizeType.SizeInBytes_4;
            GS.FramebufferWidth = 0;
            GS.FramebufferWidth = 0;
        }

        public void ReadCycle1(uint curpixel, ref uint curpixel_memcvg) {
            switch ((ImageSizeType)GS.FramebufferSize) {
                case ImageSizeType.SizeInBytes_4: {
                    GS.MemoryColor.SetRGBFromSingle(0);
                    GS.MemoryColor.A = 0xE0;
                    curpixel_memcvg = 7;
                    break;
                }

                case ImageSizeType.SizeInBytes_8: {
                    uint fb = GS.FramebufferAddress + curpixel;
                    byte mem = m_Rdp.RdramMemory.ReadIdx8(fb ^ 1);
                    GS.MemoryColor.SetRGBFromSingle(mem);
                    GS.MemoryColor.A = 0xE0;
                    curpixel_memcvg = 7;
                    break;
                }

                case ImageSizeType.SizeInBytes_16: {
                    uint fb = (GS.FramebufferAddress >> 1) + curpixel;
                    ushort read = 0;
                    byte hbyte = 0;

                    if (GS.OtherModes.EnableImageRead) {
                        m_Rdp.RdramMemory.ReadPair16(ref read, ref hbyte, fb);
                        byte lowBits;

                        if ((ImageDataFormat)GS.FramebufferFormat == ImageDataFormat.RGBA) {
                            GS.MemoryColor.SetColor(Utils.FromRGBA5551(read));
                            lowBits = (byte)(((read & 1) << 2) | hbyte);
                        }
                        else {
                            GS.MemoryColor.SetRGBFromSingle(read >> 8);
                            lowBits = (byte)((read >> 5) & 7);
                        }

                        curpixel_memcvg = lowBits;
                        GS.MemoryColor.A = lowBits << 5;
                    }
                    else {
                        read = m_Rdp.RdramMemory.ReadIdx16(fb);

                        if ((ImageDataFormat)GS.FramebufferFormat == ImageDataFormat.RGBA) {
                            GS.MemoryColor.SetColor(Utils.FromRGBA5551(read));
                        }
                        else {
                            GS.MemoryColor.SetRGBFromSingle(read >> 8);
                        }

                        curpixel_memcvg = 7;
                        GS.MemoryColor.A = 0xE0;
                    }

                    break;
                }

                case ImageSizeType.SizeInBytes_32: {
                    uint fb = (GS.FramebufferAddress >> 2) + curpixel;

                    uint read = m_Rdp.RdramMemory.ReadIdx32(fb);

                    GS.MemoryColor.SetColor(Utils.FromRGBA8888(read));

                    if (GS.OtherModes.EnableImageRead) {
                        curpixel_memcvg = (read >> 5) & 7;
                        GS.MemoryColor.A = (int)(read & 0xE0);
                    }
                    else {
                        curpixel_memcvg = 7;
                        GS.MemoryColor.A = 0xE0;
                    }

                    break;
                }

                default: throw new RdpException("invalid fb size");
            }
        }

        public void ReadCycle2(uint curpixel, ref uint curpixel_memcvg) {
            switch ((ImageSizeType)GS.FramebufferSize) {
                case ImageSizeType.SizeInBytes_4: {
                    GS.PreMemoryColor.SetRGBFromSingle(0);
                    GS.PreMemoryColor.A = 0xE0;
                    curpixel_memcvg = 7;
                    break;
                }

                case ImageSizeType.SizeInBytes_8: {
                    uint fb = GS.FramebufferAddress + curpixel;
                    byte mem = m_Rdp.RdramMemory.ReadIdx8(fb ^ 1);
                    GS.PreMemoryColor.SetRGBFromSingle(mem);
                    GS.PreMemoryColor.A = 0xE0;
                    curpixel_memcvg = 7;
                    break;
                }

                case ImageSizeType.SizeInBytes_16: {
                    uint fb = (GS.FramebufferAddress >> 1) + curpixel;
                    ushort read = 0;
                    byte hbyte = 0;
                    byte lowBits;

                    if (GS.OtherModes.EnableImageRead) {
                        m_Rdp.RdramMemory.ReadPair16(ref read, ref hbyte, fb);

                        if ((ImageDataFormat)GS.FramebufferFormat == ImageDataFormat.RGBA) {
                            GS.PreMemoryColor = Utils.FromRGBA5551(read);
                            lowBits = (byte)(((read & 1) << 2) | hbyte);
                        }
                        else {
                            GS.PreMemoryColor.SetRGBFromSingle(read >> 8);
                            lowBits = (byte)((read >> 5) & 7);
                        }

                        curpixel_memcvg = lowBits;
                        GS.PreMemoryColor.A = lowBits << 5;
                    }
                    else {
                        read = m_Rdp.RdramMemory.ReadIdx16(fb);

                        if ((ImageDataFormat)GS.FramebufferFormat == ImageDataFormat.RGBA) {
                            GS.PreMemoryColor = Utils.FromRGBA5551(read);
                        }
                        else {
                            GS.PreMemoryColor.SetRGBFromSingle(read >> 8);
                        }

                        curpixel_memcvg = 7;
                        GS.PreMemoryColor.A = 0xE0;
                    }

                    break;
                }

                case ImageSizeType.SizeInBytes_32: {
                    uint fb = (GS.FramebufferAddress >> 2) + curpixel;

                    uint read = m_Rdp.RdramMemory.ReadIdx32(fb);

                    GS.PreMemoryColor = Utils.FromRGBA8888(read);

                    if (GS.OtherModes.EnableImageRead) {
                        curpixel_memcvg = (read >> 5) & 7;
                        GS.PreMemoryColor.A = (int)(read & 0xE0);
                    }
                    else {
                        curpixel_memcvg = 7;
                        GS.PreMemoryColor.A = 0xE0;
                    }

                    break;
                }

                default: throw new RdpException("invalid fb size");
            }
        }

        // Uuse this for debugging the RDP, this puts pixels write into the framebuffer, no processing involved
        public void DirectWrite(uint curpixel, int r, int g, int b) {
            switch ((ImageSizeType)GS.FramebufferSize) {

                case ImageSizeType.SizeInBytes_4:  {
                    m_Rdp.RdramMemory.WriteIdx8(GS.FramebufferAddress + curpixel, 0);
                    break;
                }

                case ImageSizeType.SizeInBytes_8: {
                    m_Rdp.RdramMemory.WriteRdram8(GS.FramebufferAddress + curpixel, (byte)(r & 0xFF));
                    break;
                }

                case ImageSizeType.SizeInBytes_16: {
                    uint fb = (GS.FramebufferAddress >> 1) + curpixel;
                    int finalColor = ((r & ~7) << 8) | ((g & ~7) << 3) | ((b & ~7) >> 2);
                    m_Rdp.RdramMemory.WriteRdram16(fb << 1, (ushort)finalColor);
                    break;
                }

                case ImageSizeType.SizeInBytes_32: {
                    uint fb = (GS.FramebufferAddress >> 2) + curpixel;
                    uint finalColor = ((uint)r << 24) | ((uint)g << 16) | ((uint)b << 8);
                    m_Rdp.RdramMemory.WriteRdram32(fb << 2, finalColor);
                    break;
                }

                default: throw new RdpException("invalid fb size");
            }
        }

        public void Write(uint curpixel, int r, int g, int b, bool enableBlend, uint curpixel_cvg, uint curpixel_memcvg) {
            switch ((ImageSizeType)GS.FramebufferSize) {

                case ImageSizeType.SizeInBytes_4:  {
                    m_Rdp.RdramMemory.WriteIdx8(GS.FramebufferAddress + curpixel, 0);
                    break;
                }

                case ImageSizeType.SizeInBytes_8: {
                    m_Rdp.RdramMemory.WritePair8(GS.FramebufferAddress + curpixel, (byte)(r & 0xFF), (byte)(r.IsTrue(1) ? 3 : 0));
                    break;
                }

                case ImageSizeType.SizeInBytes_16: {
                    // DEBUG: Draw coverage
                    //int covdraw = (int)((curpixel_cvg - 1) << 5);
                    //r=covdraw; g=covdraw; b=covdraw;

                    uint fb = (GS.FramebufferAddress >> 1) + curpixel;

                    int finalCvg = Coverage.FinializeSpanAlpha(GS.OtherModes.CoverageDestination, enableBlend, curpixel_cvg, curpixel_memcvg);
                    int finalColor;

                    if ((ImageDataFormat)GS.FramebufferFormat == ImageDataFormat.RGBA) {
                        finalColor = ((r & ~7) << 8) | ((g & ~7) << 3) | ((b & ~7) >> 2);
                    }
                    else {
                        finalColor = (r << 8) | (finalCvg << 5);
                        finalCvg = 0;
                    }

                    ushort rval = (ushort)(finalColor | (finalCvg >> 2));
                    byte hval = (byte)(finalCvg & 3);

                    m_Rdp.RdramMemory.WritePair16(fb, rval, hval);
                    break;
                }

                case ImageSizeType.SizeInBytes_32: {
                    uint fb = (GS.FramebufferAddress >> 2) + curpixel;

                    int finalCvg = Coverage.FinializeSpanAlpha(GS.OtherModes.CoverageDestination, enableBlend, curpixel_cvg, curpixel_memcvg);
                    
                    uint finalColor = ((uint)r << 24) | ((uint)g << 16) | ((uint)b << 8);
                    finalColor |= (uint)finalCvg << 5;

                    m_Rdp.RdramMemory.WritePair32(fb, finalColor, (byte)(g.IsTrue(1) ? 3 : 0), 0);
                    break;
                }

                default: throw new RdpException("invalid fb size");
            }
        }

        public void Fill_4(uint curpixel) {
            GS.PipelineCrashed = true;
        }

        public void Fill_8(uint curpixel) {
            uint fb = GS.FramebufferAddress + curpixel;
            uint val = (GS.FillColor >> (int)(((fb & 3) ^ 3) << 3)) & 0xFF;
            byte hval = (byte)(((val & 1) << 1) | (val & 1));
            m_Rdp.RdramMemory.WritePair8(fb ^ 1, (byte)val, hval);
        }

        public void Fill_16(uint curpixel) {
            ushort val;
            byte hval;

            uint fbIndex = (GS.FramebufferAddress >> 1) + curpixel;

            if (fbIndex.IsTrue(1)) {
                val = (ushort)GS.FillColor;
            }
            else {
                val = (ushort)(GS.FillColor >> 16);
            }

            hval = (byte)(((val & 1) << 1) | (val & 1));

            m_Rdp.RdramMemory.WritePair16(
               fbIndex,
               val,
               hval);
        }

        public void Fill_32(uint curpixel) {
            uint fbIndex = (GS.FramebufferAddress >> 2) +  curpixel;

            m_Rdp.RdramMemory.WritePair32(
                fbIndex,
                GS.FillColor,
                GS.FillColor.IsTrue(0x10000) ?  (byte)3 : (byte)0,
                GS.FillColor.IsTrue(1) ? (byte)3 :(byte)0
            );
        }
    }
}