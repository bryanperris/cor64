using System.Runtime.InteropServices;
using System.Threading;
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
        private readonly MemMappedBuffer m_ControlReg = new MemMappedBuffer(4);
        private readonly MemMappedBuffer m_Origin = new MemMappedBuffer(4); // Where framebuffer is
        private readonly MemMappedBuffer m_Width = new MemMappedBuffer(4);
        private readonly MemMappedBuffer m_Interrupt = new MemMappedBuffer(4);
        private readonly MemMappedBuffer m_CurrentLine = new MemMappedBuffer(4);
        private readonly MemMappedBuffer m_Timing = new MemMappedBuffer(4);
        private readonly MemMappedBuffer m_VSync = new MemMappedBuffer(4);
        private readonly MemMappedBuffer m_HSync = new MemMappedBuffer(4);
        private readonly MemMappedBuffer m_Leap = new MemMappedBuffer(4);
        private readonly MemMappedBuffer m_HStart = new MemMappedBuffer(4);
        private readonly MemMappedBuffer m_VStart = new MemMappedBuffer(4);
        private readonly MemMappedBuffer m_VBurst = new MemMappedBuffer(4);
        private readonly MemMappedBuffer m_XScale = new MemMappedBuffer(4);
        private readonly MemMappedBuffer m_YScale = new MemMappedBuffer(4);
        private readonly N64MemoryController m_Memory;
        private readonly VideoControlReg m_ControlRegStruct;
        private readonly MipsInterface m_Interface;

        private readonly ManualResetEvent m_RdpWait = new ManualResetEvent(true);
        private bool m_UseRdp = false;
        private uint m_RdpAddress;

        private int? m_RdpFramebufferWidth;

        private bool m_LastActiveState = false;

        private int m_Ticks;

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

            m_ControlReg.Write += () => {
                // Log.Debug("Video Control Reg: {0:X8}", m_ControlReg.RegisterValue);

                if (!m_LastActiveState && IsVideoActive) {
                    Log.Debug("Video is now active");
                }

                if (m_LastActiveState && !m_LastActiveState) {
                    Log.Debug("Video is not active");
                }

                m_LastActiveState = IsVideoActive;
            };

            m_Origin.Write += FramebufferAddressHandler;
            m_CurrentLine.Write += CurrentScanlineHandler;

            // m_Width.Write += () => Log.Debug("Framebuffer width set to " + m_Width.RegisterValue.ToString("X8"));

            // m_XScale.Write += () => Log.Debug("Framebuffer xscale set to " + m_XScale.RegisterValue.ToString("X8"));

            // m_HStart.Write += () => Log.Debug("Framebuffer hstart set to " + m_HStart.RegisterValue.ToString("X8"));

            // m_VStart.Write += () => Log.Debug("Framebuffer vstart set to " + m_VStart.RegisterValue.ToString("X8"));

            // m_YScale.Write += () => Log.Debug("Framebuffer yscale set to " + m_YScale.RegisterValue.ToString("X8"));

            // m_Interrupt.Write += () => Log.Debug("Framebuffer interrupt set to " + m_Interrupt.RegisterValue.ToString("X8"));

            m_CurrentLine.Read += () => {
                if (!IsVideoActive || Interrupt == 0) return;

                Line++;

                if (Line > Interrupt) Line = 0;
            };

            m_Memory = controller;

            m_XScale.RegisterValue = 0x100U * (640U / 160U);
            m_YScale.RegisterValue = 0x100U * (480U / 60U);
            m_Width.RegisterValue = 640;


            m_ControlRegStruct = new VideoControlReg(m_ControlReg);
        }

        public void Tick() {
            m_Ticks++;

            #if FASTER_VI
            m_Ticks += 10;
            #endif

            // CPU frequency divided by 60 Hz
            if (m_Ticks >= (93750000 / 60)) {
                if (IsVideoActive) SetVideoInterrupt();
                m_Ticks = 0;
            }
        }

        private void CurrentScanlineHandler()
        {
            m_Interface.ClearInterrupt(MipsInterface.INT_VI);
            m_Ticks = 0;
        }

        private void FramebufferAddressHandler()
        {
            // Log.Debug("Framebuffer pointer set to {0:X8}", FramebufferOffset);
        }

        public int ReadFramebufferAddressSafe() {
            // m_RdpWait.WaitOne();

            // if (!m_UseRdp)
                return (int)(m_Origin.RegisterValue & 0x00FFFFFF);
            // else
            //     return (int)(m_RdpAddress & 0x00FFFFFF);
        }

        public uint Origin => m_Origin.RegisterValue;

        public uint XScale => m_XScale.RegisterValue;

        public uint YScale => m_YScale.RegisterValue;

        public uint HStart => m_HStart.RegisterValue;

        public uint VStart => m_VStart.RegisterValue;

        public uint WidthReg => m_Width.RegisterValue;

        private int ComputeHWidth()
        {
            if (m_RdpFramebufferWidth != null) {
                return m_RdpFramebufferWidth.Value;
            }

            if (XScale == 0)
                return 320;

            int start = (int)((HStart >> 16) & 0x3FF);
            int end = (int)(HStart & 0x3FF);
            int res = end - start;
            int scale = (int)(XScale & 0xFFF);
            int offset = (int)((XScale >> 16) & 0x3FF);

            //start -= 108;

            return res * scale / 1024;
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

        public uint Interrupt => m_Interrupt.RegisterValue;

        public uint Line {
            get => m_CurrentLine.RegisterValue;
            set => m_CurrentLine.RegisterValue = value;
        }

        public VideoControlReg ControlReg => m_ControlRegStruct;

        public bool IsVideoActive => (m_ControlReg.RegisterValue & 3) != 0 && m_Interrupt.RegisterValue > 0;

        public IntPtr FramebufferPtr => m_Memory.RDRAM.GetRamPointer(ReadFramebufferAddressSafe());

        // public unsafe void CopyFramebufferRGB5551_16(UnmanagedBuffer buffer)
        // {
        //     if (!IsVideoActive) return;

        //     // Converts RGB5551 to RGB565

        //     ushort* srcPixel = (ushort*)m_Memory.RDRAM.GetRamPointer(ReadFramebufferAddressSafe());
        //     ushort* dstPixel = (ushort*)buffer.GetPointer();

        //     for (int i = 0; i < buffer.Size / 2; i++)
        //     {
        //         ushort read = *srcPixel++;
        //         int pixel = 0;

        //        pixel = read.ByteSwapped();

        //         int r = (pixel >> 11) & 0x1F;
        //         int g = (pixel >> 6)  & 0x1F;
        //         int b = (pixel >> 1)  & 0x1F;

        //         // Sign extend the green field to the 6th bit
        //         g |= (g & 0x10) << 1;

        //         *dstPixel++ = (ushort)((r << 11) | (g << 6) | b);
        //     }
        // }

        // public unsafe void CopyFramebufferRGB5551_32(UnmanagedBuffer buffer)
        // {
        //     if (!IsVideoActive) return;

        //     ushort* srcPixel = (ushort*)m_Memory.RDRAM.GetRamPointer(ReadFramebufferAddressSafe());
        //     uint* dstPixel = (uint*)buffer.GetPointer();

        //     for (int i = 0; i < buffer.Size / 2; i++)
        //     {
        //         ushort read = *srcPixel++;
        //         int pixel = read;

        //         //pixel = read.ByteSwapped();

        //         // Transparancy Toggle
        //         // XXX: This prevents SkiaSharp from crashing
        //         // XXX: This breaks some rendering
        //         // XXX: Forcing alpha to 1 won't help either
        //         // if ((pixel & 1) == 0) {
        //         //     *dstPixel++ = 0;
        //         //     return;
        //         // }

        //         int r = (pixel >> 8) & 0xF8;
        //         int g = (pixel & 0x7C0) >> 3;
        //         int b = (pixel & 0x3E) << 2;

        //         int color = (b << 16) | (g << 8) | r;

        //         *dstPixel++ = (uint)color;
        //     }
        // }

        // public unsafe void CopyFramebufferRGBA8888(UnmanagedBuffer buffer)
        // {
        //     if (!IsVideoActive) return;

        //     uint* srcPixel = (uint*)m_Memory.RDRAM.GetRamPointer(ReadFramebufferAddressSafe());
        //     uint* dstPixel = (uint*)buffer.GetPointer();

        //     for (int i = 0; i < buffer.Size / 4; i++)
        //     {
        //         uint pixel = *srcPixel++;

        //        // pixel = pixel.ByteSwapped();

        //         *dstPixel++ = pixel;
        //     }
        // }

        // public void ScanlineStart() {
        //     m_CurrentLine.RegisterValue = 0;
        // }

        // public void ScanlineEnd() {
        //     m_CurrentLine.RegisterValue = m_Interrupt.RegisterValue;
        // }

        public void SimulateFullScan() {
            if (!IsVideoActive) return;

            for (m_CurrentLine.RegisterValue = 0; m_CurrentLine.RegisterValue < m_Interrupt.RegisterValue; m_CurrentLine.RegisterValue++) {
                //Thread.Sleep(1);
            }
        }

        public void SetVideoInterrupt() {
            m_Interface.SetInterrupt(MipsInterface.INT_VI, true);
        }

        public void ClearVideoInterrupt() {
            m_Interface.ClearInterrupt(MipsInterface.INT_VI);
        }

        internal void SetFBWidthFromRDP(int framebufferWidth)
        {
            m_RdpFramebufferWidth = framebufferWidth;
        }

        public void SetFBFromRDP(uint framebufferAddress)
        {
            // Enable this to directly display where RDP is rasterizing to
            // m_RdpWait.Reset();
            // m_RdpAddress = framebufferAddress;
            // m_UseRdp = true;
            // m_RdpWait.Set();
        }

        public void FakeCurrentLine() {
            m_CurrentLine.RegisterValue = m_Interrupt.ReadonlyRegisterValue;
        }
    }
}
