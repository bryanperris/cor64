using System.IO;
using System.Reflection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RunN64.External.GLide64;
using M = RunN64.External.GLide64.ZilmarGFX_1_3_Cor64;
using ERROR = RunN64.External.GLide64.M64pError;
using NLog;
using cor64.Mips;
using cor64.RCP;
using cor64.HLE;
using System.Runtime.InteropServices;
using System.Threading;

// HLE Graphics base class
// This kind of class hooks itself into RSP Gfx tasks
// RSP graphics routine is handled
// RDP is basically bypassed, host GPU is directed used

namespace RunN64
{
    public class ModGLide64 : GraphicsHLEDevice
    {
        private readonly static Logger Log = LogManager.GetCurrentClassLogger();

        private readonly COR64GFX_INFO m_GfxInfo = new COR64GFX_INFO();
        private readonly MUPEN_VID_EXT m_VidExt = new MUPEN_VID_EXT();

        private MipsInterface m_RcpInterface;
        private DPCInterface m_RdpInterface;


        private readonly PFunc m_Callback_CheckInterrupts;

        private readonly PtrVidExtInit m_Callback_CoreVideoInit;
        private readonly PtrVidExtQuit m_Callback_CoreVideoQuit;
        private readonly PtrVidExtListFullscreenModes m_Callback_CoreVideoListFullscreenModes;
        private readonly PtrVidExtListFullscreenRates m_Callback_CoreVideoListFullscreenRates;
        private readonly PtrVidExtSetVideoMode m_Callback_CoreVideoSetVideoMode;
        private readonly PtrVidExtSetVideoModeWithRate m_Callback_CoreVideoSetVideoModeWithRate;
        private readonly PtrVidExtSetCaption m_Callback_CoreVideoSetCaption;
        private readonly PtrVidExtToggleFullScreen m_Callback_CoreVideoToggleFullScreen;
        private readonly PtrVidExtResizeWindow m_Callback_CoreVideoResizeWindow;
        private readonly PtrVidExtGL_GetProcAddress m_Callback_CoreVideoGL_GetProcAddress;
        private readonly PtrVidExtGL_SetAttribute m_Callback_CoreVideoGL_SetAttribute;
        private readonly PtrVidExtGL_GetAttribute m_Callback_CoreVideoGL_GetAttribute;
        private readonly PtrVidExtGL_SwapBuffers m_Callback_CoreVideoGL_SwapBuffers;
        private readonly PtrVidExtGL_GetDefaultFramebuffer m_Callback_CoreVideoGL_GetDefaultFramebuffer;

        private uint m_DefaultFramebuffer = 0;
        private GLGetProcAddress m_GetProcAddress;
        private GLSwapBuffers m_SwapBuffers;

        public unsafe ModGLide64() {
            m_Callback_CheckInterrupts = new PFunc(CheckInterrupts);
            m_Callback_CoreVideoInit = new PtrVidExtInit(VideoExt_Init);
            m_Callback_CoreVideoQuit = new PtrVidExtQuit(VideoExt_Quit);
            m_Callback_CoreVideoListFullscreenModes = new PtrVidExtListFullscreenModes(VideoExt_ListFullscreenModes);
            m_Callback_CoreVideoListFullscreenRates = new PtrVidExtListFullscreenRates(VideoExt_ListFullscreenRates);
            m_Callback_CoreVideoSetVideoMode = new PtrVidExtSetVideoMode(VideoExt_SetVideoMode);
            m_Callback_CoreVideoSetVideoModeWithRate = new PtrVidExtSetVideoModeWithRate(VideoExt_SetVideoModeWithRate);
            m_Callback_CoreVideoSetCaption = new PtrVidExtSetCaption(VideoExt_SetCaption);
            m_Callback_CoreVideoToggleFullScreen = new PtrVidExtToggleFullScreen(VideoExt_ToggleFullscreen);
            m_Callback_CoreVideoResizeWindow = new PtrVidExtResizeWindow(VideoExt_ResizeWindow);
            m_Callback_CoreVideoGL_GetProcAddress = new PtrVidExtGL_GetProcAddress(VideoExt_GL_GetProcAddress);
            m_Callback_CoreVideoGL_SetAttribute = new PtrVidExtGL_SetAttribute(VideoExt_GL_SetAttribute);
            m_Callback_CoreVideoGL_GetAttribute = new PtrVidExtGL_GetAttribute(VideoExt_GL_GetAttribute);
            m_Callback_CoreVideoGL_SwapBuffers = new PtrVidExtGL_SwapBuffers(VideoExt_GL_SwapBuffers);
            m_Callback_CoreVideoGL_GetDefaultFramebuffer = new PtrVidExtGL_GetDefaultFramebuffer(VideoExt_GetDefaultFramebuffer);
        }

