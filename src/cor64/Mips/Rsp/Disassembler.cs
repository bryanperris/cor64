using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cor64.IO;

namespace cor64.Mips.Rsp
{
    public class Disassembler : BaseDisassembler
    {
        public Disassembler(string abi) : base(abi)
        {
        }

        protected override Opcode DecodeOpcode(BinaryInstruction inst)
        {
            return DecodeTable.OpcodeLookup(inst);
        }
    }
}
