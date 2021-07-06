using System.Numerics;
using System;
using ImGuiNET;

namespace RunN64.Graphics {
    public static class ImGuiHelper {
        public static void DrawTextWithBackground(this ImDrawListPtr drawListPtr, Vector4 textColor, Vector4 bgColor, String text) {
            Vector2 bgMin = ImGui.GetCursorScreenPos();
            Vector2 bgMax = new Vector2(bgMin.X + (8 * text.Length), bgMin.Y + ImGui.GetTextLineHeight());

            drawListPtr.AddRectFilled(bgMin, bgMax, ImGui.ColorConvertFloat4ToU32(bgColor));
            ImGui.TextColored(textColor, text);
        }
    }
}