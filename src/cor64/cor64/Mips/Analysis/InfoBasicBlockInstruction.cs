using System;
using System.Collections.Generic;

namespace cor64.Mips.Analysis
{
    public class InfoBasicBlockInstruction
    {
        private ulong m_Address;
        private BinaryInstruction m_Inst;
        private String m_Disassembly;
        private List<MemAcess> m_AddressAccessList;

        public struct MemAcess
        {
            public ulong address;
            public string note;
        }

        public InfoBasicBlockInstruction(BaseDisassembler disassembler, DecodedInstruction inst)
        {
            m_Inst = inst.Inst;
            m_Disassembly = disassembler.GetFullDisassembly(inst);
            m_Address = inst.Address;
            m_AddressAccessList = new List<MemAcess>();
        }

        public void AppendMemoryAccess(ulong address, string note)
        {
            m_AddressAccessList.Add(new MemAcess() { address = address, note = note });
        }

        public IReadOnlyList<MemAcess> MemAccessList => m_AddressAccessList;

        public BinaryInstruction Inst => m_Inst;

        public String Disasm => m_Disassembly;

        public ulong Address => m_Address;

        public long TickCount {
            get;
            set; }

        public override string ToString()
        {
            return String.Format("MIPS 0x{0:X8}: 0x{1:X8} {2} ({3} ms)", m_Address, m_Inst.inst, m_Disassembly, TickCount);
        }
    }
}
