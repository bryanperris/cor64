using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Drawing.Text;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace RunN64.Forms
{
    public static class GuiCommon
    {
        public static readonly String FONT_FIRA_REG = "FiraCode-Regular.ttf";
        public static readonly String FONT_FIRA_BOLD = "FiraCode-Bold.ttf";
        public static readonly SKTypeface NormalFont = LoadEmbeddedFont(FONT_FIRA_REG);
        public static readonly SKTypeface BoldFont = LoadEmbeddedFont(FONT_FIRA_BOLD);

        public static Stream OpenResourceStream(Type type, String name)
        {
            return type.Assembly.GetManifestResourceStream(type, name);
        }

        public static SKTypeface LoadEmbeddedFont(String name)
        {
            return SKTypeface.FromStream(OpenResourceStream(typeof(Program), name));
        }

        public static void LoadEmbeddedFont(PrivateFontCollection privateFontCollection, String name)
        {
            using (var stream = OpenResourceStream(typeof(Program), name))
            {
                stream.Position = 0;
                byte[] buffer = new byte[(int)stream.Length];
                stream.Read(buffer, 0, buffer.Length);
                IntPtr memPtr = Marshal.AllocCoTaskMem(buffer.Length);
                Marshal.Copy(buffer, 0, memPtr, buffer.Length);
                privateFontCollection.AddMemoryFont(memPtr, buffer.Length);
            }
        }

        public static int FindFontInCollection(PrivateFontCollection collection, string name)
        {
            var families = collection.Families;

            for (int i = 0; i < families.Length; i++)
            {
                var family = families[i];

                if (family.Name == name)
                {
                    return i;
                }
            }

            return -1;
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
