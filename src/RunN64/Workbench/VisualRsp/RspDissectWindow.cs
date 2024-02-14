using System.Net.Mime;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using cor64;
using cor64.Mips.Rsp;
using ImGuiNET;
using System.Numerics;
using RunN64.Graphics;

namespace RunN64.Workbench.VisualRsp
{
    public sealed class RspDissectWindow : ImGuiWindow
    {
        private const int NUM_VECTORS = 32;
        private readonly N64System m_System;
        private readonly RspInterpreter m_Rsp;

        private static readonly Vector4 COLOR_VECTOR = new Vector4(0, 1, 0, 1.0f);

        private readonly VisualRsp.Vector[] m_VisualVectors = new VisualRsp.Vector[NUM_VECTORS];

        public RspDissectWindow(N64System system) : base("RSP Dissect")
        {
            m_System = system;
            m_Rsp = (RspInterpreter)m_System.DeviceRcp.DeviceRsp;
        }

        private void Prepare() {
            // Prepare the visuals based on the RSP instruction
            
        }

        protected override void BuildComponents()
        {
            // if (m_System.Dbg.IsBreakActive) {
                
            // }

            // DrawVectorRegister(0);
        }

        private void DrawVectorRegister(int reg) {
            ImGui.TextColored(new Vector4(1, 1, 1, 1), "RSP Vector:");

            var drawList = ImGui.GetWindowDrawList();
            var min = ImGui.GetCursorScreenPos();

            int cellWidth = 18;

            for (int i = 0; i < 16; i++) {
                var colCursor = new Vector2(min.X, min.Y);

                drawList.AddLine(
                    colCursor,
                    new Vector2(colCursor.X, colCursor.Y + 13),
                    ImGui.ColorConvertFloat4ToU32(COLOR_VECTOR)
                );

                drawList.AddLine(
                    new Vector2(colCursor.X + (cellWidth - 1), colCursor.Y),
                    new Vector2(colCursor.X + (cellWidth - 1), colCursor.Y + 13),
                    ImGui.ColorConvertFloat4ToU32(COLOR_VECTOR)
                );

                drawList.AddText(
                    new Vector2(colCursor.X + 5, colCursor.Y),
                    ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1)),
                    i.ToString("X1")
                );

                colCursor.Y += ImGui.GetFontSize() + 1;

                var bgColor = COLOR_VECTOR;
                bgColor.W = 0.3f;
                drawList.AddRectFilled(
                    colCursor,
                    new Vector2(colCursor.X + cellWidth, colCursor.Y + 15),
                    ImGui.ColorConvertFloat4ToU32(bgColor)
                );

                drawList.AddRect(
                    colCursor,
                    new Vector2(colCursor.X + cellWidth, colCursor.Y + 15),
                    ImGui.ColorConvertFloat4ToU32(COLOR_VECTOR)
                );


                // colCursor.Y += 5;

                drawList.AddText(
                   new Vector2(colCursor.X + 2, colCursor.Y),
                    ImGui.ColorConvertFloat4ToU32(new Vector4(1, 1, 1, 1)),
                    "00"
                );

                min.X += 20;
            }
        }
    }
}