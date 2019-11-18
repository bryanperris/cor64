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

namespace RunN64.Graphics
{
    public class GLFramebufferWindow
    {
        private readonly NativeWindow m_Window;
        private readonly Video m_VideoInterface;
        private readonly PinnedBuffer m_FBData;
        private SKBitmap m_SourceBitmap;
        private SKBitmap m_FramebufferBitmap;
        private readonly Cartridge m_Cart;
        private int m_FrameCount;
        private const int RES_X = 640;
        private const int RES_Y = 480;
        private const int BAR_HEIGHT = 20;

        /* Must hold all of these references, as the dispose method crashes for skia */
        private readonly SKSurface m_Surface;
        private readonly GRContext m_SkiaContext;
        private readonly GRGlInterface m_GLInterface;
        private readonly Object m_GLContext;

        private ulong m_ViTime = 0;
        private bool m_Created = false;


        public GLFramebufferWindow(Video videoInterface, Cartridge cart) 
        {
            Glfw.WindowHint(Hint.ContextVersionMajor, 2);
            Glfw.WindowHint(Hint.ContextVersionMinor, 1);
            Glfw.WindowHint(Hint.Focused, true);
            Glfw.WindowHint(Hint.Resizable, false);

            m_Window = new NativeWindow(RES_X + 1, RES_Y + BAR_HEIGHT, "N64 Framebuffer");
        
            m_FBData = new PinnedBuffer(RES_X * RES_Y * 4);
            m_SourceBitmap = null;
            m_VideoInterface = videoInterface;
            m_Cart = cart;

            m_GLContext = GetNativeContext(m_Window);
            m_GLInterface = GRGlInterface.AssembleGlInterface(m_GLContext, (contextHandle, name) => Glfw.GetProcAddress(name));
            m_SkiaContext = GRContext.Create(GRBackend.OpenGL, m_GLInterface);
            m_Surface = GenerateSkiaSurface(m_SkiaContext, new Size(RES_X, RES_Y + BAR_HEIGHT));
        }

        public void Start() {
            m_ViTime = Glfw.TimerValue;

            m_Created = true;

            while (!m_Window.IsClosing)
            {
                if (Glfw.TimerValue - m_ViTime >= 17)
                {
                    Scan();
                    m_ViTime = Glfw.TimerValue;
                }
                
                Render();
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
            var frameBufferInfo = new GRGlFramebufferInfo((uint)new UIntPtr(0), GRPixelConfig.Rgba8888.ToGlSizedFormat());
            var backendRenderTarget = new GRBackendRenderTarget(surfaceSize.Width, surfaceSize.Height, 0, 8, frameBufferInfo);
            return SKSurface.Create(skiaContext, backendRenderTarget, GRSurfaceOrigin.BottomLeft, SKImageInfo.PlatformColorType);
        }

        private void ReadRGB555()
        {
            m_VideoInterface.CopyFramebufferRGB565(m_FBData);

            if (m_SourceBitmap == null || m_VideoInterface.Width != m_SourceBitmap.Width || m_VideoInterface.Height != m_SourceBitmap.Height || m_SourceBitmap.Info.ColorType != SKColorType.Rgb565)
            {
                if (m_SourceBitmap != null)
                    m_SourceBitmap.Dispose();

                m_SourceBitmap = new SKBitmap(m_VideoInterface.Width, m_VideoInterface.Height, SKColorType.Rgb565, SKAlphaType.Opaque);
            }
        }

        private void ReadRGBA8888()
        {
            m_VideoInterface.CopyFramebufferRGBA8888(m_FBData);

            if (m_SourceBitmap == null || m_VideoInterface.Width != m_SourceBitmap.Width || m_VideoInterface.Height != m_SourceBitmap.Height || m_SourceBitmap.Info.ColorType != SKColorType.Rgba8888)
            {
                if (m_SourceBitmap != null)
                    m_SourceBitmap.Dispose();

                m_SourceBitmap = new SKBitmap(m_VideoInterface.Width, m_VideoInterface.Height, SKColorType.Rgba8888, SKAlphaType.Premul);
            }
        }

        private void Scan()
        {
            switch (m_VideoInterface.ControlReg.GetPixelMode())
            {
                default:
                    {
                        if (m_SourceBitmap != null)
                            m_SourceBitmap.Dispose();

                        m_SourceBitmap = null;
                        break;
                    }
                case VideoControlReg.PIXELMODE_16BPP: ReadRGB555(); break;
                case VideoControlReg.PIXELMODE_32BPP: ReadRGBA8888(); break;
            }

            if (m_SourceBitmap != null)
            {
                m_SourceBitmap.SetPixels(m_FBData.GetPointer());
                //m_SourceBitmap.SetPixels(m_VideoInterface.FramebufferPtr);
                m_FramebufferBitmap = m_SourceBitmap;
            }
            else
            {
                if (m_FramebufferBitmap != null)
                    m_FramebufferBitmap.Dispose();

                m_FramebufferBitmap = null;
            }

            if (m_FrameCount >= 60)
            {
                TriggerVI();
            }
            else
                m_FrameCount++;
        }

        public void TriggerVI()
        {
            m_VideoInterface.SetVideoInterrupt();
            m_FrameCount = 0;
        }

        private static float ComputeScaleFactor(float srcW, float srcH, float dstW, float dstH)
        {
            float dstRatio = dstW / dstH;
            float srcRatio = srcW / srcH;
            float scaleFactor = 0.0f;

            if (dstRatio > srcRatio)
            {
                scaleFactor = dstH / srcH;
            }
            else
            {
                scaleFactor = dstW / srcW;
            }

            return scaleFactor;
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

            String vidType = m_SourceBitmap == null ? "No Video" : m_Cart.Region.ToString();

            DrawString(vidType, canvas, strPos, 0);

            if (m_FramebufferBitmap != null)
            {
                String res = String.Format("{0}x{1}", m_SourceBitmap.Width, m_SourceBitmap.Height);

                strPos += StrLen(vidType) + 5;
                DrawString(res, canvas, strPos, 0);

                String colorMode = null;

                switch (m_FramebufferBitmap.Info.ColorType)
                {
                    default: break;
                    case SKColorType.Rgb565: colorMode = "16BPP"; break;
                    case SKColorType.Rgba8888: colorMode = "32BPP"; break;
                }

                strPos += StrLen(res);

                DrawString(colorMode, canvas, strPos, 0);

                var addr = m_VideoInterface.FramebufferOffset.ToString("X8");

                strPos += StrLen(colorMode);

                DrawString(addr, canvas, strPos, 0);

                float sf = ComputeScaleFactor(m_SourceBitmap.Width, m_SourceBitmap.Height, RES_X, RES_Y);

                canvas.Scale(sf);

                canvas.DrawBitmap(m_FramebufferBitmap, new SKPoint(1, BAR_HEIGHT / sf));

                canvas.ResetMatrix();
            }

            canvas.Flush();
        }
    }
}