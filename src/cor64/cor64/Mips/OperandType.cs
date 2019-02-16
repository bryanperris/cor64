using System;
namespace cor64.Mips
{
    public enum OperandType : int
    {
        Empty = 0,
        JUMP,
        I_STI,
        I_TSI,
        I_TI,
        I_TIS,
        I_SI,
        I_FTIS,
        Cop0_CT,
        Cop0_TC,
        TLB,
        BCZ,
        Cop1_DST,
        Cop1_DS,
        Cop1_TS,
        Cop1_TC,
        Cop1_CT,
        R_DTA,
        R_DTS,
        R_S,
        R_SD,
        R_D,
        R_ST,
        R_DST,
        Code,
        Sync,
        TrapWithCode
    }
}
