using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static cor64.Mips.OpcodesCommon;
using static cor64.Mips.Rsp.RspOpcodes;

/* RSP Opcode Notes:
The following R4000 instructions are not present in the RSP instruction set:
• LDL, LDR, LWL, LWR, LWU, SWL, SDL, SDR, SWR, LL, LLD, LDC1, LDC2, LD, SDC1, SDC2, SD, (all 64-bit loads/stores, load locked, and load/store left/right)
• SC, SCD, (store conditionals)
• BEQL, BNEL, BLEZL, BGTZL, BLTZL, BGEZL, BLTZALL,BGTZALL, BGEZALL, (all “likely” branches)
• MFHI, MTHI, MFLO, MTLO, (all HI/LO register moves)
• DADDI, DADDIU, DSLLV, DSRLV, DSRAV, DMULT, DMULTU,DDIV, DDIVU, DADD, DADDU, DSUB, DSUBU, DSLL, DSRL,DSRA, DSLL32, DSRL32, DSRA32, (all 64-bit instructions)
• MULT, MULTU, DIV, DIVU, (all multiply/divide instructions)
• SYSCALL, (RSP does not generate exceptions)
• SYNC, (this instruction is intended for multiprocessor systems)
• BCzF, BCzT (all branch-on-coprocessor instructions)
• TGE, TGEU, TLT, TLTU, TEQ, TNE, TGEI, TGEIU, TLTI, TLTIU,TEQI, TNEI, (all TRAP instructions)
*/

/*
    |--000--|--001--|--010--|--011--|--100--|--101--|--110--|--111--| lo
000 | VMULF | VMULU | VRNDP | VMULQ | VMUDL | VMUDM | VMUDN | VMUDH |
001 | VMACF | VMACU | VRNDN | VMACQ | VMADL | VMADM | VMADN | VMADH |
010 | VADD  | VSUB  | VSUT? | VABS  | VADDC | VSUBC | VADDB?| VSUBB?|
100 |  VLT  |  VEQ  |  VNE  |  VGE  |  VCL  |  VCH  |  VCR  | VMRG  |
101 | VAND  | VNAND |  VOR  | VNOR  | VXOR  | VNXOR | V056? | V057? |
110 | VRCP  | VRCPL | VRCPH | VMOV  | VRSQ  | VRSQL | VRSQH |  VNOP |
110 | VEXTT?| VEXTQ?| VEXTN?| V073? | VINST?| VINSQ?| VINSN?| VNULL?|
 hi |-------|-------|-------|-------|-------|-------|-------|-------| 
 
RSP Load: Instr. encoded by rd field when opcode field = LWC2
    31---------26-------------------15-------11---------------------0
    |  110010   |                   |   rd   |                      |
    ------6-----------------------------5----------------------------
    |--000--|--001--|--010--|--011--|--100--|--101--|--110--|--111--| lo
 00 |  LBV  |  LSV  |  LLV  |  LDV  |  LQV  |  LRV  |  LPV  |  LUV  |
 01 |  LHV  |  LFV  |  LWV  |  LTV  |  ---  |  ---  |  ---  |  ---  |
 10 |  ---  |  ---  |  ---  |  ---  |  ---  |  ---  |  ---  |  ---  |
 11 |  ---  |  ---  |  ---  |  ---  |  ---  |  ---  |  ---  |  ---  |
 hi |-------|-------|-------|-------|-------|-------|-------|-------|

    RSP Store: Instr. encoded by rd field when opcode field = SWC2
    31---------26-------------------15-------11---------------------0
    |  111010   |                   |   rd   |                      |
    ------6-----------------------------5----------------------------
    |--000--|--001--|--010--|--011--|--100--|--101--|--110--|--111--| lo
 00 |  SBV  |  SSV  |  SLV  |  SDV  |  SQV  |  SRV  |  SPV  |  SUV  |
 01 |  SHV  |  SFV  |  SWV  |  STV  |  ---  |  ---  |  ---  |  ---  |
 10 |  ---  |  ---  |  ---  |  ---  |  ---  |  ---  |  ---  |  ---  |
 11 |  ---  |  ---  |  ---  |  ---  |  ---  |  ---  |  ---  |  ---  |
 hi |-------|-------|-------|-------|-------|-------|-------|-------|

 */

namespace cor64.Mips.Rsp
{
    internal static class DecodeTable
    {
        private readonly static Opcode[] MainTable = {
            SPECIAL, REGIMM, J,     JAL,   BEQ,   BNE,   BLEZ,  BGTZ,
            ADDI,    ADDIU,  SLTI,  SLTIU, ANDI,  ORI,   XORI,  LUI,
            COP0,    NULL,   COP2,  NULL,  NULL,  NULL,  NULL,  NULL,
            NULL,    NULL,   NULL,  NULL,  NULL,  NULL,  NULL,  NULL,
            LB,      LH,     NULL,  LW,    LBU,   LHU,   NULL,  NULL,
            SB,      SH,     NULL,  SW,    NULL,  NULL,  NULL,  NULL,
            NULL,    NULL,   LWC2,  NULL,  NULL,  NULL,  NULL,  NULL,
            NULL,    NULL,   SWC2,  NULL,  NULL,  NULL,  NULL,  NULL
        };

