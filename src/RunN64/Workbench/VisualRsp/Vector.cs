using System.Numerics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ImGuiNET;
using RunN64.Graphics;

namespace RunN64.Workbench.VisualRsp
{
    public class Vector : ImGuiWidget
    {
        private const int SLICES = 16;
        private const float cellPadding = 17;
        private static readonly uint COLOR_BG = Color(0, 1, 0);
        private static readonly uint COLOR_FG = Color(1, 1, 1);
        private cor64.Mips.Rsp.RspVector m_Vector;

        private static readonly string[] LABELS;

        static Vector() {
            LABELS = new string[SLICES];

            for (int i = 0; i < SLICES; i++) {
                LABELS[i] = i.ToString("X1");
            }
        }

        public Vector()
        {

        }

        public override void Build()
        {
            for (int i = 0; i < SLICES; i++) {
                DrawSlice(i);
            }
        }

        private void DrawSlice(int position) {
            var drawList = ImGui.GetWindowDrawList();
            var cursor = new GfxCursor(ImGui.GetCursorScreenPos());

            drawList.AddLine(cursor, cursor.SizeY(13), COLOR_BG);
            cursor.SizeX(cellPadding);
            drawList.AddLine(cursor, cursor.SizeY(13), COLOR_BG);
            drawList.AddText(cursor.SizeX(5), COLOR_FG, LABELS[position]);
            cursor.SizeY(ImGui.GetFontSize() + 1);

            var backCursor = cursor.Clone();
            backCursor.SizeX(cellPadding).SizeY(15);
            drawList.AddRectFilled(cursor, backCursor, AdjustAlpha(COLOR_BG, 0.3f));
        }
    }
}