        ~ModGLide64() {
            M.CloseDLL();
        }

        public unsafe void AttachRenderBackend() {

            m_VidExt.CoreVideoInit = m_Callback_CoreVideoInit;
            m_VidExt.CoreVideoQuit = m_Callback_CoreVideoQuit;
            m_VidExt.CoreVideoListFullscreenModes = m_Callback_CoreVideoListFullscreenModes;
            m_VidExt.CoreVideoListFullscreenRates = m_Callback_CoreVideoListFullscreenRates;
            m_VidExt.CoreVideoSetVideoMode = m_Callback_CoreVideoSetVideoMode;
            m_VidExt.CoreVideoSetVideoModeWithRate = m_Callback_CoreVideoSetVideoModeWithRate;
            m_VidExt.CoreVideoSetCaption = m_Callback_CoreVideoSetCaption;
            m_VidExt.CoreVideoToggleFullScreen = m_Callback_CoreVideoToggleFullScreen;
            m_VidExt.CoreVideoResizeWindow = m_Callback_CoreVideoResizeWindow;
            m_VidExt.CoreVideoGL_GetProcAddress = m_Callback_CoreVideoGL_GetProcAddress;
            m_VidExt.CoreVideoGL_SetAttribute = m_Callback_CoreVideoGL_SetAttribute;
            m_VidExt.CoreVideoGL_GetAttribute = m_Callback_CoreVideoGL_GetAttribute;
            m_VidExt.CoreVideoGL_SwapBuffers = m_Callback_CoreVideoGL_SwapBuffers;
            m_VidExt.CoreVideoGL_GetDefaultFramebuffer = m_Callback_CoreVideoGL_GetDefaultFramebuffer;

            M.ConfigurePlugin(m_VidExt);
        }

