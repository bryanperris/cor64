using cor64.IO;
using cor64.Mips;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.RCP
{

    /*
     *         0x0440 0000 to 0x0440 0003  VI_STATUS_REG or VI_CONTROL_REG
                 VI status/control
           (RW): [1:0] type[1:0] (pixel size) 
                     0: blank (no data, no sync)
                     1: reserved
                     2: 5/5/5/3 ("16" bit)
                     3: 8/8/8/8 (32 bit)
                 [2] gamma_dither_enable (normally on, unless "special effect")
                 [3] gamma_enable (normally on, unless MPEG/JPEG)
                 [4] divot_enable (normally on if antialiased,
                     unless decal lines)
                 [5] reserved - always off
                 [6] serrate (always on if interlaced, off if not)                                          
                 [7] reserved - diagnostics only
                 [9:8] anti-alias (aa) mode[1:0] 
                     0: aa & resamp (always fetch extra lines)                                           
                     1: aa & resamp (fetch extra lines if needed)                                                
                     2: resamp only (treat as all fully covered)                                             
                     3: neither (replicate pixels, no interpolate)                                          
                 [11] reserved - diagnostics only
                 [15:12] reserved
        0x0440 0004 to 0x0440 0007  VI_ORIGIN_REG or VI_DRAM_ADDR_REG
                 VI origin
           (RW): [23:0] frame buffer origin in bytes
        0x0440 0008 to 0x0440 000B  VI_WIDTH_REG or VI_H_WIDTH_REG
                 VI width
           (RW): [11:0] frame buffer line width in pixels                                         
        0x0440 000C to 0x0440 000F  VI_INTR_REG or VI_V_INTR_REG
                 VI vertical intr
           (RW): [9:0] interrupt when current half-line = V_INTR
        0x0440 0010 to 0x0440 0013  VI_CURRENT_REG or VI_V_CURRENT_LINE_REG                                           
                 VI current vertical line
           (RW): [9:0] current half line, sampled once per line (the lsb of
                       V_CURRENT is constant within a field, and in
                       interlaced modes gives the field number - which is
                       constant for non-interlaced modes)
                       - Writes clears interrupt line
        0x0440 0014 to 0x0440 0017  VI_BURST_REG or VI_TIMING_REG
                 VI video timing
           (RW): [7:0] horizontal sync width in pixels
                 [15:8] color burst width in pixels
                 [19:16] vertical sync width in half lines
                 [29:20] start of color burst in pixels from h-sync
        0x0440 0018 to 0x0440 001B  VI_V_SYNC_REG
                 VI vertical sync
           (RW): [9:0] number of half-lines per field
        0x0440 001C to 0x0440 001F  VI_H_SYNC_REG
                 VI horizontal sync
           (RW): [11:0] total duration of a line in 1/4 pixel
                 [20:16] a 5-bit leap pattern used for PAL only (h_sync_period)
        0x0440 0020 to 0x0440 0023  VI_LEAP_REG or VI_H_SYNC_LEAP_REG
                 VI horizontal sync leap
           (RW): [11:0] identical to h_sync_period
                 [27:16] identical to h_sync_period
        0x0440 0024 to 0x0440 0027  VI_H_START_REG or VI_H_VIDEO_REG
                 VI horizontal video
           (RW): [9:0] end of active video in screen pixels
                 [25:16] start of active video in screen pixels
        0x0440 0028 to 0x0440 002B  VI_V_START_REG or VI_V_VIDEO_REG
                 VI vertical video
           (RW): [9:0] end of active video in screen half-lines
                 [25:16] start of active video in screen half-lines
        0x0440 002C to 0x0440 002F  VI_V_BURST_REG
                 VI vertical burst
           (RW): [9:0] end of color burst enable in half-lines
                 [25:16] start of color burst enable in half-lines
        0x0440 0030 to 0x0440 0033  VI_X_SCALE_REG
                 VI x-scale
           (RW): [11:0] 1/horizontal scale up factor (2.10 format)
                 [27:16] horizontal subpixel offset (2.10 format)
        0x0440 0034 to 0x0440 0037  VI_Y_SCALE_REG
                 VI y-scale
           (RW): [11:0] 1/vertical scale up factor (2.10 format)
                 [27:16] vertical subpixel offset (2.10 format)
        0x0440 0038 to 0x044F FFFF  Unused*/

    public class Video : PerpherialDevice
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private MemMappedBuffer m_ControlReg = new MemMappedBuffer(4);
        private MemMappedBuffer m_Origin = new MemMappedBuffer(4); // Where framebuffer is
        private MemMappedBuffer m_Width = new MemMappedBuffer(4);
        private MemMappedBuffer m_Interrupt = new MemMappedBuffer(4);
        private MemMappedBuffer m_CurrentLine = new MemMappedBuffer(4);
        private MemMappedBuffer m_Timing = new MemMappedBuffer(4);
        private MemMappedBuffer m_VSync = new MemMappedBuffer(4);
        private MemMappedBuffer m_HSync = new MemMappedBuffer(4);
        private MemMappedBuffer m_Leap = new MemMappedBuffer(4);
        private MemMappedBuffer m_HStart = new MemMappedBuffer(4);
        private MemMappedBuffer m_VStart = new MemMappedBuffer(4);
        private MemMappedBuffer m_VBurst = new MemMappedBuffer(4);
        private MemMappedBuffer m_XScale = new MemMappedBuffer(4);
        private MemMappedBuffer m_YScale = new MemMappedBuffer(4);
        private N64MemoryController m_Memory;
        private VideoControlReg m_ControlRegStruct;
        private MipsInterface m_Interface;

        public Video(N64MemoryController controller, MipsInterface mipsInterface) : base(controller, 0x100000)
        {
            Map(
                m_ControlReg,
                m_Origin,
                m_Width,
                m_Interrupt,
                m_CurrentLine,
                m_Timing,
                m_VSync,
                m_HSync,
                m_Leap,
                m_HStart,
                m_VStart,
                m_VBurst,
                m_XScale,
                m_YScale);

            m_Interface = mipsInterface;

            m_Origin.Write += FramebufferAddressHandler;
            m_CurrentLine.Write += CurrentScanlineHandler;

            m_Width.Write += () =>
            {
                Log.Debug("Framebuffer width set to " + m_Width.ReadPtr.AsType_32Swp().ToString("X8"));
            };

            m_XScale.Write += () => {
                Log.Debug("Framebuffer xscale set to " + m_XScale.ReadPtr.AsType_32Swp().ToString("X8"));
            };

            m_HStart.Write += () => {
                Log.Debug("Framebuffer hstart set to " + m_HStart.ReadPtr.AsType_32Swp().ToString("X8"));
            };

            m_VStart.Write += () => {
                Log.Debug("Framebuffer vstart set to " + m_VStart.ReadPtr.AsType_32Swp().ToString("X8"));
            };

            m_YScale.Write += () => {
                Log.Debug("Framebuffer yscale set to " + m_YScale.ReadPtr.AsType_32Swp().ToString("X8"));
            };

            m_Memory = controller;

            m_XScale.ReadPtr.AsType_32Swp(0x100U * (640U / 160U));
            m_YScale.ReadPtr.AsType_32Swp(0x100U * (480U / 60U));
            m_Width.ReadPtr.AsType_32Swp(640);


            m_ControlRegStruct = new VideoControlReg(m_ControlReg.ReadPtr);
        }

        private void CurrentScanlineHandler()
        {
            uint curr = m_CurrentLine.ReadPtr.AsType_32Swp();

            Log.Debug("Current scanline set to {0:X8}", curr);
        }

        private void FramebufferAddressHandler()
        {
            Log.Debug("Framebuffer pointer set to {0:X8}", FramebufferOffset);
        }

        public int FramebufferOffset => (int)(m_Origin.ReadPtr.AsType_32Swp() << 8 >> 8);

        public uint XScale => m_XScale.ReadPtr.AsType_32Swp();

        public uint YScale => m_YScale.ReadPtr.AsType_32Swp();

        public uint HStart => m_HStart.ReadPtr.AsType_32Swp();

        public uint VStart => m_VStart.ReadPtr.AsType_32Swp();

        public uint WidthReg => m_Width.ReadPtr.AsType_32Swp();

        private int ComputeHWidth()
        {
            if (XScale == 0)
                return 320;

            var start = ((HStart & 0x03FF0000) >> 16) & 0x3FF;
            var end = (HStart & 0x3FF);
            var delta = end - start;
            var scale = (XScale & 0xFFF);

            if (delta == 0)
            {
                delta = WidthReg;
            }

            return (int)((delta * scale) / 0x400);
        }

        private int ComputeVHeight()
        {
            if (YScale == 0)
                return 240;
                
            var start = ((VStart & 0x03FF0000) >> 16) & 0x3FF;
            var end = (VStart & 0x3FF);
            var delta = end - start;
            var scale = (YScale & 0xFFF);

            return (int)((delta * scale) / 0x800);
        }

        public int Width => ComputeHWidth();

        public int Height => ComputeVHeight();

        public VideoControlReg ControlReg => m_ControlRegStruct;

        public void CopyFramebufferRGB565(byte[] buffer)
        {
            m_CurrentLine.ReadPtr.AsType_32Swp(0);

            unsafe
            {
                fixed (byte * ptr = buffer)
                {
                    ushort* srcPixel = (ushort*)m_Memory.RDRAM.GetRamPointer(FramebufferOffset);
                    ushort* dstPixel = (ushort*)ptr;

                    for (int i = 0; i < buffer.Length / 2; i++)
                    {
                        ushort pixel = (*srcPixel).ByteSwapped();
                        srcPixel++;

                        if ((pixel & 1) == 1)
                        {
                            pixel >>= 1;
                            pixel <<= 1;
                        }
                        else
                        {
                            pixel = 0;
                        }

                        *dstPixel = pixel;
                        dstPixel++;
                    }
                }
            }

            m_CurrentLine.ReadPtr.AsType_32Swp(m_Interrupt.ReadPtr.AsType_32Swp());
        }

        public void CopyFramebufferRGBA8888(byte[] buffer)
        {
            m_CurrentLine.ReadPtr.AsType_32Swp(0);

            unsafe
            {
                fixed (byte* ptr = buffer)
                {
                    uint* srcPixel = (uint*)m_Memory.RDRAM.GetRamPointer(FramebufferOffset);
                    uint* dstPixel = (uint*)ptr;

                    for (int i = 0; i < buffer.Length / 4; i++)
                    {
                        uint pixel = *srcPixel;
                        srcPixel++;
                        *dstPixel = pixel;
                        dstPixel++;
                    }
                }
            }

            m_CurrentLine.ReadPtr.AsType_32Swp(m_Interrupt.ReadPtr.AsType_32Swp());
        }

        public void SetVideoInterrupt()
        {
            m_Interface.SetInterrupt(MipsInterface.INT_VI, true);
        }
    }
}
