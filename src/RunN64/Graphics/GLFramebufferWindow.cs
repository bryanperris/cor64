// #define USE_RGBA8888_FOR_16BPP

// XXX: We can convert RGB5551 to RGBA8888 instead of RGB565, which is faster
//      However this conversion is unstable, as SkiaSharp seems to get mad at the 1 bit alpha
//      We can check the 1 bit alpha and return 0, but it can break some 16-bit RDP rendering

using System.Text;
using cor64.IO;
using cor64.RCP;
using GLFW;
using SkiaSharp;
using cor64;
using System;
using System.Runtime.InteropServices;
using System.Drawing;
using RunN64;
using System.Threading;
using NLog;
using static cor64.RCP.DPCInterface;

namespace RunN64.Graphics
{
    public class GLFramebufferWindow
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly N64System m_System;
        private readonly NativeWindow m_Window;
        private readonly Video m_VideoInterface;
        private readonly DPCInterface m_RdpInterface;
        private readonly UnmanagedBuffer m_FBData;
        private SKBitmap m_SourceBitmap;
        private SKBitmap m_FramebufferBitmap;
        private int m_FramebufferColorMode = 0;
        private readonly Cartridge m_Cart;
        private const int RES_X = 640;
        private const int RES_Y = 480;
        private const int BAR_HEIGHT = 20;

        /* Must hold all of these references, as the dispose method crashes for skia */
        private readonly SKSurface m_Surface;
        private readonly GRContext m_SkiaContext;
        private readonly GRGlInterface m_GLInterface;
        private readonly Object m_GLContext;

        private bool m_Created = false;


        public GLFramebufferWindow(N64System system)
        {
            m_System = system;

            Glfw.WindowHint(Hint.ContextVersionMajor, 2);
            Glfw.WindowHint(Hint.ContextVersionMinor, 1);
            Glfw.WindowHint(Hint.Focused, true);
            Glfw.WindowHint(Hint.Resizable, false);

            m_Window = new NativeWindow(RES_X + 1, RES_Y + BAR_HEIGHT, "N64 Framebuffer");

            // Glfw.IconifyWindow(m_Window);

            m_FBData = new UnmanagedBuffer(RES_X * RES_Y * 4);
            m_SourceBitmap = null;
            m_VideoInterface = system.DeviceRcp.VideoInterface;
            m_RdpInterface = system.DeviceRcp.DisplayProcessorCommandInterface;
            m_Cart = system.AttachedCartridge;

            m_GLContext = GetNativeContext(m_Window);

            StringBuilder glFunctions = new StringBuilder();
            glFunctions.Append("GL Function Attachments: ");

            m_GLInterface = GRGlInterface.Create((name) => {
                /* Skip the egl ones, this prevents crashing */
                if (name.StartsWith("egl")) {
                    return IntPtr.Zero;
                }
                else {
                    glFunctions.Append(' ').Append(name);
                    return Glfw.GetProcAddress(name);
                }
            });

            //Log.Debug(glFunctions.ToString());

            m_SkiaContext = GRContext.CreateGl(m_GLInterface);
            m_Surface = GenerateSkiaSurface(m_SkiaContext, new Size(RES_X, RES_Y + BAR_HEIGHT));
        }

        public void Start() {
            m_Created = true;

            while (!m_Window.IsClosing)
            {
                Scan();
                Render();
                Thread.Sleep(17);
            }
        }

        public bool IsCreated => m_Created;

        private void Render()
        {
            RenderN64Framebuffer(m_Surface.Canvas);
            Glfw.SwapBuffers(m_Window);
            Glfw.PollEvents();
        }

