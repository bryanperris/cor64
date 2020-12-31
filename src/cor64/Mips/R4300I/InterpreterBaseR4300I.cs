using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using cor64.Debugging;
using cor64.IO;
using cor64.RCP;
using NLog;
using static cor64.Cartridge;

using static cor64.Mips.OpcodesCommon;
using static cor64.Mips.R4300I.Opcodes;

namespace cor64.Mips.R4300I
{
    public abstract class InterpreterBaseR4300I : BaseInterpreter, IMipsOpcodes
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        protected readonly Opcodes.CallTable CallTable = Opcodes.CreateCallTable();
        private SystemController m_Coprocessor0;
        private Clock m_CoreClock;
        protected DataMemory m_DataMemory;
        public bool StartedWithProfiler { get; protected set; }
        private CoreDebugger m_CoreDebugger = new CoreDebugger();

        private readonly BranchUnit m_BranchUnit = new BranchUnit();


        /* TODO: When we do have a cache system, 
         * some conditions of the cache can break
         * atomic operations (LLBit is set back to 0)
         */

        /* Debugging */

        protected InterpreterBaseR4300I(BaseDisassembler disassembler) : base(disassembler)
        {
            // Attach core debugger
            State.SetCoreDebugger(m_CoreDebugger);

            m_CoreClock = new Clock(10, 2);
            m_Coprocessor0 = new SystemController(State, m_CoreClock);

            CallTable
            .Map(Add32, ADD, ADDU, ADDI, ADDIU)
            .Map(BitwiseLogic, AND, ANDI, OR, ORI, NOR, XOR, XORI)
            .Map(Add64, DADD, DADDI, DADDIU, DADDU)
            .Map(Divide32, DIV, DIVU)
            .Map(Divide64, DDIV, DDIVU)
            .Map(Multiply32, MULT, MULTU)
            .Map(Multiply64, DMULT, DMULTU)
            .Map(Shift32, SLLV, SRLV, SRAV, SLL, SRL, SRA)
            .Map(Shift64, DSLLV, DSRLV, DSRAV, DSLL, DSRL, DSRA, DSLL32, DSRL32, DSRA32)
            .Map(Subtract32, SUB, SUBU)
            .Map(Subtract64, DSUB, DSUBU)
            .Map(SetOnLessThan, SLT, SLTI, SLTIU, SLTU)
            .Map(TransferReg, MFHI, MFLO, MTHI, MTLO, MTC0, MTC1, MFC0, MFC1, DMTC0, DMTC1, DMFC0, DMFC1, CTC1, CFC1)
            .Map(Branch, BC1F, BC1FL, BC1T, BC1TL, BEQ, BEQL, BGEZ, BGEZAL, BGEZALL, BGEZL, BGTZ, BGTZL, BLEZ, BLEZL, BLTZ, BLTZAL, BLTZALL, BLTZL, BNE, BNEL)
            .Map(Jump, J, JAL, JR, JALR)
            .Map(Store, SB, SH, SD, SW, SWL, SWR, SC, SCD, SDL, SDR)
            .Map(Load, LUI, LB, LBU, LH, LHU, LW, LWU, LWL, LWR, LD, LDL, LDR, LL, LLD)
            .Map(Cache, CACHE)
            .Map(Sync, SYNC)
            .Map(ExceptionReturn, ERET)
            .Map(Trap, TGEI, TGEIU, TLTI, TLTIU, TEQI, TNEI, TGE, TGEU, TLT, TLTU, TEQ, TNE)
            .Map(Break, BREAK)

            /* FPU Hooks */
            .Map(FloatLoad, LDC1, LWC1)
            .Map(FloatStore, SWC1, SDC1)
            .Map(Add, ADD_FPU)
            .Map(Subtract, SUB_FPU)
            .Map(Multiply, MUL_FPU)
            .Map(Divide, DIV_FPU)
            .Map(SqrRoot, SQRT)
            .Map(Abs, ABS)
            .Map(Mov, MOV)
            .Map(Neg, NEG)
            .Map(Round, ROUND_L, ROUND_W)
            .Map(Truncate, TRUNC_L, TRUNC_W)
            .Map(Ceil, CEIL_L, CEIL_W)
            .Map(Floor, FLOOR_L, FLOOR_W)
            .Map(Convert, CVT_D, CVT_L, CVT_S, CVT_W)
            .Map(Condition, C_F, C_UN, C_EQ, C_UEQ, C_OLT, C_ULT, C_OLE, C_ULE, C_SF, C_NGLE, C_SEQ, C_NGL, C_LT, C_NGE, C_LE, C_NGT)

            /* Things on the ignore list until future implentation */
            .Map(InstructionIgnore, TLBWI, TLBP, TLBR)

            .Finish();
        }

