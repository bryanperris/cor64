using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.Mips
{
    public struct DecodedInstruction
    {
        /* Decoded Instruction data */
        private readonly BinaryInstruction m_Inst;
        private readonly Opcode m_Opcode;
        private readonly ulong m_Address;
        private readonly bool m_NoOp;
        private readonly bool m_LastInst;
  
        internal DecodedInstruction(ulong address, Opcode opcode, BinaryInstruction inst, bool emuNoOps, bool lastOne)
        {
            m_Opcode = opcode;
            m_Inst = inst;
            m_Address = address;
            m_NoOp = emuNoOps;
            m_LastInst = lastOne;
            TagIsDelaySlot = false;
        }

        public BinaryInstruction Inst => m_Inst;

        public Opcode Op => m_Opcode;

        public bool IsBranch => Op.Family == OperationFamily.Branch;

        public FpuValueType Format => m_Inst.fmtType;

        public int FloatSource => m_Inst.fs;

        public int FloatTarget => m_Inst.ft;

        public int FloatDest => m_Inst.fd;

        public ulong Address => m_Address;

        public String Opcode => m_Opcode.Op;

        public bool TagIsDelaySlot { get; set; }

        /// <summary>
        /// Indicates the instruction was fetched from valid readable memory
        /// </summary>
        public bool IsValid => m_NoOp;

        public bool LastOne => m_LastInst;

        public bool IsFloatingType => Format == FpuValueType.FSingle || Format == FpuValueType.FDouble;

        public bool IsBranchConditional
        {
            get
            {
                if (!IsBranch) return false;

                switch (Op.ArithmeticType)
                {
                    default: return false;
                    case ArithmeticOp.EQUAL: 
                    case ArithmeticOp.NOT_EQUAL:
                    case ArithmeticOp.GREATER_THAN:
                    case ArithmeticOp.LESS_THAN:
                    case ArithmeticOp.GREATER_THAN_OR_EQUAL:
                    case ArithmeticOp.LESS_THAN_OR_EQUAL:
                    case ArithmeticOp.FALSE:
                    case ArithmeticOp.TRUE: return true;
                }
            }
        }

        public int Source => m_Inst.rs;
        public int Destination => m_Inst.rd;
        public int Target => m_Inst.rt;
        public uint Immediate => m_Inst.imm;
        public int ShiftAmount => m_Inst.sa;

        public bool IsNull => m_Opcode.Family == OperationFamily.Null;
    }
}
