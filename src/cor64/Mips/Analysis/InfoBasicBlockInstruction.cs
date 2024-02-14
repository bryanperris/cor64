using System.Data;
using cor64.Debugging;
using System;
using System.Collections.Generic;
using System.Text;

namespace cor64.Mips.Analysis
{
    public class InfoBasicBlockInstruction
    {
        private readonly InfoBasicBlock m_Parent;

        public InfoBasicBlockInstruction(InfoBasicBlock parentBlock, long address, DecodedInstruction inst)
        {
            m_Parent = parentBlock;
            Inst = inst;
            Address = address;
        }

        public DecodedInstruction Inst { get; }

        public long Address { get; }

        public override string ToString()
        {
            return String.Format("{0:X8} {1}", Address, m_Parent.Disassembler.Disassemble(Address, Inst));
        }
    }
}