        public void AttachRcp(RcpCore rcp)
        {
            m_Coprocessor0.AttachInterface(rcp.RcpInterface);
        }

        protected override void EntryPointHit() {
            base.EntryPointHit();

            /* Report RDRAM Size */
            var traceMode = TraceMode;
            SetTraceMode(Analysis.ProgramTrace.TraceMode.None);
            m_DataMemory.Data32 = 0x00800000;
            m_DataMemory.WriteData(0x80000318, 4);
            SetTraceMode(traceMode);
        }

        /********************************************************
         * Processor Core Logic
         ********************************************************/

        protected ulong ReadCp0Value(int select, bool isDwordInst)
        {
            var value = Cop0.CpuRegisterRead(select);

            /* If running 64-bit mode, with the 32-bit version, then sign extend */
            if (IsOperation64 && !isDwordInst)
            {
                value = (ulong)(int)(uint)value;
            }

            return value;
        }

        /********************************************************
         * Core FPU Logic
         ********************************************************/

        protected void SetExceptionState(FpuExceptionFlags flags)
        {
            State.FCR.Cause |= flags;
        }

        public void ClearExceptionFlags()
        {
            State.FCR.Cause = 0;
        }

        /********************************************************
         * Branch Unit Logic
         ********************************************************/

        protected BranchUnit BranchControl => m_BranchUnit;

        public bool TakeBranch {
            get => m_BranchUnit.Take;
            set => m_BranchUnit.Take = value;
        }

        public bool BranchDelay{
            get => m_BranchUnit.DelaySlot;
            set => m_BranchUnit.DelaySlot = value;
        }

        public ulong TargetAddress {
            get => m_BranchUnit.Target;
            set => m_BranchUnit.Target = value;
        }

        protected bool UnconditionalJump {
            get => m_BranchUnit.Unconditonal;
            set => m_BranchUnit.Unconditonal = value;
        }

        public bool NullifyNext {
            get => m_BranchUnit.NullifyNext;
            set => m_BranchUnit.NullifyNext = value;
        }

        public bool WillJump => TakeBranch || UnconditionalJump;

        public bool BranchDelaySlot => BranchDelay;

        public ulong BranchTarget => TargetAddress;

        public void ClearBranchUnit()
        {
            m_BranchUnit.ResetAll();
        }

        /********************************************************
         * Processor State Properties
         ********************************************************/

        public SystemController Cop0 => m_Coprocessor0;

        public ExceptionType Exceptions => State.Cp0.Cause.Exception;

        public bool IsAddress64 => State.Cp0.Status.IsAddress64;

        public bool IsOperation64 {
            get {

                #if CPU_FORCE_32
                return false;
                #else
                return State.Cp0.Status.IsOperation64;
                #endif
            }
        }

        public Clock CoreClock => m_CoreClock;

        public DataMemory DataMem => m_DataMemory;


        /********************************************************
         * State Interface
         ********************************************************/

        public void OverrideCop0(SystemController cop0)
        {
            m_Coprocessor0 = cop0;
        }

        protected uint ReadGPR32(int i)
        {
            return State.GetGpr32(i);
        }

        public ulong ReadGPR64(int i)
        {
            return State.GetGpr64(i);
        }

        public ulong ReadRA()
        {
            return State.GetGpr64(31);
        }

        public void SetOperation64Mode(bool mode)
        {
            State.Cp0.Status.DebugSet_Operation64(mode);
        }

        public void SetExceptionState(ExceptionType type)
        {
            State.LLBit = false; // All exceptions cause atomic operations to fail
            State.Cp0.Cause.SetException(type);
        }

        protected void Writeback32(int reg, ulong value)
        {
            if (IsOperation64)
            {
                State.SetGpr64(reg, (ulong)(int)value);
            }
            else
            {
                State.SetGpr32(reg, (uint)value);
            }
        }

        protected void Writeback64(int reg, ulong value)
        {
            State.SetGpr64(reg, value);
        }

        protected void WritebackHiLo32(ulong hi, ulong lo)
        {
            if (IsOperation64)
            {
                State.SetHi((ulong)(int)hi);
                State.SetLo((ulong)(int)lo);
            }
            else
            {
                State.SetHi((uint)hi);
                State.SetLo((uint)lo);
            }
        }

        protected void WritebackHiLo64(ulong hi, ulong lo)
        {
            State.SetHi(hi);
            State.SetLo(lo);
        }