        /* Attachments
           window = the OpenGL render window
           rcpInterface: RCP Interface
           spInterface: RSP Interface
           dpcInterface: RDP Command Interface
           video: Video Interface
        */
        public unsafe override void AttachInterface(
            IntPtr window,
            IntPtr cartridgePtr,
            MipsInterface rcpInterface, 
            SPInterface iface, 
            DPCInterface rdpInterface, 
            Video videoInterface) {

            m_RcpInterface = rcpInterface;
            m_RdpInterface = rdpInterface;

            // Window stuff
            m_GfxInfo.HWnd = window;
            m_GfxInfo.HStatusBar = IntPtr.Zero;

            m_GfxInfo.MemoryBswaped = 1;

            // TODO: Will need to byte swap to littl-endian in Glide64?
            var rspExports = iface.ExportPointers();
            m_GfxInfo.IMEM = (byte*)rspExports.IMEMPtr;
            m_GfxInfo.DMEM = (byte*)rspExports.DMEMPtr;
            m_GfxInfo.RDRAM = (byte*)rspExports.RDRAMPtr;
            m_GfxInfo.SP_STATUS = (uint*)rspExports.StatusPtr;

            m_GfxInfo.HEADER = (byte*)cartridgePtr;

            m_GfxInfo.CheckInterrupts = m_Callback_CheckInterrupts;

            m_GfxInfo.MI_INTR_REG = (uint*)rcpInterface.ExportInterruptPtr();

            var rdpExports = rdpInterface.ExportPointers();

            m_GfxInfo.DPC_BUFBUSY_REG = (uint*)rdpExports.BufferBusyCounterPtr;
            m_GfxInfo.DPC_CLOCK_REG = (uint*)rdpExports.ClockPtr;
            m_GfxInfo.DPC_CURRENT_REG = (uint*)rdpExports.CurrentPtr;
            m_GfxInfo.DPC_END_REG = (uint*)rdpExports.EndPtr;
            m_GfxInfo.DPC_PIPEBUSY_REG = (uint*)rdpExports.PipeBusyCounterPtr;
            m_GfxInfo.DPC_START_REG = (uint*)rdpExports.StartPtr;
            m_GfxInfo.DPC_STATUS_REG = (uint*)rdpExports.StatusPtr;
            m_GfxInfo.DPC_TMEM_REG = (uint*)rdpExports.TmemLoadCounterPtr;

            var videoExports = videoInterface.ExportPointers();

        	m_GfxInfo.VI_STATUS_REG = (uint*)videoExports.Status;
        	m_GfxInfo.VI_ORIGIN_REG = (uint*)videoExports.Origin;
        	m_GfxInfo.VI_WIDTH_REG = (uint*)videoExports.Width;
        	m_GfxInfo.VI_INTR_REG = (uint*)videoExports.Interrupt;
        	m_GfxInfo.VI_V_CURRENT_LINE_REG = (uint*)videoExports.CurrentLine;
        	m_GfxInfo.VI_TIMING_REG = (uint*)videoExports.Timing;
        	m_GfxInfo.VI_V_SYNC_REG = (uint*)videoExports.VSync;
        	m_GfxInfo.VI_H_SYNC_REG = (uint*)videoExports.HSync;
        	m_GfxInfo.VI_LEAP_REG = (uint*)videoExports.Leap;
        	m_GfxInfo.VI_H_START_REG = (uint*)videoExports.HStart;
        	m_GfxInfo.VI_V_START_REG = (uint*)videoExports.VStart;
        	m_GfxInfo.VI_V_BURST_REG = (uint*)videoExports.VBurst;
        	m_GfxInfo.VI_X_SCALE_REG = (uint*)videoExports.XScale;
        	m_GfxInfo.VI_Y_SCALE_REG = (uint*)videoExports.YScale;

            M.InitiateGFX(m_GfxInfo);
        }

        public override void Init() {
            M.RomOpen();
        }

        private readonly EventWaitHandle waitHandle = new EventWaitHandle(false, EventResetMode.ManualReset);

        public override void ExecuteGfxTask() {
            EnqueueRenderTask(() => {
                M.ProcessDList();
                waitHandle.Set();
            });

            waitHandle.WaitOne();
            waitHandle.Reset();
        }

        public override void Render() {
            M.DrawScreen();
        }

        private void CheckInterrupts() {
            // m_RcpInterface.CheckInterrupts();
            // m_RdpInterface.ReflectStatus();
        }

        public override void AttachGL(uint defaultFramebuffer, GLGetProcAddress getProcAddress, GLSwapBuffers swapBuffers)
        {
            m_DefaultFramebuffer = defaultFramebuffer;
            m_GetProcAddress = getProcAddress;
            m_SwapBuffers = swapBuffers;
        }

        public override string Description => "GLide64";

        private ERROR VideoExt_Init() {
            Console.WriteLine("Video Init");
            return ERROR.M64ERR_SUCCESS;
        }

        private ERROR VideoExt_Quit() {
            Console.WriteLine("Video Quit");
            return ERROR.M64ERR_SUCCESS;
        }

