using SkiaSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RunN64.Forms
{
    public static class GuiCommon
    {
        public static readonly SKTypeface NormalFont = LoadEmbeddedFont("FiraCode-Regular.ttf");
        public static readonly SKTypeface BoldFont = LoadEmbeddedFont("FiraCode-Bold.ttf");

        public static Stream OpenResourceStream(Type type, String name)
        {
            return type.Assembly.GetManifestResourceStream(type, name);
        }

        public static SKTypeface LoadEmbeddedFont(String name)
        {
            return SKTypeface.FromStream(OpenResourceStream(typeof(Program), name));
        }

        public static readonly SKPaint HackerPaint = new SKPaint()
        {
            Color = SKColors.Lime,
            IsAntialias = true,
            Style = SKPaintStyle.Fill,
            TextAlign = SKTextAlign.Left,
            TextSize = 14,
            Typeface = BoldFont
        };
    }
}
