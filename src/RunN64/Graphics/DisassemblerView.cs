using System;
using System.Collections.Generic;
using System.Numerics;
using cor64;
using cor64.Mips;
using cor64.Mips.Rsp;
using ImGuiNET;

namespace RunN64.Graphics {
    public class DisassemblerView : ImGuiWidget
    {
        private readonly RspInterpreter m_Core;
        private readonly BaseDisassembler m_Disassembler;
        private readonly N64System m_System;
        private readonly List<string> m_Lines = new List<string>();

        public event Action OnBreak;

        private ulong m_LastPC;

        public ulong PC { get; set; }

        private const string EMPTY = "--------";

        private static readonly uint COLOR_BREAK_LINE = ImGui.ColorConvertFloat4ToU32(new(0.7f, 0, 0, 1.0f));

        public DisassemblerView(N64System system, BaseDisassembler disassembler) {
            m_System = system;
            m_Disassembler = disassembler;
        }

        public override void Build()
        {
            var activeBreak = m_System.Dbg.IsBreakActive;
            var drawList = ImGui.GetWindowDrawList();

            OnBreak?.Invoke();

            int num = (int)(ImGui.GetWindowHeight() / ImGui.GetTextLineHeightWithSpacing()) - 1;

            var start = PC;
            var byteSize = (ulong)(num * 4);
            var end = PC + byteSize;

            if (start < m_LastPC || start > (m_LastPC + byteSize)) {
                m_LastPC = start;
            }

            if (num > m_Lines.Count) {
                for (int i = 0; i <= (num - m_Lines.Count); i++) {
                    m_Lines.Add(EMPTY);
                }
            }

            if (activeBreak) {
                for (int i = 0; i < num; i++)
                {
                    var addr = m_LastPC + (uint)(i * 4);

                    var inst = m_Disassembler.Disassemble(addr);

                    if (addr == PC) {
                        Vector2 bgMin = ImGui.GetCursorScreenPos();
                        Vector2 bgMax = new Vector2(bgMin.X + ImGui.GetWindowWidth(), bgMin.Y + ImGui.GetTextLineHeight());
                        drawList.AddRectFilled(bgMin, bgMax, COLOR_BREAK_LINE);
                    }

                    var text = String.Format("{0:X8} {1:X8} {2}",
                        addr,
                        inst.Inst.inst,
                        inst.CanFullyDisassemble ? m_Disassembler.GetFullDisassembly(inst) : EMPTY
                    );

                    ImGui.Text(text);

                    if (i < m_Lines.Count) {
                        m_Lines[i] = text;
                    }
                }
            }
            else {
                for (int i = 0; i < num; i++) {
                    if (m_Lines.Count > 0 && i < m_Lines.Count)
                    {
                        ImGui.Text(m_Lines[i]);
                    }
                    else
                    {
                        ImGui.Text(EMPTY);
                    }
                }
            }
        }
    }
}