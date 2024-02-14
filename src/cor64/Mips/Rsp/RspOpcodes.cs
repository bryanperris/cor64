using System.Security.Cryptography.X509Certificates;
using static cor64.Mips.ExecutionFlags;
using static cor64.Mips.OperandType;
using static cor64.Mips.OperationFamily;
using static cor64.Mips.RegBoundType;
using static cor64.Mips.OpcodeFactory;
using static cor64.Mips.VectorOpFlags;

namespace cor64.Mips.Rsp
{
    public class RspOpcodes : OpcodesCommon
    {
        public static readonly Opcode VU_OP = Define(TBL_VU);
        public static readonly Opcode LWC2 = Define(TBL_VU_LOAD);
        public static readonly Opcode SWC2 = Define(TBL_VU_STORE);


        /* Reg moving */
        public static readonly Opcode SPMFC0 = Define(Cop0, "mfc0", RspCp0_CT, Cp0, Gpr, Data32);
        public static readonly Opcode SPMTC0 = Define(Cop0, "mtc0", RspCp0_TC, Gpr, Cp0, Data32);
        public static readonly Opcode MFC2 = Define(VU, "mfc2", VU_CT, Cp2, Gpr, Data32);
        public static readonly Opcode MTC2 = Define(VU, "mtc2", VU_TC, Gpr, Cp2, Data32);
        public static readonly Opcode CFC2 = Define(VU, "cfc2", VU_FromCtrl, Cp2Ctl, Gpr, Data32);
        public static readonly Opcode CTC2 = Define(VU, "ctc2", VU_ToCtrl, Gpr, Cp2Ctl, Data32);


        /* Other */
        public static readonly Opcode VSAR = Define(VU, "vsar", VU_STD);
        public static readonly Opcode VNOP = Define(VU, "vnop", Empty);


        /* Vector Multiplication */
        public static readonly Opcode VMULF = Define(VU, "vmulf", VU_STD);
        public static readonly Opcode VMACF = Define(VU, "vmacf", VU_STD, Accumulate);
        public static readonly Opcode VMULU = Define(VU, "vmulu", VU_STD, Unsigned);
        public static readonly Opcode VMACU = Define(VU, "vmacu", VU_STD, Unsigned, Accumulate);
        public static readonly Opcode VMUDL = Define(VU, "vmudl", VU_STD, Unsigned, PartialL);
        public static readonly Opcode VMADL = Define(VU, "vmadl", VU_STD, Unsigned, Accumulate | PartialL);
        public static readonly Opcode VMUDM = Define(VU, "vmudm", VU_STD, PartialM);
        public static readonly Opcode VMADM = Define(VU, "vmadm", VU_STD, Accumulate | PartialM);
        public static readonly Opcode VMUDN = Define(VU, "vmudn", VU_STD, PartialN);
        public static readonly Opcode VMADN = Define(VU, "vmadn", VU_STD, Accumulate | PartialN);
        public static readonly Opcode VMUDH = Define(VU, "vmudh", VU_STD, PartialH);
        public static readonly Opcode VMADH = Define(VU, "vmadh", VU_STD, Accumulate | PartialH);


        /* MPEG Stuff */
        public static readonly Opcode VMULQ = Define(VU, "vmulq", VU_STD, Quantized);
        public static readonly Opcode VMACQ = Define(VU, "vmacq", VU_STD, Accumulate | Quantized);
        public static readonly Opcode VRNDP = Define(VU, "vrndp", VU_STD, DctRoundPositive);
        public static readonly Opcode VRNDN = Define(VU, "vrndn", VU_STD, DctRoundNegative);


        public static readonly Opcode VADD  = Define(VU, "vadd", VU_STD);
        public static readonly Opcode VSUB  = Define(VU, "vsub", VU_STD);
        public static readonly Opcode VADDC = Define(VU, "vaddc", VU_STD, Carry);
        public static readonly Opcode VSUBC = Define(VU, "vsubc", VU_STD, Carry);


        /* Vector BitWise */
        public static readonly Opcode VABS  = Define(VU, "vabs", VU_STD, ArithmeticOp.ABSOLUTE);
        public static readonly Opcode VMOV  = Define(VU, "vmov", VU_STD, ArithmeticOp.MOVE);
        public static readonly Opcode VAND  = Define(VU, "vand", VU_STD, ArithmeticOp.AND);
        public static readonly Opcode VNAND = Define(VU, "vnand", VU_STD, ArithmeticOp.NAND);
        public static readonly Opcode VOR   = Define(VU, "vor", VU_STD, ArithmeticOp.OR);
        public static readonly Opcode VNOR  = Define(VU, "vnor", VU_STD, ArithmeticOp.NOR);
        public static readonly Opcode VXOR  = Define(VU, "vxor", VU_STD, ArithmeticOp.XOR);
        public static readonly Opcode VNXOR = Define(VU, "vnxor", VU_STD, ArithmeticOp.NXOR);


        /* Vector Compare */
        public static readonly Opcode VEQ = Define(VU, "veq", VU_STD, ArithmeticOp.EQUAL);
        public static readonly Opcode VNE = Define(VU, "vne", VU_STD, ArithmeticOp.NOT_EQUAL);
        public static readonly Opcode VLT = Define(VU, "vlt", VU_STD, ArithmeticOp.LESS_THAN_OR_EQUAL);
        public static readonly Opcode VGE = Define(VU, "vge", VU_STD, ArithmeticOp.GREATER_THAN_OR_EQUAL);


