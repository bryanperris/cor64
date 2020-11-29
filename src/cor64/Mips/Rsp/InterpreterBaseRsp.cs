using System.IO;
using cor64.RCP;
using static cor64.Mips.OpcodesCommon;
using static cor64.Mips.R4300I.Opcodes;
using static cor64.Mips.Rsp.RspOpcodes;

namespace cor64.Mips.Rsp
{
    public abstract class InterpreterBaseRsp : BaseInterpreter, IMipsOpcodes
    {
        public DataMemory DMem { get; private set; }
        protected readonly RspOpcodes.CallTable CallTable = RspOpcodes.CreateCallTable();
        protected SPInterface Interface { get; private set; }
        protected DPCInterface RdpInterface {get; private set; }
        protected MipsInterface RcpInterface { get; private set; }
        protected SPStatusRegister Status { get; private set; }
        public bool IsHalted { get; protected set; } = true;
        public DecodedInstruction LastReadInst { get; protected set; }

        protected InterpreterBaseRsp(BaseDisassembler disassembler) : base(disassembler)
        {
            CallTable
            .Map(Add, ADD, ADDU, ADDI, ADDIU)
            .Map(BitwiseLogic, AND, ANDI, OR, ORI, NOR, XOR, XORI)
            .Map(Shift, SLLV, SRLV, SRAV, SLL, SRL, SRA)
            .Map(Subtract, SUB, SUBU)
            .Map(SetOnLessThan, SLT, SLTI, SLTIU, SLTU)
            .Map(TransferReg, SPMTC0, SPMFC0, MTC2, MFC2, CTC2, CFC2)
            .Map(Branch, BEQ, BGEZ, BGEZAL, BGTZ, BLEZ, BLTZ, BLTZAL, BNE)
            .Map(Jump, J, JAL, JR, JALR)
            .Map(Store, SB, SH, SW)
            .Map(Load, LUI, LB, LBU, LH, LHU, LW, LWU)
            .Map(Break, BREAK)
            .Map(VectorUnitReserved, VSUT, VADDB, VSUBB, VEXTT, VEXTQ, VEXTN, V073, VINST, VINSQ, VINSN, VNULL, V056, V057, VMULQ, VMACQ, VRNDP, VRNDN)
            .Map(VectorLoad, LBV, LSV, LLV, LDV, LQV, LRV, LPV, LUV, LHV, LFV, LWV, LTV)
            .Map(VectorStore, SBV, SSV, SLV, SDV, SQV, SRV, SPV, SUV, SHV, SFV, SWV, STV)
            .Map(VectorAdd, VADD, VADDC)
            .Map(VectorSubtract, VSUB, VSUBC)
            .Map(VectorMultiply, VMULF, VMULU, VMUDL, VMUDM, VMUDN, VMUDH, VMACF, VMACU, VMADL, VMADM, VMADN, VMADH)
            .Map(VectorAccumulatorRead, VSAR)
            .Map(VectorBitwise, VABS, VMOV, VAND, VNAND, VOR, VNOR, VXOR, VNXOR)
            .Map(VectorReciprocal, VRCP, VRCPL, VRCPH, VRSQ, VRSQL, VRSQH)
            .Map(VectorCompare, VEQ, VNE, VLT, VGE)
            .Map(VectorClip, VCL, VCH, VCR, VMRG)
            .Finish();
        }

        public abstract void WriteVCC(ushort vcc);

        public override void AttachDStream(Stream memoryStream) {
            DMem = new DataMemory(memoryStream);
        }

        public abstract void Init();

        public virtual void AttachInterface(MipsInterface rcpInterface, SPInterface iface, DPCInterface rdpInterface) {
            Interface = iface;
            Status = iface.Status;
            RdpInterface = rdpInterface;
            RcpInterface = rcpInterface;
        }

        public virtual void Halt() {
            IsHalted = true;
        }

        public abstract void ManualStart(ulong pc);

        /********************************************************
        * Branch Unit Logic
        ********************************************************/

        public bool TakeBranch { get; set; }

        public bool BranchDelay { get; set; }

        public ulong TargetAddress { get; set; }

        protected bool UnconditionalJump { get; set; }

        public bool NullifyNext { get; set; }

        public bool WillJump => TakeBranch || UnconditionalJump;

        public bool BranchDelaySlot => BranchDelay;

        public ulong BranchTarget => TargetAddress;

        public void ClearBranchUnit()
        {
            TargetAddress = 0;
            BranchDelay = false;
            UnconditionalJump = false;
            TakeBranch = false;
            NullifyNext = false;
        }

        /********************************************************
        * State Logic
        ********************************************************/

        public void Writeback(int reg, uint value) {
            State.SetGpr32(reg, value);
        }

        public uint ReadGPR(int reg) {
            return State.GetGpr32(reg);
        }

        public void WriteGPR(int reg, uint value) {
            State.SetGpr32(reg, value);
        }

        public abstract ushort ReadVCO();

        public abstract ushort ReadVCC();

        public abstract byte ReadVCE();

        public Accumulator Acc { get; } = new Accumulator();

        /********************************************************
        * Execution Logic
        ********************************************************/

        public abstract void Add(DecodedInstruction inst);
        public abstract void BitwiseLogic(DecodedInstruction inst);
        public abstract void Branch(DecodedInstruction inst);
        public abstract void Jump(DecodedInstruction inst);
        public abstract void Load(DecodedInstruction inst);
        public abstract void SetOnLessThan(DecodedInstruction inst);
        public abstract void Shift(DecodedInstruction inst);
        public abstract void Store(DecodedInstruction inst);
        public abstract void Subtract(DecodedInstruction inst);
        public abstract void TransferReg(DecodedInstruction inst);
        public abstract void Break(DecodedInstruction inst);
        public abstract void VectorUnitReserved(DecodedInstruction inst);
        public abstract void VectorLoad(DecodedInstruction inst);
        public abstract void VectorStore(DecodedInstruction inst);
        public abstract void VectorAdd(DecodedInstruction inst);
        public abstract void VectorSubtract(DecodedInstruction inst);
        public abstract void VectorMultiply(DecodedInstruction inst);
        public abstract void VectorAccumulatorRead(DecodedInstruction inst);
        public abstract void VectorBitwise(DecodedInstruction inst);
        public abstract void VectorReciprocal(DecodedInstruction inst);
        public abstract void VectorCompare(DecodedInstruction inst);
        public abstract void VectorClip(DecodedInstruction inst);
    }
}