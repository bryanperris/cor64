using System.Numerics;
using System;
using ImGuiNET;
using System.Collections.Generic;

namespace RunN64.Graphics {
    public abstract class ImGuiWidget {

        public abstract void Build();

        protected static uint Color(float r, float g, float b) {
            return
                ImGui.ColorConvertFloat4ToU32(
                    new Vector4(r, g, b, 1.0f)
                );
        }

        protected static uint AdjustAlpha(uint color, float alpha) {
            Vector4 color4 = ImGui.ColorConvertU32ToFloat4(color);
            color4.W = alpha;
            return ImGui.ColorConvertFloat4ToU32(color4);
        }
    }
}