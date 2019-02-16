using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using cor64.Mips;
using SkiaSharp;
using SkiaSharp.Views.Desktop;

namespace RunN64.Forms.EmuControls
{
    public class DisassemblyView : SKControl
    {
        private Timer m_UpdateTimer;
        private BaseInterpreter m_Core;
        private List<DecodedInstruction> m_DecodedList = new List<DecodedInstruction>();
        private int m_DisplayLength = -1;
        private const float ScaleFactor = 1.0f;

        public DisassemblyView() : base()
        {
            m_UpdateTimer = new Timer()
            {
                Interval = 100,
                Enabled = true
            };

            m_UpdateTimer.Tick += TimerTick;
        }

        public void AttachSystem(BaseInterpreter baseInterpreter)
        {
            m_Core = baseInterpreter;
            Refresh();
        }

        void TimerTick(object sender, EventArgs e)
        {
            Refresh();
        }

        protected override void OnSizeChanged(EventArgs e)
        {
            base.OnSizeChanged(e);
        }

        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            var canvas = e.Surface.Canvas;
            var scaledSize = new SKSize(e.Info.Width / ScaleFactor, e.Info.Height / ScaleFactor);
            canvas.Scale(ScaleFactor);
            canvas.Clear(SKColors.Black);

            if (m_Core != null)
            {
                //var list = m_Core.TraceLog.PeekLast();

                //for (int i = 0; i < list.Count; i++)
                //{
                //    canvas.DrawText(list[i].Disassembly, new SKPoint(0, ((i + 1) * HackerPaint.TextSize)), HackerPaint);
                //}
            }
            else
            {
                canvas.DrawText("No Core Attached", new SKPoint(0, GuiCommon.HackerPaint.TextSize), GuiCommon.HackerPaint);
            }
        }
    }
}
