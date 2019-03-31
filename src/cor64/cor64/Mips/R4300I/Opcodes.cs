using static cor64.Mips.ExecutionFlags;
using static cor64.Mips.OperandType;
using static cor64.Mips.OperationFamily;
using static cor64.Mips.RegBoundType;
using static cor64.Mips.OpcodeFactory;

namespace cor64.Mips.R4300I
{
    internal static class Opcodes
    {
        /* Table reference opcodes */
        public static readonly Opcode SPECIAL = Define(TBL_SPECIAL);
        public static readonly Opcode REGIMM = Define(TBL_REGIMM);
        public static readonly Opcode COP0 = Define(TBL_COP0);
        public static readonly Opcode COP1 = Define(TBL_COP1);
        public static readonly Opcode BC1 = Define(TBL_BC1);
        public static readonly Opcode FPU = Define(TBL_FPU);
        public static readonly Opcode NULL = DefineNull();

        /* Jumping and Branching */
        public static readonly Opcode J = Define(Branch, "j", JUMP);
        public static readonly Opcode JAL = Define(Branch, "jal", JUMP, Link);
        public static readonly Opcode BEQ = Define(Branch, "beq", I_STI, ArithmeticOp.EQUAL);
        public static readonly Opcode BNE = Define(Branch, "bne", I_STI, ArithmeticOp.NOT_EQUAL);
        public static readonly Opcode BLEZ = Define(Branch, "blez", I_STI, ArithmeticOp.LESS_THAN_OR_EQUAL);
        public static readonly Opcode BGTZ = Define(Branch, "bgtz", I_STI, ArithmeticOp.GREATER_THAN);
        public static readonly Opcode BEQL = Define(Branch, "beql", I_STI, ArithmeticOp.EQUAL, Likely);
        public static readonly Opcode BNEL = Define(Branch, "bnel", I_STI, ArithmeticOp.NOT_EQUAL, Likely);
        public static readonly Opcode BLEZL = Define(Branch, "blezl", I_STI, ArithmeticOp.LESS_THAN_OR_EQUAL, Likely);
        public static readonly Opcode BGTZL = Define(Branch, "bgtzl", I_STI, ArithmeticOp.GREATER_THAN, Likely);
        public static readonly Opcode BLTZ = Define(Branch, "bltz", I_STI, ArithmeticOp.LESS_THAN);
        public static readonly Opcode BGEZ = Define(Branch, "bgez", I_STI, ArithmeticOp.GREATER_THAN_OR_EQUAL);
        public static readonly Opcode BLTZL = Define(Branch, "bltzl", I_STI, ArithmeticOp.LESS_THAN, Likely);
        public static readonly Opcode BGEZL = Define(Branch, "bgezl", I_STI, ArithmeticOp.GREATER_THAN_OR_EQUAL, Likely);
        public static readonly Opcode JR = Define(Branch, "jr", R_S, Register);
        public static readonly Opcode JALR = Define(Branch, "jalr", R_SD, Link | Register);
        public static readonly Opcode BLTZAL = Define(Branch, "bltzal", I_STI, ArithmeticOp.LESS_THAN_OR_EQUAL, Link);
        public static readonly Opcode BGEZAL = Define(Branch, "bgezal", I_STI, ArithmeticOp.GREATER_THAN_OR_EQUAL, Link);
        public static readonly Opcode BLTZALL = Define(Branch, "bltzall", I_STI, ArithmeticOp.LESS_THAN_OR_EQUAL, Link | Likely);
        public static readonly Opcode BGEZALL = Define(Branch, "bgezall", I_STI, ArithmeticOp.GREATER_THAN_OR_EQUAL, Link | Likely);