        private unsafe ERROR VideoExt_ListFullscreenModes(IntPtr resolutionsPtr, int* length) {

            M64p2dSize.__Internal * resolutions = (M64p2dSize.__Internal *) resolutionsPtr;
            int len = *length;

            M64p2dSize.__Internal * end = resolutions + len;

            while (resolutions < end) {
                var res = *resolutions;
                Console.WriteLine("Fullscreen resolution available: {0}x{1}", res.uiWidth, res.uiHeight);
                resolutions++;
            }


            return ERROR.M64ERR_SUCCESS;
        }

        private unsafe ERROR VideoExt_ListFullscreenRates(M64p2dSize.__Internal size, int* length, int* rates) {
            int * end = rates + *length;

            Console.WriteLine("Fullscreen {0}x{1} rate(s)", size.uiWidth, size.uiHeight);

            while (rates < end) {
                var rate = *rates;
                Console.WriteLine("Rate available {0}", rate);
                rates++;
            }

            return ERROR.M64ERR_SUCCESS;
        }

        private ERROR VideoExt_SetVideoMode(int width, int height, int bpp, M64pVideoMode mode, M64pVideoFlags flags) {

            Console.WriteLine("Set video mode: {0}x{1} {2} {3} {4}", width, height, bpp, mode.ToString(), flags.ToString());

            return ERROR.M64ERR_SUCCESS;
        }

        private ERROR VideoExt_SetVideoModeWithRate(int width, int height, int rate, int bpp, M64pVideoMode mode, M64pVideoFlags flags) {

            Console.WriteLine("Set video mode: {0}x{1} {2} {3} {4} {5}", width, height, rate, bpp, mode.ToString(), flags.ToString());

            return ERROR.M64ERR_SUCCESS;
        }

        private ERROR VideoExt_ResizeWindow(int width, int height) {
            Console.WriteLine("Resize Window {0}x{1}", width, height);

            return ERROR.M64ERR_SUCCESS;
        }

        private ERROR VideoExt_SetCaption([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(CppSharp.Runtime.UTF8Marshaller))] string caption) {
            Console.WriteLine("Set Caption: {0}", caption);

            return ERROR.M64ERR_SUCCESS;
        }

        private ERROR VideoExt_ToggleFullscreen() {
            Console.WriteLine("Toggle Fullscreen");
            return ERROR.M64ERR_SUCCESS;
        }

        private unsafe M64pFunction VideoExt_GL_GetProcAddress([MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(CppSharp.Runtime.UTF8Marshaller))] string name) {
            Console.WriteLine("GL GetProcAddress: {0}", name);
            var glFunc = m_GetProcAddress(name);
            return (M64pFunction)Marshal.GetDelegateForFunctionPointer(glFunc, typeof(M64pFunction));
        }

        // https://wiki.libsdl.org/SDL_GL_SetAttribute
        private ERROR VideoExt_GL_SetAttribute(M64pGLattr attr, int value) {
            Console.WriteLine("GL SetAttribute: {0} {1:X8}", (uint)attr, value);
            return ERROR.M64ERR_SUCCESS;
        }

        // https://wiki.libsdl.org/SDL_GL_GetAttribute
        private unsafe ERROR VideoExt_GL_GetAttribute(M64pGLattr attr, int* value) {
            int val = 0;
            Console.WriteLine("GL GetAttribute: {0} {1:X8}", (uint)attr, val);
            *value = val;
            return ERROR.M64ERR_SUCCESS;
        }

        private ERROR VideoExt_GL_SwapBuffers() {
            // Console.WriteLine("GL Swap buffers");
            // EnqueueRenderTask(() => {
                // m_SwapBuffers();
            // });
            return ERROR.M64ERR_SUCCESS;
        }

        private uint VideoExt_GetDefaultFramebuffer() {
            Console.WriteLine("Get Default Framebuffer");
            return m_DefaultFramebuffer;
        }
    }
}