using static cor64.Mips.ExecutionFlags;
using static cor64.Mips.OperandType;
using static cor64.Mips.OperationFamily;
using static cor64.Mips.RegBoundType;
using static cor64.Mips.OpcodeFactory;

namespace cor64.Mips.R4300I
{
    internal sealed class Opcodes : OpcodesCommon
    {
        /* Table reference opcodes */
        public static readonly Opcode BC1 = Define(TBL_BC1);
        public static readonly Opcode FPU = Define(TBL_FPU);

        /* Memory Load/Store for FPU */
        public static readonly Opcode SDC1 = Define(StoreFpu, "sdc1", I_FTIS, Data64);
        public static readonly Opcode SWC1 = Define(StoreFpu, "swc1", I_FTIS, Data32);
        public static readonly Opcode LDC1 = Define(LoadFpu, "ldc1", I_FTIS, Data64);
        public static readonly Opcode LWC1 = Define(LoadFpu, "lwc1", I_FTIS, Data32);

        /* Reg to reg FPU ops */
        public static readonly Opcode MFC1 = Define(Fpu, "mfc1", Cop1_CT, Cp1, Gpr);
        public static readonly Opcode DMFC1 = Define(Fpu, "dmfc1", Cop1_CT, Cp1, Gpr, Data64);
        public static readonly Opcode CFC1 = Define(Fpu, "cfc1", Cop1_FromCtrl, Cp1Ctl, Gpr, Data32);
        public static readonly Opcode MTC1 = Define(Fpu, "mtc1", Cop1_TC, Gpr, Cp1, Data32);
        public static readonly Opcode DMTC1 = Define(Fpu, "dmtc1", Cop1_TC, Gpr, Cp1, Data64);
        public static readonly Opcode CTC1 = Define(Fpu, "ctc1", Cop1_ToCtrl, Gpr, Cp1Ctl, Data32);

        /* MMU Operations */
        public static readonly Opcode CACHE = Define(Cache, "cache", I_TIS);
        public static readonly Opcode SYNC = Define(Synchronize, "sync", Sync);
        public static readonly Opcode TLBR = Define(Tlb, "tlbr ", OperandType.TLB);
        public static readonly Opcode TLBWI = Define(Tlb, "tlbwi ", OperandType.TLB);
        public static readonly Opcode TLBWR = Define(Tlb, "tlbwr ", OperandType.TLB, Random);
        public static readonly Opcode TLBP = Define(Tlb, "tlbp ", OperandType.TLB);

        /* Operating system */
        public static readonly Opcode ERET = Define(Branch, "eret ", Empty);

        /* Trapping */
        public static readonly Opcode TGEI = Define(Trap, "tgei", I_SI, ArithmeticOp.GREATER_THAN, Immediate);
        public static readonly Opcode TGEIU = Define(Trap, "tgeiu", I_SI, ArithmeticOp.GREATER_THAN, Immediate | Unsigned);
        public static readonly Opcode TLTI = Define(Trap, "tlti", I_SI, ArithmeticOp.LESS_THAN, Immediate);
        public static readonly Opcode TLTIU = Define(Trap, "tltiu", I_SI, ArithmeticOp.LESS_THAN, Immediate | Unsigned);
        public static readonly Opcode TEQI = Define(Trap, "teqi", I_SI, ArithmeticOp.EQUAL, Immediate);
        public static readonly Opcode TNEI = Define(Trap, "tnei", I_SI, ArithmeticOp.NOT_EQUAL, Immediate);
        public static readonly Opcode TGE = Define(Trap, "tge", TrapWithCode, ArithmeticOp.GREATER_THAN);
        public static readonly Opcode TGEU = Define(Trap, "tgeu", TrapWithCode, ArithmeticOp.GREATER_THAN, Unsigned);
        public static readonly Opcode TLT = Define(Trap, "tlt", TrapWithCode, ArithmeticOp.LESS_THAN);
        public static readonly Opcode TLTU = Define(Trap, "tltu", TrapWithCode, ArithmeticOp.LESS_THAN, Unsigned);
        public static readonly Opcode TEQ = Define(Trap, "teq", TrapWithCode, ArithmeticOp.EQUAL);
        public static readonly Opcode TNE = Define(Trap, "tne", TrapWithCode, ArithmeticOp.NOT_EQUAL);

