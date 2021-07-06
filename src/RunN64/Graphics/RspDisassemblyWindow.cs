using System;
using cor64;
using cor64.Mips.Rsp;
using ImGuiNET;

namespace RunN64.Graphics {
    public sealed class RspDisassemblyWindow : ImGuiWindow
    {
        private readonly RspInterpreter m_Rsp;
        private readonly DisassemblerView m_Disasm;

        public RspDisassemblyWindow(N64System system) : base("RSP Disassembly")
        {
            m_Rsp = (RspInterpreter)system.DeviceRcp.DeviceRsp;
            m_Disasm = new DisassemblerView(system, m_Rsp.Disassembler);

            m_Disasm.OnBreak += () => {
                m_Disasm.PC = m_Rsp.ReadPC();
            };
        }

        protected override void BuildComponents()
        {
            m_Disasm.Build();
        }
    }
}