        private static object GetNativeContext(NativeWindow nativeWindow)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return Native.GetWglContext(nativeWindow);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                // XServer
                return Native.GetGLXContext(nativeWindow);
                // Wayland
                //return Native.GetEglContext(nativeWindow);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return Native.GetNSGLContext(nativeWindow);
            }

            throw new PlatformNotSupportedException();
        }

        private static SKSurface GenerateSkiaSurface(GRContext skiaContext, Size surfaceSize)
        {
            var frameBufferInfo = new GRGlFramebufferInfo((uint)new UIntPtr(0), SKColorType.Rgba8888.ToGlSizedFormat());
            var backendRenderTarget = new GRBackendRenderTarget(surfaceSize.Width, surfaceSize.Height, 0, 8, frameBufferInfo);
            return SKSurface.Create(skiaContext, backendRenderTarget, GRSurfaceOrigin.BottomLeft, SKImageInfo.PlatformColorType);
        }

        private void ReadRGB555()
        {
            #if USE_RGBA8888_FOR_16BPP
            m_VideoInterface.CopyFramebufferRGB5551_32(m_FBData);
            #else
            m_VideoInterface.CopyFramebufferRGB5551_16(m_FBData);
            #endif

            if (m_SourceBitmap == null || m_VideoInterface.Width != m_SourceBitmap.Width || m_VideoInterface.Height != m_SourceBitmap.Height || m_FramebufferColorMode != VideoControlReg.PIXELMODE_16BPP)
            {
                m_SourceBitmap?.Dispose();

                #if USE_RGBA8888_FOR_16BPP
                m_SourceBitmap = new SKBitmap(m_VideoInterface.Width, m_VideoInterface.Height, SKColorType.Rgba8888, SKAlphaType.Opaque);
                #else
                m_SourceBitmap = new SKBitmap(m_VideoInterface.Width, m_VideoInterface.Height, SKColorType.Rgb565, SKAlphaType.Opaque);
                #endif

                m_FramebufferColorMode = VideoControlReg.PIXELMODE_16BPP;
            }
        }

        private void ReadRGBA8888()
        {
            m_VideoInterface.CopyFramebufferRGBA8888(m_FBData);

            if (m_SourceBitmap == null || m_VideoInterface.Width != m_SourceBitmap.Width || m_VideoInterface.Height != m_SourceBitmap.Height || m_FramebufferColorMode != VideoControlReg.PIXELMODE_32BPP)
            {
                m_SourceBitmap?.Dispose();

                m_SourceBitmap = new SKBitmap(m_VideoInterface.Width, m_VideoInterface.Height, SKColorType.Rgba8888, SKAlphaType.Opaque);
                m_FramebufferColorMode = VideoControlReg.PIXELMODE_32BPP;
            }
        }

        private void Scan()
        {
            if (m_VideoInterface.IsVideoActive && !m_RdpInterface.IsBusy) {
                switch (m_VideoInterface.ControlReg.GetPixelMode())
                {
                    default: break;
                    case VideoControlReg.PIXELMODE_16BPP: ReadRGB555(); break;
                    case VideoControlReg.PIXELMODE_32BPP: ReadRGBA8888(); break;
                }
            }
            else {
                Thread.Sleep(10);

                m_SourceBitmap?.Dispose();
                m_SourceBitmap = null;
                m_FramebufferColorMode = 0;
            }

            if (m_SourceBitmap != null)
            {
                m_SourceBitmap.SetPixels(m_FBData.GetPointer());
                //m_SourceBitmap.SetPixels(m_VideoInterface.FramebufferPtr);
                m_FramebufferBitmap = m_SourceBitmap;
            }
            else
            {
                m_FramebufferBitmap?.Dispose();
                m_FramebufferBitmap = null;
            }
        }

        public void TriggerVI()
        {
            m_VideoInterface.SetVideoInterrupt();
        }

        private static float ComputeScaleFactor(float srcW, float srcH, float dstW, float dstH)
        {
            float dstRatio = dstW / dstH;
            float srcRatio = srcW / srcH;

            if (dstRatio > srcRatio)
            {
                return dstH / srcH;
            }
            else
            {
                return dstW / srcW;
            }
        }

        private void DrawString(String str, SKCanvas canvas, int x, int y)
        {
            var paint = GuiCommon.GetHackerPaint();
            canvas.DrawText(str, new SKPoint(x, paint.TextSize + y), paint);
        }

        private int StrLen(String text)
        {
            return (int)GuiCommon.GetHackerPaint().TextSize * text.Length;
        }

        private void RenderN64Framebuffer(SKCanvas canvas)
        {
            int strPos = 0;

            canvas.Clear(SKColors.Black);

            String vidType = !m_VideoInterface.IsVideoActive ? "No Video" : m_Cart.Region.ToString();

            DrawString(vidType, canvas, strPos, 0);

            if (m_FramebufferBitmap != null)
            {
                String res = String.Format("{0}x{1}", m_SourceBitmap.Width, m_SourceBitmap.Height);
                String mipsCount = String.Format("MIPS TMR {0:X8}:{1:X8}", m_System.DeviceCPU.Cop0.TimerCount,  m_System.DeviceCPU.Cop0.TimerMax);

                strPos += StrLen(vidType) + 5;
                DrawString(res, canvas, strPos, 0);

                String colorMode = null;

                switch (m_FramebufferColorMode)
                {
                    default: break;
                    case VideoControlReg.PIXELMODE_16BPP: colorMode = "16BPP"; break;
                    case VideoControlReg.PIXELMODE_32BPP: colorMode = "32BPP"; break;
                }

                strPos += StrLen(res);

                DrawString(colorMode, canvas, strPos, 0);

                // var addr = m_VideoInterface.FramebufferOffset.ToString("X8");
                var addr = m_VideoInterface.ReadFramebufferAddressSafe().ToString("X8");

                strPos += StrLen(colorMode);

                DrawString(addr, canvas, strPos, 0);

                strPos += StrLen(addr);

                DrawString(mipsCount, canvas, strPos, 0);

                float sf = ComputeScaleFactor(m_SourceBitmap.Width, m_SourceBitmap.Height, RES_X, RES_Y);

                // DEBUG: Zoom in
                //sf = 18.0f;

                canvas.Scale(sf);

                canvas.DrawBitmap(m_FramebufferBitmap, new SKPoint(1, BAR_HEIGHT / sf));

                canvas.ResetMatrix();
            }

            // DEBUG: Move to specific area
            //canvas.Translate(-225.0f * 18.0f, -38.0f * 18.0f);

            canvas.Flush();
        }
    }
}