        /* FPU */
        public static readonly Opcode ADD_FPU = Define(Fpu, "add.fmt", Cop1_DST);
        public static readonly Opcode SUB_FPU = Define(Fpu, "sub.fmt", Cop1_DST);
        public static readonly Opcode MUL_FPU = Define(Fpu, "mul.fmt", Cop1_DST);
        public static readonly Opcode DIV_FPU = Define(Fpu, "div.fmt", Cop1_DST);
        public static readonly Opcode SQRT = Define(Fpu, "sqrt.fmt", Cop1_DS);
        public static readonly Opcode ABS = Define(Fpu, "abs.fmt", Cop1_DS);
        public static readonly Opcode MOV = Define(Fpu, "mov.fmt", Cop1_DS);
        public static readonly Opcode NEG = Define(Fpu, "neg.fmt", Cop1_DS);
        public static readonly Opcode ROUND_L = Define(Fpu, "round.l.fmt", Cop1_DS, Data64);
        public static readonly Opcode TRUNC_L = Define(Fpu, "trunc.l.fmt", Cop1_DS, Data64);
        public static readonly Opcode CEIL_L = Define(Fpu, "ceil.l.fmt", Cop1_DS, Data64);
        public static readonly Opcode FLOOR_L = Define(Fpu, "floor.l.fmt", Cop1_DS, Data64);
        public static readonly Opcode ROUND_W = Define(Fpu, "round.w.fmt", Cop1_DS, Data32);
        public static readonly Opcode TRUNC_W = Define(Fpu, "trunc.w.fmt", Cop1_DS, Data32);
        public static readonly Opcode CEIL_W = Define(Fpu, "ceil.w.fmt", Cop1_DS, Data32);
        public static readonly Opcode FLOOR_W = Define(Fpu, "floor.w.fmt", Cop1_DS, Data32);
        public static readonly Opcode CVT_S = Define(Fpu, "cvt.s.fmt", Cop1_DS, DataS);
        public static readonly Opcode CVT_D = Define(Fpu, "cvt.d.fmt", Cop1_DS, DataD);
        public static readonly Opcode CVT_W = Define(Fpu, "cvt.w.fmt", Cop1_DS, Data32);
        public static readonly Opcode CVT_L = Define(Fpu, "cvt.l.fmt", Cop1_DS, Data64);

        /* FPU Branching */
        public static readonly Opcode BC1F = Define(Branch, "bc1f", Bc1, ArithmeticOp.FALSE, ExecutionFlags.None, Cp1);
        public static readonly Opcode BC1T = Define(Branch, "bc1t", Bc1, ArithmeticOp.TRUE, ExecutionFlags.None, Cp1);
        public static readonly Opcode BC1FL = Define(Branch, "bc1fl", Bc1, ArithmeticOp.FALSE, Likely, Cp1);
        public static readonly Opcode BC1TL = Define(Branch, "bc1tl", Bc1, ArithmeticOp.TRUE, Likely, Cp1);

        /* FPU Condition Opcodes */
        public static readonly Opcode C_F = Define(Fpu, "c.f.fmt", Cop1_TS);                                              // False
        public static readonly Opcode C_UN = Define(Fpu, "c.un.fmt", Cop1_TS, CondUn);                                      // Unordered
        public static readonly Opcode C_EQ = Define(Fpu, "c.eq.fmt", Cop1_TS, CondEq);                                      // Equal
        public static readonly Opcode C_UEQ = Define(Fpu, "c.ueq.fmt", Cop1_TS, CondUn | CondEq);                             // Unordered or Equal
        public static readonly Opcode C_OLT = Define(Fpu, "c.olt.fmt", Cop1_TS, CondLT);                                      // Ordered or Less Than
        public static readonly Opcode C_ULT = Define(Fpu, "c.ult.fmt", Cop1_TS, CondUn | CondLT);                             // Unordered or Less Than
        public static readonly Opcode C_OLE = Define(Fpu, "c.ole.fmt", Cop1_TS, CondLT | CondEq);                             // Ordered or Less Than or Equal
        public static readonly Opcode C_ULE = Define(Fpu, "c.ule.fmt", Cop1_TS, CondUn | CondLT | CondEq);                    // Unordered or Less Than or Equal
        public static readonly Opcode C_SF = Define(Fpu, "c.sf.fmt", Cop1_TS, ArithmeticOp.SIGNALING, CondOrd);             // Signaling False
        public static readonly Opcode C_NGLE = Define(Fpu, "c.ngle.fmt", Cop1_TS, CondOrd | CondNot | CondLT | CondGT | CondEq);         // Not Greater Than or Less Than or Equal
        public static readonly Opcode C_SEQ = Define(Fpu, "c.seq.fmt", Cop1_TS, ArithmeticOp.SIGNALING, CondEq | CondOrd);    // Signaling Equal
        public static readonly Opcode C_NGL = Define(Fpu, "c.ngl.fmt", Cop1_TS, CondOrd | CondNot | CondGT | CondLT);         // Not Greater Than or Less Than
        public static readonly Opcode C_LT = Define(Fpu, "c.lt.fmt", Cop1_TS, CondOrd | CondLT);                            // Less Than
        public static readonly Opcode C_NGE = Define(Fpu, "c.nge.fmt", Cop1_TS, CondOrd | CondNot | CondGT | CondEq);         // Not Greater Then or Equal
        public static readonly Opcode C_LE = Define(Fpu, "c.le.fmt", Cop1_TS, CondOrd | CondLT | CondEq);                            // Less Than or Equal
        public static readonly Opcode C_NGT = Define(Fpu, "c.ngt.fmt", Cop1_TS, CondOrd | CondNot | CondGT);                  // Not Greater Than
    }
}