        /* ALU */
        public static readonly Opcode ADDI = Define(Reg, "addi", I_TSI, Immediate);
        public static readonly Opcode ADDIU = Define(Reg, "addiu", I_TSI, Immediate | Unsigned);
        public static readonly Opcode SLTI = Define(Reg, "slti", I_TSI, Immediate);
        public static readonly Opcode SLTIU = Define(Reg, "sltiu", I_TSI, Immediate | Unsigned);
        public static readonly Opcode ANDI = Define(Reg, "andi", I_TSI, ArithmeticOp.AND, Immediate);
        public static readonly Opcode ORI = Define(Reg, "ori", I_TSI, ArithmeticOp.OR, Immediate);
        public static readonly Opcode XORI = Define(Reg, "xori", I_TSI, ArithmeticOp.XOR, Immediate);
        public static readonly Opcode DADDI = Define(Reg, "daddi", I_TSI, Immediate | Reserved32);
        public static readonly Opcode DADDIU = Define(Reg, "daddiu", I_TSI, Immediate | Unsigned | Reserved32);
        public static readonly Opcode DSLL = Define(Reg, "dsll", R_DTA, ArithmeticOp.LSHIFT, Reserved32 | Unsigned);
        public static readonly Opcode DSRL = Define(Reg, "dsrl", R_DTA, ArithmeticOp.RSHIFT, Reserved32 | Unsigned);
        public static readonly Opcode DSRA = Define(Reg, "dsra", R_DTA, ArithmeticOp.RSHIFT, Reserved32);
        public static readonly Opcode DSLL32 = Define(Reg, "dsll32", R_DTA, ArithmeticOp.LSHIFT, Reserved32 | Unsigned | Shift32);
        public static readonly Opcode DSRL32 = Define(Reg, "dsrl32", R_DTA, ArithmeticOp.RSHIFT, Reserved32 | Unsigned | Shift32);
        public static readonly Opcode DSRA32 = Define(Reg, "dsra32", R_DTA, ArithmeticOp.RSHIFT, Reserved32 | Shift32);
        public static readonly Opcode DSLLV = Define(Reg, "dsllv", R_DTS, ArithmeticOp.LSHIFT, Reserved32 | Unsigned | VarShift);
        public static readonly Opcode DSRLV = Define(Reg, "dsrlv", R_DTS, ArithmeticOp.RSHIFT, Reserved32 | Unsigned | VarShift);
        public static readonly Opcode DSRAV = Define(Reg, "dsrav", R_DTS, ArithmeticOp.RSHIFT, Reserved32 | VarShift);
        public static readonly Opcode MULT = Define(Reg, "mult", R_ST);
        public static readonly Opcode MULTU = Define(Reg, "multu", R_ST, Unsigned);
        public static readonly Opcode NOR = Define(Reg, "nor", R_DST, ArithmeticOp.NOR);
        public static readonly Opcode DIV = Define(Reg, "div", R_ST);
        public static readonly Opcode DIVU = Define(Reg, "divu", R_ST, Unsigned);
        public static readonly Opcode DMULT = Define(Reg, "dmult", R_ST, Reserved32);
        public static readonly Opcode DMULTU = Define(Reg, "dmultu", R_ST, Unsigned | Reserved32);
        public static readonly Opcode DDIV = Define(Reg, "ddiv", R_ST, Reserved32);
        public static readonly Opcode DDIVU = Define(Reg, "ddivu", R_ST, Unsigned | Reserved32);
        public static readonly Opcode ADD = Define(Reg, "add", R_DST);
        public static readonly Opcode ADDU = Define(Reg, "addu", R_DST, Unsigned);
        public static readonly Opcode SUB = Define(Reg, "sub", R_DST);
        public static readonly Opcode SUBU = Define(Reg, "subu", R_DST, Unsigned);
        public static readonly Opcode AND = Define(Reg, "and", R_DST, ArithmeticOp.AND);
        public static readonly Opcode OR = Define(Reg, "or", R_DST, ArithmeticOp.OR);
        public static readonly Opcode XOR = Define(Reg, "xor", R_DST, ArithmeticOp.XOR);
        public static readonly Opcode SLT = Define(Reg, "slt", R_DST);
        public static readonly Opcode SLTU = Define(Reg, "sltu", R_DST, Unsigned);
        public static readonly Opcode DADD = Define(Reg, "dadd", R_DST, Reserved32);
        public static readonly Opcode DADDU = Define(Reg, "daddu", R_DST, Unsigned | Reserved32);
        public static readonly Opcode DSUB = Define(Reg, "dsub", R_DST, Reserved32);
        public static readonly Opcode DSUBU = Define(Reg, "dsubu", R_DST, Unsigned | Reserved32);
        public static readonly Opcode SLL = Define(Reg, "sll", R_DTA, ArithmeticOp.LSHIFT, Unsigned);
        public static readonly Opcode SRL = Define(Reg, "srl", R_DTA, ArithmeticOp.RSHIFT, Unsigned);
        public static readonly Opcode SRA = Define(Reg, "sra", R_DTA, ArithmeticOp.RSHIFT);
        public static readonly Opcode SLLV = Define(Reg, "sllv", R_DTS, ArithmeticOp.LSHIFT, Unsigned | VarShift);
        public static readonly Opcode SRLV = Define(Reg, "srlv", R_DTS, ArithmeticOp.RSHIFT, Unsigned | VarShift);
        public static readonly Opcode SRAV = Define(Reg, "srav", R_DTS, ArithmeticOp.RSHIFT, VarShift);