        /* Vector Clipping */
        public static readonly Opcode VCL = Define(VU, "vcl", VU_STD, PartialL);
        public static readonly Opcode VCH = Define(VU, "vch", VU_STD, PartialH);
        public static readonly Opcode VCR = Define(VU, "vcr", VU_STD, Crimp);
        public static readonly Opcode VMRG = Define(VU, "vmrg", VU_STD, Merge);


        /* Vector Reciprocal */
        public static readonly Opcode VRCP = Define(VU, "vrcp", VU_SE_DE);
        public static readonly Opcode VRCPL = Define(VU, "vrcpl", VU_SE_DE, PartialL);
        public static readonly Opcode VRCPH = Define(VU, "vrcph", VU_SE_DE, PartialH);


        /* Vector SquareRoot Reciprocal */
        public static readonly Opcode VRSQ = Define(VU, "vrsq", VU_SE_DE, SquareRoot);
        public static readonly Opcode VRSQL = Define(VU, "vrsql", VU_SE_DE, SquareRoot | PartialL);
        public static readonly Opcode VRSQH = Define(VU, "vrsqh", VU_SE_DE, SquareRoot | PartialH);


        /* Vector Unit Load Opcodes */
        public static readonly Opcode LBV = Define(VU, "lbv", VU_LOADSTORE, Data8);   // Load uint8 into vector[e]
        public static readonly Opcode LSV = Define(VU, "lsv", VU_LOADSTORE, Data16);  // Load uint16 into vector[e]
        public static readonly Opcode LLV = Define(VU, "llv", VU_LOADSTORE, Data32);  // Load uint32 into vector[e]
        public static readonly Opcode LDV = Define(VU, "ldv", VU_LOADSTORE, Data64);   // Load uint64 into vector[e]
        public static readonly Opcode LQV = Define(VU, "lqv", VU_LOADSTORE, Data128); // Load uint128 into vector
        public static readonly Opcode LRV = Define(VU, "lrv", VU_LOADSTORE, Rest); // Load rest uint128 into vector
        public static readonly Opcode LPV = Define(VU, "lpv", VU_LOADSTORE, Upper); // [Packed] Load 8 bytes into the upper region of each element
        public static readonly Opcode LUV = Define(VU, "luv", VU_LOADSTORE, Unsigned); // [Packed] loads every unsigned into vector
        public static readonly Opcode LHV = Define(VU, "lhv", VU_LOADSTORE, Half); // [Packed] load every half into vector
        public static readonly Opcode LFV = Define(VU, "lfv", VU_LOADSTORE, Forth); // [Packed] load every forth into vector[e]
        public static readonly Opcode LWV = Define(VU, "lwv", VU_LOADSTORE, Wrap);  // Load wraped into vector (undocumented)
        public static readonly Opcode LTV = Define(VU, "ltv", VU_LOADSTORE, Transpose); // Load transpose into vector[e]


        /* Vector Unit Store Opcodes */
        public static readonly Opcode SBV = Define(VU, "sbv", VU_LOADSTORE, Data8);
        public static readonly Opcode SSV = Define(VU, "ssv", VU_LOADSTORE, Data16);
        public static readonly Opcode SLV = Define(VU, "slv", VU_LOADSTORE, Data32);
        public static readonly Opcode SDV = Define(VU, "sdv", VU_LOADSTORE, Data64);
        public static readonly Opcode SQV = Define(VU, "sqv", VU_LOADSTORE, Data128);
        public static readonly Opcode SRV = Define(VU, "srv", VU_LOADSTORE, Rest);
        public static readonly Opcode SPV = Define(VU, "spv", VU_LOADSTORE, Upper);
        public static readonly Opcode SUV = Define(VU, "suv", VU_LOADSTORE, Unsigned);
        public static readonly Opcode SHV = Define(VU, "shv", VU_LOADSTORE, Half);
        public static readonly Opcode SFV = Define(VU, "sfv", VU_LOADSTORE, Forth);
        public static readonly Opcode SWV = Define(VU, "swv", VU_LOADSTORE, Wrap);
        public static readonly Opcode STV = Define(VU, "stv", VU_LOADSTORE, Transpose);


        /* Reserved (Unsupported) */
        public static readonly Opcode VSUT = Define(VU, "vsut", VUFMT);
        public static readonly Opcode VADDB = Define(VU, "vaddb", VUFMT);
        public static readonly Opcode VSUBB = Define(VU, "vsubb", VUFMT);
        public static readonly Opcode VACCB = Define(VU, "vaccb", VUFMT);
        public static readonly Opcode VSUCB = Define(VU, "vsucb", VUFMT);
        public static readonly Opcode VSAD = Define(VU, "vsad", VUFMT);
        public static readonly Opcode VSAC = Define(VU, "vsac", VUFMT);
        public static readonly Opcode VSUM = Define(VU, "vsum", VUFMT);
        public static readonly Opcode VACC = Define(VU, "vacc", VUFMT);
        public static readonly Opcode VSUC = Define(VU, "vsuc", VUFMT);
        public static readonly Opcode V056 = Define(VU, "v056", VUFMT);
        public static readonly Opcode V057 = Define(VU, "v057", VUFMT);
        public static readonly Opcode VEXTT = Define(VU, "vextt", VUFMT);
        public static readonly Opcode VEXTQ = Define(VU, "vextq", VUFMT);
        public static readonly Opcode VEXTN = Define(VU, "vextn", VUFMT);
        public static readonly Opcode V073 = Define(VU, "v073", VUFMT);
        public static readonly Opcode VINST = Define(VU, "vinst", VUFMT);
        public static readonly Opcode VINSQ = Define(VU, "vinsq", VUFMT);
        public static readonly Opcode VINSN = Define(VU, "vinsn", VUFMT);
        public static readonly Opcode VNULL = Define(VU, "vnull", VUFMT);
    }
}