        private readonly static Opcode[] Cop0Table = {
            MFC0,  NULL,  NULL, NULL, MTC0, NULL,  NULL, NULL,
            NULL,  NULL,  NULL, NULL, NULL, NULL,  NULL, NULL,
            NULL,  NULL,  NULL, NULL, NULL, NULL,  NULL, NULL,
            NULL,  NULL,  NULL, NULL, NULL, NULL,  NULL, NULL
        };
        
        private readonly static Opcode[] Cop2Table = {
            MFC2,  NULL,  CFC2,  NULL,  MTC2,  NULL,  CTC2,  NULL,
            NULL,  NULL,  NULL,  NULL,  NULL,  NULL,  NULL,  NULL,
            VU_OP, VU_OP, VU_OP, VU_OP, VU_OP, VU_OP, VU_OP, VU_OP,
            VU_OP, VU_OP, VU_OP, VU_OP, VU_OP, VU_OP, VU_OP, VU_OP
        };

        private readonly static Opcode[] VuTable = {
            VMULF,  VMULU,  VRNDP, VMULQ, VMUDL, VMUDM,  VMUDN, VMUDH,
            VMACF,  VMACU,  VRNDN, VMACQ, VMADL, VMADM,  VMADN, VMADH,
            VADD,   VSUB,   VSUT,  VABS,  VADDC, VSUBC,  VADDB, VSUBB,
            VACCB,  VSUCB,  VSAD,  VSAC,  VSUM,  VSAR,   VACC,  VSUC,
            VLT,    VEQ,    VNE,   VGE,   VCL,   VCH,    VCR,   VMRG,
            VAND,   VNAND,  VOR,   VNOR,  VXOR,  VNXOR,  V056,  V057,
            VRCP,   VRCPL,  VRCPH, VMOV,  VRSQ,  VRSQL,  VRSQH, VNOP,
            VEXTT,  VEXTQ,  VEXTN, V073,  VINST, VINSQ,  VINSN, VNULL
        };

        private readonly static Opcode[] RegImmTable = {
            BLTZ,   BGEZ,   NULL,   NULL,  NULL, NULL, NULL, NULL,
            NULL,   NULL,   NULL,   NULL,  NULL, NULL, NULL, NULL,
            BLTZAL, BGEZAL, NULL,   NULL,  NULL, NULL, NULL, NULL,
            NULL,   NULL,   NULL,   NULL,  NULL, NULL, NULL, NULL
        };

        private readonly static Opcode[] SpecialTable = {
            SLL,   NULL,  SRL,   SRA,     SLLV,    NULL,   SRLV,   SRAV,
            JR,    JALR,  NULL,  NULL,    NULL,    BREAK,  NULL,   NULL,
            NULL,  NULL,  NULL,  NULL,    NULL,    NULL,   NULL,   NULL,
            NULL,  NULL,  NULL,  NULL,    NULL,    NULL,   NULL,   NULL,
            ADD,   ADDU,  SUB,   SUBU,    AND,     OR,     XOR,    NOR,
            NULL,  NULL,  SLT,   SLTU,    NULL,    NULL,   NULL,   NULL,
            NULL,  NULL,  NULL,  NULL,    NULL,    NULL,   NULL,   NULL,
            NULL,  NULL,  NULL,  NULL,    NULL,    NULL,   NULL,   NULL
        };

        private readonly static Opcode[] VuLoadTable = {
            LBV,    LSV,    LLV,    LDV,   LQV,  LRV,  LPV,  LUV,
            LHV,    LFV,    LWV,    LTV,   NULL, NULL, NULL, NULL,
            NULL,   NULL,   NULL,   NULL,  NULL, NULL, NULL, NULL,
            NULL,   NULL,   NULL,   NULL,  NULL, NULL, NULL, NULL
        };

        private readonly static Opcode[] VuStoreTable = {
            SBV,    SSV,    SLV,    SDV,   SQV,  SRV,  SPV,  SUV,
            SHV,    SFV,    SWV,    STV,   NULL, NULL, NULL, NULL,
            NULL,   NULL,   NULL,   NULL,  NULL, NULL, NULL, NULL,
            NULL,   NULL,   NULL,   NULL,  NULL, NULL, NULL, NULL
        };

        public static Opcode OpcodeLookup(BinaryInstruction inst)
        {
            var op = MainTable[inst.op];

            switch (op.Family)
            {
                case OperationFamily.TBL_SPECIAL: return SpecialTable[inst.func];
                case OperationFamily.TBL_REGIMM: return RegImmTable[inst.rt];
                case OperationFamily.TBL_COP0: return Cop0Table[inst.rs];
                case OperationFamily.TBL_VU_LOAD: return VuLoadTable[inst.rd];
                case OperationFamily.TBL_VU_STORE: return VuStoreTable[inst.rd];

                case OperationFamily.TBL_COP2: {
                    var cop2op =  Cop2Table[inst.rs];

                    if (cop2op.Family == OperationFamily.TBL_VU) {
                        return VuTable[inst.func];
                    }
                    else {
                        return cop2op;
                    }
                }

                default: break;
            }

            return op;
        }
    }
}