        protected void WriteGPR64(int select, ulong value)
        {
            State.SetGpr64(select, value);
        }

        protected void WriteGPR32(int select, uint value)
        {
            State.SetGpr32(select, value);
        }

        protected float ReadFPR_S(int select)
        {
            return State.GetFprS(select);
        }

        protected double ReadFPR_D(int select)
        {
            return State.GetFprD(select);
        }

        protected uint ReadFPR_W(int select)
        {
            return State.GetFprW(select);
        }

        protected ulong ReadFPR_DW(int select)
        {
            return State.GetFprDW(select);
        }

        protected void WriteFPR_DW(int select, ulong value)
        {
            State.SetFprDW(select, value);
        }

        protected void WriteFPR_W(int select, uint value)
        {
            State.SetFprW(select, value);
        }

        protected void WriteFPR_D(int select, double value)
        {
            State.SetFprD(select, RoundHelper.Round(value, State.FCR.RoundMode));
        }

        protected void WriteFPR_S(int select, float value)
        {
            State.SetFprS(select, RoundHelper.Round(value, State.FCR.RoundMode));
        }

        protected void WriteFPR_DNR(int select, double value)
        {
            State.SetFprD(select, value);
        }

        protected void WriteFPR_SNR(int select, float value)
        {
            State.SetFprS(select, value);
        }

        protected ulong ReadHi() => State.GetHi();

        protected ulong ReadLo() => State.GetLo();

        protected void WriteHi(ulong value) => State.SetHi(value);

        protected void WriteLo(ulong value) => State.SetLo(value);

        public override void AttachBootManager(BootManager bootManager)
        {
            bootManager.PCWrite += (pc) =>
            {
                PC = pc;
            };

            bootManager.MemWrite += (addr, val) =>
            {
                var debugMode = DebugMode;
                SetDebuggingMode(false);

                /* Incoming values are expected to be big-endian */

                if (CoreConfig.Current.ByteSwap) {
                    m_DataMemory.Data32 = val.ByteSwapped();
                }
                else {
                    m_DataMemory.Data32 = val;
                }

                m_DataMemory.WriteData(addr, 4);

                SetDebuggingMode(debugMode);
            };

            bootManager.RegWrite += (off, val) =>
            {
                State.SetGpr64(off, val);
            };

            bootManager.CP0Write += (i, x) => State.Cp0.Write(i, x);
        }

        /********************************************************
         * Memory Logic
         ********************************************************/

        #region Memory Logic

        /* For testing-purposes */
        public bool BypassMMU { get; set; }

        public override void AttachIStream(Stream memoryStream)
        {
            base.AttachIStream(new SimpleVMemStream(this, memoryStream, false));
        }

        public void OverrideIStream(Stream memoryStream)
        {
            base.AttachIStream(memoryStream);
        }

        public override void AttachDStream(Stream memoryStream)
        {
            m_DataMemory = new DataMemory(new SimpleVMemStream(this, memoryStream, true));
        }

        public void OverrideDStream(DataMemory dataMemory)
        {
            m_DataMemory = dataMemory;
        }

        protected DataMemory DMemoryStream => m_DataMemory;

        private long TLBTranslate(long address)
        {
            throw new NotImplementedException(String.Format("TLB Mapped Memory not yet supported: {0:X8}", address));
        }

        private sealed class SimpleVMemStream : VMemStream
        {
            private readonly InterpreterBaseR4300I m_Core;
            private bool m_UseCache;
            private bool m_IsDataPath;
            private readonly uint m_Size = 0xFFFFFFFF;
            private readonly Stream m_BaseStream;

            public SimpleVMemStream(InterpreterBaseR4300I core, Stream stream, bool isDataPath) : base(stream)
            {
                m_BaseStream = stream;
                m_Core = core;
                m_IsDataPath = isDataPath;

                if (m_Core.BypassMMU)
                {
                    m_Size = (uint)stream.Length;
                }
            }

            public override long Length => m_Size;

            public override long Position { get; set; }

            public override int Read(byte[] buffer, int offset, int count)
            {
                var read = base.Read(buffer, offset, count);

                if ((m_Core.IsMemTraceActive || m_Core.InstDebugMode != InstructionDebugMode.None) && m_IsDataPath)
                {
                    m_Core.TraceMemoryHit((uint)(ulong)m_BaseStream.Position, false, DebugValue(buffer, offset, count));
                }

                return read;
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                base.Write(buffer, offset, count);

                if ((m_Core.IsMemTraceActive || m_Core.InstDebugMode != InstructionDebugMode.None) && m_IsDataPath)
                {
                    m_Core.TraceMemoryHit((uint)(ulong)m_BaseStream.Position, true, DebugValue(buffer, offset, count));
                }
            }

