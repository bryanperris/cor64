using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RunN64.Graphics
{
    public static class GuiCommon
    {
        public static readonly String FONT_FIRA_REG = "FiraCode-Regular.ttf";
        public static readonly String FONT_FIRA_BOLD = "FiraCode-Bold.ttf";

        private static bool s_FontLoaded = false;
        
        public static SKTypeface NormalFont;
        public static SKTypeface BoldFont;

        private static SKPaint s_HackerPaint;

        public static void LoadFonts() {
            if (!s_FontLoaded) {
                NormalFont = LoadEmbeddedFont(FONT_FIRA_REG);
                BoldFont = LoadEmbeddedFont(FONT_FIRA_BOLD);
                s_FontLoaded = true;
            }
        }

        public static SKPaint GetHackerPaint() {
            if (s_HackerPaint == null) {
                LoadFonts();

                s_HackerPaint = new SKPaint()
                {
                    Color = SKColors.Lime,
                    IsAntialias = true,
                    Style = SKPaintStyle.Fill,
                    TextAlign = SKTextAlign.Left,
                    TextSize = 14,
                    Typeface = BoldFont
                };
            }

            return s_HackerPaint;
        }

        public static Stream OpenResourceStream(Type type, String name)
        {
            return type.Assembly.GetManifestResourceStream(type, name);
        }

        public static SKTypeface LoadEmbeddedFont(String name)
        {
            using var source = OpenResourceStream(typeof(GuiCommon), name);
            byte[] buffer = new byte[source.Length];
            source.Position = 0;
            source.Read(buffer, 0, buffer.Length);
            var skSource = SKData.CreateCopy(buffer);
            return SKTypeface.FromData(skSource);
        }
    }
}
