using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static cor64.Mips.R4300I.Opcodes;

namespace cor64.Mips.R4300I
{
    internal static class DecodeTable
    {
        private readonly static Opcode[] MainTable = {
            SPECIAL, REGIMM, J,     JAL,   BEQ,  BNE,   BLEZ,  BGTZ,
            ADDI,    ADDIU,  SLTI,  SLTIU, ANDI, ORI,   XORI,  LUI,
            COP0,    COP1,   NULL,  NULL,  BEQL, BNEL,  BLEZL, BGTZL,
            DADDI,   DADDIU, LDL,   LDR,   NULL, NULL,  NULL,  NULL,
            LB,      LH,     LWL,   LW,    LBU,  LHU,   LWR,   LWU,
            SB,      SH,     SWL,   SW,    SDL,  SDR,   SWR,   CACHE,
            LL,      LWC1,   NULL,  NULL,  LLD,  LDC1,  NULL,  LD,
            SC,      SWC1,   NULL,  NULL,  SCD,  SDC1,  NULL,  SD
        };

        private readonly static Opcode[] Cop0Table = {
            MFC0,  DMFC0, NULL, NULL, MTC0, DMTC0, NULL, NULL,
            NULL,  NULL,  NULL, NULL, NULL, NULL,  NULL, NULL,
            NULL,  NULL,  NULL, NULL, NULL, NULL,  NULL, NULL,
            NULL,  NULL,  NULL, NULL, NULL, NULL,  NULL, NULL
        };

        private readonly static Opcode[] Cop1Table = {
            MFC1, DMFC1, CFC1, NULL, MTC1, DMTC1, CTC1, NULL,
            BC1,  NULL,  NULL, NULL, NULL, NULL,  NULL, NULL,
            FPU,  FPU,   NULL, NULL, FPU,  FPU,   NULL, NULL,
            NULL, NULL,  NULL, NULL, NULL, NULL,  NULL, NULL
        };

        private readonly static Opcode[] TlbTable = {
            NULL, TLBR, TLBWI, NULL, NULL, NULL, TLBWR, NULL,
            TLBP, NULL, NULL,  NULL, NULL, NULL, NULL,  NULL,
            NULL, NULL, NULL,  NULL, NULL, NULL, NULL,  NULL,
            ERET, NULL, NULL,  NULL, NULL, NULL, NULL,  NULL,
            NULL, NULL, NULL,  NULL, NULL, NULL, NULL,  NULL,
            NULL, NULL, NULL,  NULL, NULL, NULL, NULL,  NULL
        };

        private readonly static Opcode[] Bc1Table = {
            BC1F,  BC1T,
            BC1FL, BC1TL
        };

        private readonly static Opcode[] FpuTable = {
            ADD_FPU, SUB_FPU, MUL_FPU, DIV_FPU, SQRT,    ABS,     MOV,    NEG,
            ROUND_L, TRUNC_L, CEIL_L,  FLOOR_L, ROUND_W, TRUNC_W, CEIL_W, FLOOR_W,
            NULL,    NULL,    NULL,    NULL,    NULL,    NULL,    NULL,   NULL,
            NULL,    NULL,    NULL,    NULL,    NULL,    NULL,    NULL,   NULL,
            CVT_S,   CVT_D,   NULL,    NULL,    CVT_W,   CVT_L,   NULL,   NULL,
            NULL,    NULL,    NULL,    NULL,    NULL,    NULL,    NULL,   NULL,
            C,       C,       C,       C,       C,       C,       C,      C,
            C,       C,       C,       C,       C,       C,       C,      C
        };

        private readonly static Opcode[] RegImmTable = {
            BLTZ,   BGEZ,   BLTZL,   BGEZL,   NULL, NULL, NULL, NULL,
            TGEI,   TGEIU,  TLTI,    TLTIU,   TEQI, NULL, TNEI, NULL,
            BLTZAL, BGEZAL, BLTZALL, BGEZALL, NULL, NULL, NULL, NULL,
            NULL,   NULL,   NULL,    NULL,    NULL, NULL, NULL, NULL
        };

        private readonly static Opcode[] SpecialTable = {
            SLL,   NULL,  SRL,   SRA,     SLLV,    NULL,   SRLV,   SRAV,
            JR,    JALR,  NULL,  NULL,    SYSCALL, BREAK,  NULL,   SYNC,
            MFHI,  MTHI,  MFLO,  MTLO,    DSLLV,   NULL,   DSRLV,  DSRAV,
            MULT,  MULTU, DIV,   DIVU,    DMULT,   DMULTU, DDIV,   DDIVU,
            ADD,   ADDU,  SUB,   SUBU,    AND,     OR,     XOR,    NOR,
            NULL,  NULL,  SLT,   SLTU,    DADD,    DADDU,  DSUB,   DSUBU,
            TGE,   TGEU,  TLT,   TLTU,    TEQ,     NULL,   TNE,    NULL,
            DSLL,  NULL,  DSRL,  DSRA,    DSLL32,  NULL,   DSRL32, DSRA32
        };

        public static Opcode OpcodeLookup(BinaryInstruction inst)
        {
            var op = MainTable[inst.op];

            switch (op.Family)
            {
                case OperationFamily.TBL_SPECIAL: return SpecialTable[inst.func];
                case OperationFamily.TBL_REGIMM: return RegImmTable[inst.rt];
                case OperationFamily.TBL_COP0:
                    {
                        switch (inst.rs)
                        {
                            case 0x10: return TlbTable[inst.func];
                            default: return Cop0Table[inst.rs];
                        }
                    }
                case OperationFamily.TBL_COP1:
                    {
                        var op2 = Cop1Table[inst.rs];

                        switch (op2.Family)
                        {
                            case OperationFamily.TBL_BC1: return Bc1Table[inst.rt];
                            case OperationFamily.TBL_FPU: return FpuTable[inst.func];
                            default: return op2;
                        }
                    }

                default: break;
            }

            return op;
        }
    }
}