            private String DebugValue(byte[] buffer, int offset, int size)
            {
                StringBuilder sb = new StringBuilder();

                for (int i = 0; i < size; i++)
                {
                    sb.Append(buffer[offset + i].ToString("X2"));
                }

                return sb.ToString();
            }

            protected override long TranslateAddress(long address)
            {
                if (m_Core.BypassMMU)
                    return address;

                m_UseCache = false;

                /* Kernel VMap 32-bit map */
                // TODO: handle maps for user and supervisor mode
                // TODO: Should somehow put the mapping logic in conf files

                if (m_Core.m_Coprocessor0.IsSupervisorMode)
                {
                    throw new NotSupportedException("Supervisor vmap not yet supported");
                }
                else if (m_Core.m_Coprocessor0.IsUserMode)
                {
                    throw new NotSupportedException("User vmap not yet supported");
                }
                else
                {
                    byte index = (byte)((uint)address >> 28);

                    switch (index)
                    {
                        default: throw new ArgumentException("invalid vmem address");
                        case 0x0:
                        case 0x1:
                        case 0x2:
                        case 0x3:
                        case 0x4:
                        case 0x5:
                        case 0x6:
                        case 0x7: return m_Core.TLBTranslate(address);
                        case 0x8:
                        case 0x9: m_UseCache = true; return address - 0x80000000;
                        case 0xA:
                        case 0xB: return address - 0xA0000000;
                        case 0xC:
                        case 0xD:
                        case 0xE:
                        case 0xF: return m_Core.TLBTranslate(address);
                    }
                }
            }
        }

        #endregion

        /********************************************************
         * Debugger Interface
         ********************************************************/

        public override string GetGPRName(int i)
        {
            return ABI.GetLabel("o32", ABI.RegType.GPR, i);
        }

        public String DebugMemReadHex(long address, int size)
        {
            m_DataMemory.ReadData(address, size);
            return m_DataMemory.Data64.ToString("X16");
        }

        public CoreDebugger CoreDbg => m_CoreDebugger;

        protected void InstructionIgnore(DecodedInstruction inst) {
            //Log.Debug("Ignoring instruction: {0:X8} {1}", m_Pc, Disassembler.GetFullDisassembly(inst));
        }


        public abstract void BitwiseLogic(DecodedInstruction inst);
        public abstract void Add32(DecodedInstruction inst);
        public abstract void Add64(DecodedInstruction inst);
        public abstract void Subtract32(DecodedInstruction inst);
        public abstract void Subtract64(DecodedInstruction inst);
        public abstract void Shift32(DecodedInstruction inst);
        public abstract void Shift64(DecodedInstruction inst);
        public abstract void Divide32(DecodedInstruction inst);
        public abstract void Divide64(DecodedInstruction inst);
        public abstract void Multiply32(DecodedInstruction inst);
        public abstract void Multiply64(DecodedInstruction inst);
        public abstract void SetOnLessThan(DecodedInstruction inst);
        public abstract void TransferReg(DecodedInstruction inst);
        public abstract void Branch(DecodedInstruction inst);
        public abstract void Jump(DecodedInstruction inst);
        public abstract void Store(DecodedInstruction inst);
        public abstract void Load(DecodedInstruction inst);
        public abstract void Cache(DecodedInstruction inst);
        public abstract void Sync(DecodedInstruction inst);
        public abstract void FloatLoad(DecodedInstruction inst);
        public abstract void FloatStore(DecodedInstruction inst);
        public abstract void Add(DecodedInstruction inst);
        public abstract void Subtract(DecodedInstruction inst);
        public abstract void Multiply(DecodedInstruction inst);
        public abstract void Divide(DecodedInstruction inst);
        public abstract void SqrRoot(DecodedInstruction inst);
        public abstract void Abs(DecodedInstruction inst);
        public abstract void Mov(DecodedInstruction inst);
        public abstract void Neg(DecodedInstruction inst);
        public abstract void Round(DecodedInstruction inst);
        public abstract void Truncate(DecodedInstruction inst);
        public abstract void Ceil(DecodedInstruction inst);
        public abstract void Floor(DecodedInstruction inst);
        public abstract void Convert(DecodedInstruction inst);
        public abstract void Condition(DecodedInstruction inst);
        public abstract void ExceptionReturn(DecodedInstruction inst);
        public abstract void Trap(DecodedInstruction inst);
        public abstract void Break(DecodedInstruction inst);
    }
}
