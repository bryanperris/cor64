using cor64.Mips.Analysis;
using cor64.Mips.JitCommon;
using cor64.Mips.R4300I.JitIL;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.Mips.R4300I
{
    public class RuntimeBasicBlock : BasicBlock<DecodedInstruction>
    {
        private readonly RegUsageCollector m_RegUsageCollector;

        public RuntimeBasicBlock(BaseDisassembler disassembler, long address) : base(address)
        {
            m_RegUsageCollector = new RegUsageCollector(disassembler);
        }

        public override void Append(DecodedInstruction instRecord)
        {
            m_RegUsageCollector.Collect(instRecord);
            base.Append(instRecord);
        }

        public RegUsageCollector RegUsage => m_RegUsageCollector;

        public EmitterRegisterMap GenerateRegAllocationMap(MipsILEmitter emitter, int localOffset) {
            return new EmitterRegisterMap(emitter, m_RegUsageCollector, localOffset);
        }
    }
}
