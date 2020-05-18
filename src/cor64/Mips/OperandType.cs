using System;
namespace cor64.Mips
{
    public enum OperandType : int
    {
        Empty = 0,
        JUMP,
        BRANCH,
        I_TSI,
        I_TI,
        I_TIS,
        I_SI,
        I_FTIS,

        Cop0_CT,
        Cop0_TC,


        VU_STD,
        VU_SE_DE,
        VU_LOADSTORE,
        VU_CT,
        VU_TC,
        VU_FromCtrl,
        VU_ToCtrl,
        TLB,

        Bc1,

        Cop1_DST,
        Cop1_DS,
        Cop1_TS,
        Cop1_TC,
        Cop1_CT,
        Cop1_FromCtrl,
        Cop1_ToCtrl,

        R_DTA,
        R_DTS,
        R_S,
        R_SD,
        R_D,
        R_ST,
        R_DST,
        Code,
        Sync,
        TrapWithCode,
        VUFMT
    }
}
