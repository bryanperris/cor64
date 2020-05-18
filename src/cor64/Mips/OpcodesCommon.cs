using static cor64.Mips.ExecutionFlags;
using static cor64.Mips.OperandType;
using static cor64.Mips.OperationFamily;
using static cor64.Mips.RegBoundType;
using static cor64.Mips.OpcodeFactory;

namespace cor64.Mips
{
    public class OpcodesCommon : OpcodeFactory
    {
        public static readonly Opcode SPECIAL = Define(TBL_SPECIAL);
        public static readonly Opcode REGIMM = Define(TBL_REGIMM);
        public static readonly Opcode COP0 = Define(TBL_COP0);
        public static readonly Opcode COP1 = Define(TBL_COP1);
        public static readonly Opcode COP2 = Define(TBL_COP2);
        public static readonly Opcode NULL = DefineNull();

        /* Jumping and Branching */
        public static readonly Opcode J = Define(Branch, "j", JUMP);
        public static readonly Opcode JAL = Define(Branch, "jal", JUMP, Link);
        public static readonly Opcode BEQ = Define(Branch, "beq", BRANCH, ArithmeticOp.EQUAL);
        public static readonly Opcode BNE = Define(Branch, "bne", BRANCH, ArithmeticOp.NOT_EQUAL);
        public static readonly Opcode BLEZ = Define(Branch, "blez", BRANCH, ArithmeticOp.LESS_THAN_OR_EQUAL);
        public static readonly Opcode BGTZ = Define(Branch, "bgtz", BRANCH, ArithmeticOp.GREATER_THAN);
        public static readonly Opcode BEQL = Define(Branch, "beql", BRANCH, ArithmeticOp.EQUAL, Likely);
        public static readonly Opcode BNEL = Define(Branch, "bnel", BRANCH, ArithmeticOp.NOT_EQUAL, Likely);
        public static readonly Opcode BLEZL = Define(Branch, "blezl", BRANCH, ArithmeticOp.LESS_THAN_OR_EQUAL, Likely);
        public static readonly Opcode BGTZL = Define(Branch, "bgtzl", BRANCH, ArithmeticOp.GREATER_THAN, Likely);
        public static readonly Opcode BLTZ = Define(Branch, "bltz", BRANCH, ArithmeticOp.LESS_THAN);
        public static readonly Opcode BGEZ = Define(Branch, "bgez", BRANCH, ArithmeticOp.GREATER_THAN_OR_EQUAL);
        public static readonly Opcode BLTZL = Define(Branch, "bltzl", BRANCH, ArithmeticOp.LESS_THAN, Likely);
        public static readonly Opcode BGEZL = Define(Branch, "bgezl", BRANCH, ArithmeticOp.GREATER_THAN_OR_EQUAL, Likely);
        public static readonly Opcode JR = Define(Branch, "jr", R_S, Register);
        public static readonly Opcode JALR = Define(Branch, "jalr", R_SD, Link | Register);
        public static readonly Opcode BLTZAL = Define(Branch, "bltzal", BRANCH, ArithmeticOp.LESS_THAN_OR_EQUAL, Link);
        public static readonly Opcode BGEZAL = Define(Branch, "bgezal", BRANCH, ArithmeticOp.GREATER_THAN_OR_EQUAL, Link);
        public static readonly Opcode BLTZALL = Define(Branch, "bltzall", BRANCH, ArithmeticOp.LESS_THAN_OR_EQUAL, Link | Likely);
        public static readonly Opcode BGEZALL = Define(Branch, "bgezall", BRANCH, ArithmeticOp.GREATER_THAN_OR_EQUAL, Link | Likely);

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

        /* Register to register operations */
        public static readonly Opcode MFC0 = Define(Cop0, "mfc0", Cop0_CT, Cp0, Gpr, Data32);
        public static readonly Opcode DMFC0 = Define(Cop0, "dmfc0", Cop0_CT, Cp0, Gpr, Data64);
        public static readonly Opcode MTC0 = Define(Cop0, "mtc0", Cop0_TC, Gpr, Cp0, Data32);
        public static readonly Opcode DMTC0 = Define(Cop0, "dmtc0", Cop0_TC, Gpr, Cp0, Data64);
        public static readonly Opcode MFHI = Define(Reg, "mfhi", R_D, Hi, Gpr);
        public static readonly Opcode MTHI = Define(Reg, "mthi", R_S, Gpr, Hi);
        public static readonly Opcode MFLO = Define(Reg, "mflo", R_D, Lo, Gpr);
        public static readonly Opcode MTLO = Define(Reg, "mtlo", R_S, Gpr, Lo);


        /* Operating system */
        public static readonly Opcode SYSCALL = Define(Trap, "syscall", Code);
        public static readonly Opcode BREAK = Define(Trap, "break", Code);
    }
}