using System;
using System.Windows.Forms;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System.Drawing;
using System.ComponentModel;

namespace RunN64Forms
{
    [DesignTimeVisible(false)]
    public class SkiaTestWindow : Form
    {
        private SKControl m_SKView;

        public SkiaTestWindow()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            SuspendLayout();

            m_SKView = new SKControl
            {
                Location = new Point(0, 0),
                Size = new Size(100, 100),
                Dock = DockStyle.Fill
            };

            m_SKView.PaintSurface += PaintSurface;
            Controls.Add(m_SKView);

            ResumeLayout(false);
        }

        void PaintSurface(object sender, SKPaintSurfaceEventArgs e)
        {
            // the the canvas and properties
            var canvas = e.Surface.Canvas;

            // get the screen density for scaling
            var scale = 1f;
            var scaledSize = new SKSize(e.Info.Width / scale, e.Info.Height / scale);

            // handle the device screen density
            canvas.Scale(scale);

            // make sure the canvas is blank
            canvas.Clear(SKColors.White);

            // draw some text
            var paint = new SKPaint
            {
                Color = SKColors.Black,
                IsAntialias = true,
                Style = SKPaintStyle.Fill,
                TextAlign = SKTextAlign.Center,
                TextSize = 24
            };

            var coord = new SKPoint(scaledSize.Width / 2, (scaledSize.Height + paint.TextSize) / 2);
            canvas.DrawText("SkiaSharp", coord, paint);
        }

    }
}
