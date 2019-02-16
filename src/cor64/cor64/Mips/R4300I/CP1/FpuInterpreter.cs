using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static cor64.Mips.R4300I.Opcodes;

namespace cor64.Mips.R4300I.CP1
{
    public abstract class FpuInterpreter
    {
        private Action<DecodedInstruction>[] m_CallTable;
        private Interpreter m_Cpu;

        public FpuInterpreter(Interpreter cpu, Action<DecodedInstruction>[] parentCallTable)
        {
            m_Cpu = cpu;
            m_CallTable = parentCallTable;

            Map(Add, ADD_FPU);
        }

        private void Map(Action<DecodedInstruction> instAction, params Opcode[] opcodes)
        {
            foreach (var op in opcodes)
            {
                m_CallTable[op.ID] = instAction;
            }
        }

        protected abstract void Add(DecodedInstruction inst);

        public ExecutionState State => m_Cpu.State;

        public bool FR => m_Cpu.Cop0.SR.FRMode;
    }
}
