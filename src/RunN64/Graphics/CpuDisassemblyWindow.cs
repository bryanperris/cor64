using System;
using cor64;
using cor64.Mips.R4300I;
using ImGuiNET;

namespace RunN64.Graphics {
    public sealed class CpuDisassemblyWindow : ImGuiWindow
    {
        private readonly InterpreterBaseR4300I m_Cpu;
        private readonly DisassemblerView m_Disasm;

        public CpuDisassemblyWindow(N64System system) : base("CPU Disassembly")
        {
            m_Cpu = system.DeviceCPU;
            m_Disasm = new DisassemblerView(system, m_Cpu.Disassembler);

            m_Disasm.OnBreak += () => {
                m_Disasm.PC = m_Cpu.ReadPC();
            };
        }

        protected override void BuildComponents()
        {
            m_Disasm.Build();
        }
    }
}