        /* Memory Loading */
        public static readonly Opcode LUI = Define(Reg, "lui", I_TI, Immediate);
        public static readonly Opcode LDL = Define(Load, "ldl", I_TIS, Data64 | Left);
        public static readonly Opcode LDR = Define(Load, "ldr", I_TIS, Data64 | Right);
        public static readonly Opcode LB = Define(Load, "lb", I_TIS, Data8);
        public static readonly Opcode LH = Define(Load, "lh", I_TIS, Data16);
        public static readonly Opcode LWL = Define(Load, "lwl", I_TIS, Data32 | Left);
        public static readonly Opcode LW = Define(Load, "lw", I_TIS, Data32);
        public static readonly Opcode LBU = Define(Load, "lbu", I_TIS, Data8 | Unsigned);
        public static readonly Opcode LHU = Define(Load, "lhu", I_TIS, Data16 | Unsigned);
        public static readonly Opcode LWR = Define(Load, "lwr", I_TIS, Data32 | Right);
        public static readonly Opcode LWU = Define(Load, "lwu", I_TIS, Data32 | Unsigned);
        public static readonly Opcode LL = Define(Load, "ll", I_TIS, Link | Data32);
        public static readonly Opcode LLD = Define(Load, "lld", I_FTIS, Link | Data64);
        public static readonly Opcode LD = Define(Load, "ld", I_TIS, Data64);

        /* Memory Storing */
        public static readonly Opcode SB = Define(Store, "sb", I_TIS, Data8);
        public static readonly Opcode SH = Define(Store, "sh", I_TIS, Data16);
        public static readonly Opcode SWL = Define(Store, "swl", I_TIS, Data32 | Left);
        public static readonly Opcode SW = Define(Store, "sw", I_TIS, Immediate | Data32);
        public static readonly Opcode SDL = Define(Store, "sdl", I_TIS, Data64 | Left);
        public static readonly Opcode SDR = Define(Store, "sdr", I_TIS, Data64 | Right);
        public static readonly Opcode SWR = Define(Store, "swr", I_TIS, Data32 | Right);
        public static readonly Opcode SD = Define(Store, "sd", I_TIS, Immediate | Data64);
        public static readonly Opcode SCD = Define(Store, "scd", I_TIS, Link | Data64);
        public static readonly Opcode SC = Define(Store, "sc", I_TIS, Link | Data32);

        /* Memory Load/Store for FPU */
        public static readonly Opcode SDC1 = Define(StoreFpu, "sdc1", I_FTIS, Data64);
        public static readonly Opcode SWC1 = Define(StoreFpu, "swc1", I_FTIS, Data32);
        public static readonly Opcode LDC1 = Define(LoadFpu, "ldc1", I_FTIS, Data64);
        public static readonly Opcode LWC1 = Define(LoadFpu, "lwc1", I_FTIS, Data32);

        /* Register to register operations */
        public static readonly Opcode MFC0 = Define(Cop0, "mfc0", Cop0_CT, Cp0, Gpr, Data32);
        public static readonly Opcode DMFC0 = Define(Cop0, "dmfc0", Cop0_CT, Cp0, Gpr, Data64);
        public static readonly Opcode MTC0 = Define(Cop0, "mtc0", Cop0_TC, Gpr, Cp0, Data32);
        public static readonly Opcode DMTC0 = Define(Cop0, "dmtc0", Cop0_TC, Gpr, Cp0, Data64);
        public static readonly Opcode MFC1 = Define(Fpu, "mfc1", Cop1_TC, Cp1, Gpr);
        public static readonly Opcode DMFC1 = Define(Fpu, "dmfc1", Cop1_TC, Cp1, Gpr, Data64);
        public static readonly Opcode CFC1 = Define(Fpu, "cfc1", Cop1_FromCtrl, Cp1Ctl, Gpr, Data32);
        public static readonly Opcode MTC1 = Define(Fpu, "mtc1", Cop0_CT, Gpr, Cp1, Data32);
        public static readonly Opcode DMTC1 = Define(Fpu, "dmtc1", Cop0_CT, Gpr, Cp1, Data64);
        public static readonly Opcode CTC1 = Define(Fpu, "ctc1", Cop1_ToCtrl, Gpr, Cp1Ctl, Data32);
        public static readonly Opcode MFHI = Define(Reg, "mfhi", R_D, Hi, Gpr);
        public static readonly Opcode MTHI = Define(Reg, "mthi", R_D, Gpr, Hi);
        public static readonly Opcode MFLO = Define(Reg, "mflo", R_D, Lo, Gpr);
        public static readonly Opcode MTLO = Define(Reg, "mtlo", R_D, Gpr, Lo);

