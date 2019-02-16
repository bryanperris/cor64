using cor64;
using cor64.RCP;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RunN64.Forms
{
    public class RawVideoRendererForm : Form
    {
        private SKControl m_SkiaControl;
        private SKGLControl m_GLSkiaControl;
        private Control m_RenderControl;
        private Video m_VideoInterface;
        private Timer m_VTimer;
        private byte[] m_FBData;
        private SKBitmap m_SourceBitmap;
        private SKBitmap m_FramebufferBitmap;
        private Cartridge m_Cart;

        private const int RES_X = 640;
        private const int RES_Y = 480;
        private const int BAR_HEIGHT = 20;

        public RawVideoRendererForm(Video videoInterface, Cartridge cart)
        {
            m_VideoInterface = videoInterface;
            m_Cart = cart;
            Width = RES_X + 20;
            Height = RES_Y + BAR_HEIGHT + 41;
            m_FBData = new byte[RES_X * RES_Y * 4];
            m_SourceBitmap = null;
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Text = "N64 Framebuffer";
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            if (Environment.GetEnvironmentVariable("NOVIDEO") == null)
            {
                if (Environment.OSVersion.Platform == PlatformID.Unix)
                {
                    m_SkiaControl = new SKControl()
                    {
                        Location = new Point(0, 0),
                        Size = this.Size,
                        Dock = DockStyle.Fill
                    };

                    m_SkiaControl.PaintSurface += PaintSurface;
                    Controls.Add(m_SkiaControl);

                    m_RenderControl = m_SkiaControl;
                }
                else
                {
                    m_GLSkiaControl = new SKGLControl()
                    {
                        Location = new Point(0, 0),
                        Size = this.Size,
                        Dock = DockStyle.Fill
                    };

                    m_GLSkiaControl.PaintSurface += GLPaintSurface;
                    Controls.Add(m_GLSkiaControl);

                    m_RenderControl = m_GLSkiaControl;
                }
            }
            else
            {
                Size = new Size(1, 1);
            }

            m_VTimer = new Timer()
            {
                Interval = 17,
                Enabled = true
            };

            m_VTimer.Tick += TimerTickHandler;

            ResumeLayout(false);
        }

        private void GLPaintSurface(object sender, SKPaintGLSurfaceEventArgs e)
        {
            RenderN64Framebuffer(e.Surface.Canvas);
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

                m_SourceBitmap = new SKBitmap(m_VideoInterface.Width, m_VideoInterface.Height, SKColorType.Rgba8888, SKAlphaType.Unpremul);
            }
        }

        private void TimerTickHandler(object sender, EventArgs e)
        {
            switch (m_VideoInterface.ControlReg.GetPixelMode()) {
                default:
                    {
                        if (m_SourceBitmap != null)
                            m_SourceBitmap.Dispose();

                        m_SourceBitmap = null;
                        break;
                    }
                case VideoControlReg.PIXELMODE_16BPP: ReadRGB555(); break;
                case VideoControlReg.PIXRLMODE_32BPP: ReadRGBA8888(); break;
            }

            if (m_SourceBitmap != null)
            {
                unsafe
                {
                    fixed (byte* ptr = m_FBData)
                    {
                        m_SourceBitmap.SetPixels(new IntPtr(ptr));
                    }
                }

                m_FramebufferBitmap = m_SourceBitmap;
            }
            else
            {
                if (m_FramebufferBitmap != null)
                    m_FramebufferBitmap.Dispose();

                m_FramebufferBitmap = null;
            }

            if (m_RenderControl != null)
                m_RenderControl.Invalidate();
        }

        void PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            RenderN64Framebuffer(e.Surface.Canvas);
        }

        private void DrawString(String str, SKCanvas canvas, int x, int y)
        {
            canvas.DrawText(str, new SKPoint(x, GuiCommon.HackerPaint.TextSize + y), GuiCommon.HackerPaint);
        }

        private int StrLen(String text)
        {
            return (int)GuiCommon.HackerPaint.TextSize * text.Length;
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

                switch (m_FramebufferBitmap.Info.ColorType) {
                    default: break;
                    case SKColorType.Rgb565: colorMode = "16BPP"; break;
                    case SKColorType.Rgba8888: colorMode = "32BPP"; break;
                }

                strPos += StrLen(res);

                DrawString(colorMode, canvas, strPos, 0);

                if (m_SourceBitmap.Width < RES_X || m_SourceBitmap.Height < RES_Y)
                {
                    float sf = (float)RES_Y / (float)m_SourceBitmap.Height;
                    canvas.Scale(sf);
                }

                canvas.DrawBitmap(m_FramebufferBitmap, new SKPoint(2, BAR_HEIGHT));
            }

            canvas.Flush();
        }
    }
}
