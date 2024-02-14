using System;
using cor64;
using cor64.Mips;
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

    public class CpuDisassemblyView : DisassemblerView
    {
        public CpuDisassemblyView(N64System system, BaseDisassembler disassembler) : base(system, disassembler)
        {
        }

        protected override ulong? TranslateAddress(ulong vaddr)
        {
            /* Kernel 32-bit memory addressing */
            byte index = (byte)((uint)vaddr >> 28);

            return index switch
            {
                0x0 or 0x1 or 0x2 or 0x3 or 0x4 or 0x5 or 0x6 or 0x7 => null,
                0x8 or 0x9 => vaddr - 0x80000000,
                0xA => vaddr & 0x1FFFFFFF,
                0xB => vaddr & 0x1FFFFFFF,
                0xC or 0xD or 0xE or 0xF => null,
                _ => null,
            };
        }
    }
}