        /* MMU Operations */
        public static readonly Opcode CACHE = Define(Cache, "cache", I_TIS);
        public static readonly Opcode SYNC = Define(Synchronize, "sync", Sync);
        public static readonly Opcode TLBR = Define(Tlb, "tlbr ", TLB);
        public static readonly Opcode TLBWI = Define(Tlb, "tlbwi ", TLB);
        public static readonly Opcode TLBWR = Define(Tlb, "tlbwr ", TLB);
        public static readonly Opcode TLBP = Define(Tlb, "tlbp ", TLB);

        /* Operating system */
        public static readonly Opcode ERET = Define(Tlb, "eret ", TLB);
        public static readonly Opcode SYSCALL = Define(Trap, "syscall", Code);
        public static readonly Opcode BREAK = Define(Trap, "break", Code);

        /* Trapping */
        public static readonly Opcode TGEI = Define(Trap, "tgei", I_SI);
        public static readonly Opcode TGEIU = Define(Trap, "tgeiu", I_SI);
        public static readonly Opcode TLTI = Define(Trap, "tlti", I_SI);
        public static readonly Opcode TLTIU = Define(Trap, "tltiu", I_SI);
        public static readonly Opcode TEQI = Define(Trap, "teqi", I_SI);
        public static readonly Opcode TNEI = Define(Trap, "tnei", I_SI);
        public static readonly Opcode TGE = Define(Trap, "tge", TrapWithCode);
        public static readonly Opcode TGEU = Define(Trap, "tgeu", TrapWithCode);
        public static readonly Opcode TLT = Define(Trap, "tlt", TrapWithCode);
        public static readonly Opcode TLTU = Define(Trap, "tltu", TrapWithCode);
        public static readonly Opcode TEQ = Define(Trap, "teq", TrapWithCode);
        public static readonly Opcode TNE = Define(Trap, "tne", TrapWithCode);

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
        public static readonly Opcode C_F =    Define(Fpu, "c.f.fmt",    Cop1_TS);                                              // False
        public static readonly Opcode C_UN =   Define(Fpu, "c.un.fmt",   Cop1_TS, CondUn);                                      // Unordered
        public static readonly Opcode C_EQ =   Define(Fpu, "c.eq.fmt",   Cop1_TS, CondEq);                                      // Equal
        public static readonly Opcode C_UEQ =  Define(Fpu, "c.ueq.fmt",  Cop1_TS, CondUn | CondEq);                             // Unordered or Equal
        public static readonly Opcode C_OLT =  Define(Fpu, "c.olt.fmt",  Cop1_TS, CondLT);                                      // Ordered or Less Than
        public static readonly Opcode C_ULT =  Define(Fpu, "c.ult.fmt",  Cop1_TS, CondUn | CondLT);                             // Unordered or Less Than
        public static readonly Opcode C_OLE =  Define(Fpu, "c.ole.fmt",  Cop1_TS, CondLT | CondEq);                             // Ordered or Less Than or Equal
        public static readonly Opcode C_ULE =  Define(Fpu, "c.ule.fmt",  Cop1_TS, CondUn | CondLT | CondEq);                    // Unordered or Less Than or Equal
        public static readonly Opcode C_SF =   Define(Fpu, "c.sf.fmt",   Cop1_TS, ArithmeticOp.SIGNALING, CondOrd);             // Signaling False
        public static readonly Opcode C_NGLE = Define(Fpu, "c.ngle.fmt", Cop1_TS, CondOrd | CondNot | CondLT | CondGT | CondEq);         // Not Greater Than or Less Than or Equal
        public static readonly Opcode C_SEQ =  Define(Fpu, "c.seq.fmt",  Cop1_TS, ArithmeticOp.SIGNALING, CondEq | CondOrd);    // Signaling Equal
        public static readonly Opcode C_NGL =  Define(Fpu, "c.ngl.fmt",  Cop1_TS, CondOrd | CondNot | CondGT | CondLT);         // Not Greater Than or Less Than
        public static readonly Opcode C_LT =   Define(Fpu, "c.lt.fmt",   Cop1_TS, CondOrd | CondLT);                            // Less Than
        public static readonly Opcode C_NGE =  Define(Fpu, "c.nge.fmt",  Cop1_TS, CondOrd | CondNot | CondGT | CondEq);         // Not Greater Then or Equal
        public static readonly Opcode C_LE =   Define(Fpu, "c.le.fmt",   Cop1_TS, CondOrd | CondLT | CondEq);                            // Less Than or Equal
        public static readonly Opcode C_NGT =  Define(Fpu, "c.ngt.fmt",  Cop1_TS, CondOrd | CondNot | CondGT);                  // Not Greater Than
    }
}