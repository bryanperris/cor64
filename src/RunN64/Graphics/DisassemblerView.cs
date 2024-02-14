using System;
using System.Collections.Generic;
using System.Numerics;
using cor64;
using cor64.IO;
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

        private long m_LastPC;

        public long PC { get; set; }

        protected const string EMPTY = "--------";

        private static readonly uint COLOR_BREAK_LINE = ImGui.ColorConvertFloat4ToU32(new(0.7f, 0, 0, 1.0f));

        protected BaseDisassembler Disassembler => m_Disassembler;

        public DisassemblerView(N64System system, BaseDisassembler disassembler) {
            m_System = system;
            m_Disassembler = disassembler;
        }

        protected virtual ulong? TranslateAddress(ulong vaddr) {
            return vaddr;
        }

        public override void Build()
        {
            // CLEANUP: fix this
            // var activeBreak = m_System.Dbg.IsBreakActive;
            // var drawList = ImGui.GetWindowDrawList();

            // OnBreak?.Invoke();

            // int num = (int)(ImGui.GetWindowHeight() / ImGui.GetTextLineHeightWithSpacing()) - 1;

            // var start = PC;
            // var byteSize = num * 4;
            // var end = PC + byteSize;

            // if (start < m_LastPC || start > (m_LastPC + byteSize)) {
            //     m_LastPC = start;
            // }

            // if (num > m_Lines.Count) {
            //     for (int i = 0; i <= (num - m_Lines.Count); i++) {
            //         m_Lines.Add(EMPTY);
            //     }
            // }

            // if (activeBreak) {
            //     for (int i = 0; i < num; i++)
            //     {
            //         var addr = m_LastPC + (uint)(i * 4);

            //         var physaddr = TranslateAddress(addr);

            //         var inst = m_Disassembler.Disassemble(addr);

            //         if (addr == PC) {
            //             Vector2 bgMin = ImGui.GetCursorScreenPos();
            //             Vector2 bgMax = new Vector2(bgMin.X + ImGui.GetWindowWidth(), bgMin.Y + ImGui.GetTextLineHeight());
            //             drawList.AddRectFilled(bgMin, bgMax, COLOR_BREAK_LINE);
            //         }

            //         var text = String.Format("{0:X8} {1} {2}",
            //             addr,
            //             physaddr != null ? inst.Inst.inst.ToString("X8") : EMPTY,
            //             (physaddr != null && inst.CanFullyDisassemble ) ? m_Disassembler.Disassemble(inst) : EMPTY
            //         );

            //         ImGui.Text(text);

            //         if (i < m_Lines.Count) {
            //             m_Lines[i] = text;
            //         }
            //     }
            // }
            // else {
            //     for (int i = 0; i < num; i++) {
            //         if (m_Lines.Count > 0 && i < m_Lines.Count)
            //         {
            //             ImGui.Text(m_Lines[i]);
            //         }
            //         else
            //         {
            //             ImGui.Text(EMPTY);
            //         }
            //     }
            // }
        }
    }
}