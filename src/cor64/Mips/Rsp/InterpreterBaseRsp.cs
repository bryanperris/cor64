using System;
using System.IO;
using System.Text;
using cor64.Debugging;
using cor64.RCP;
using static cor64.Mips.OpcodesCommon;
using static cor64.Mips.R4300I.Opcodes;
using static cor64.Mips.Rsp.RspOpcodes;
using cor64.IO;

namespace cor64.Mips.Rsp
{
    public abstract class InterpreterBaseRsp : BaseInterpreter, IMipsOpcodes
    {
        protected readonly RspOpcodes.CallTable CallTable = RspOpcodes.CreateCallTable();
        public SPInterface Interface { get; private set; }
        protected DPCInterface RdpInterface {get; private set; }
        protected MipsInterface RcpInterface { get; private set; }
        protected cor64.HLE.GraphicsHLEDevice HLEGraphicsDevice { get; private set; }
        protected SPStatusRegister Status { get; private set; }
        public virtual bool IsHalted { get; protected set; } = true;
        public DecodedInstruction LastReadInst { get; protected set; }

        public RspMemorySection IMEM { get; private set; }
        public RspMemorySection DMEM { get; private set; }

        private readonly MipsDebugger m_Debugger = new MipsDebugger();

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
            .Map(VectorAccumulatorReadWrite, VSAR)
            .Map(VectorBitwise, VABS, VAND, VNAND, VOR, VNOR, VXOR, VNXOR)
            .Map(VectorReciprocal, VRCP, VRCPL, VRCPH, VRSQ, VRSQL, VRSQH)
            .Map(VectorCompare, VEQ, VNE, VLT, VGE)
            .Map(VectorClip, VCL, VCH, VCR, VMRG)
            .Map(VectorMove, VMOV)
            .Finish();

            if (CoreConfig.Current.WorkbenchMode) {
                Debugger.ActivateDebugger();
            }
        }

        private void NothingOp(DecodedInstruction inst) {

        }

        public abstract void WriteVCC(ushort vcc);

        public abstract void WriteVC0(ushort vc0);

        public override MipsDebugger Debugger => m_Debugger;

        private sealed class DataStreamWrapper : Stream
        {
            private readonly InterpreterBaseRsp m_Core;
            private readonly Stream m_BaseStream;

            public DataStreamWrapper(InterpreterBaseRsp core, Stream stream)
            {
                m_BaseStream = stream;
                m_Core = core;
            }

            public override long Length => m_BaseStream.Length;

            public override long Position {
                get => m_BaseStream.Position;
                set => m_BaseStream.Position = value & 0xFFF;
            }

            public override bool CanRead => m_BaseStream.CanRead;

            public override bool CanSeek => m_BaseStream.CanSeek;

            public override bool CanWrite => m_BaseStream.CanSeek;

            public override int Read(byte[] buffer, int offset, int count)
            {
                var read = m_BaseStream.Read(buffer, offset, count);

                if (m_Core.IsMemTraceActive || m_Core.InstDebugMode != InstructionDebugMode.None)
                {
                    var addr = 0x04000000 | (uint)m_BaseStream.Position;
                    m_Core.TraceMemoryHit(addr, false, DebugValue(buffer, offset, count));
                }

                return read;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                m_BaseStream.Write(buffer, offset, count);

                if (m_Core.IsMemTraceActive || m_Core.InstDebugMode != InstructionDebugMode.None)
                {
                    var addr = 0x04000000 | (uint)m_BaseStream.Position;
                    m_Core.TraceMemoryHit(addr, true, DebugValue(buffer, offset, count));
                }
            }

            private static String DebugValue(byte[] buffer, int offset, int size)
            {
                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < size; i++)
                {
                    sb.Append(buffer[offset + i].ToString("X2"));
                }

                return sb.ToString();
            }

            public override void Flush()
            {
                m_BaseStream.Flush();
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return m_BaseStream.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                m_BaseStream.SetLength(value);
            }

            public override int ReadByte()
            {
                return m_BaseStream.ReadByte();
            }

            public override void WriteByte(byte value)
            {
                m_BaseStream.WriteByte(value);
            }
        }

        public abstract void Init();

        public virtual void AttachInterface(MipsInterface rcpInterface, SPInterface iface, DPCInterface rdpInterface) {
            Interface = iface;
            Status = iface.Status;
            RdpInterface = rdpInterface;
            RcpInterface = rcpInterface;
        }

        public virtual void AttachHLEGraphics(cor64.HLE.GraphicsHLEDevice device) {
            HLEGraphicsDevice = device;
        }

        public virtual void AttachMemory(N64MemoryController memory) {
            DMEM = new RspMemorySection(memory, 0x04000000);
            IMEM = new RspMemorySection(memory, 0x04001000);
        }

        public virtual void Halt() {
            IsHalted = true;
        }

        public abstract void ManualStart(long pc);

        /********************************************************
        * Branch Unit Logic
        ********************************************************/

        public bool TakeBranch { get; set; }

        public bool BranchDelay { get; set; }

        public long TargetAddress { get; set; }

        protected bool UnconditionalJump { get; set; }

        public bool NullifyNext { get; set; }

        public bool WillJump => TakeBranch || UnconditionalJump;

        public bool BranchDelaySlot => BranchDelay;

        public long BranchTarget => TargetAddress;

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
        public abstract void VectorAccumulatorReadWrite(DecodedInstruction inst);
        public abstract void VectorBitwise(DecodedInstruction inst);
        public abstract void VectorReciprocal(DecodedInstruction inst);
        public abstract void VectorCompare(DecodedInstruction inst);
        public abstract void VectorClip(DecodedInstruction inst);
        public abstract void VectorMove(DecodedInstruction inst);
    }
}