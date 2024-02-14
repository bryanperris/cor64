using System.Net.NetworkInformation;
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
using cor64.Mips.R4300I.TLB;
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
        private readonly ControlRegisters m_Cop0Regs;
        private readonly Clock m_CoreClock;
        // protected DataMemory m_DataMemory;
        public N64MemoryController PhysicalMemory { get; private set; }
        public bool StartedWithProfiler { get; protected set; }
        // private readonly BranchUnit m_BranchUnit = new BranchUnit();
        private readonly ExecutionStateR4300I m_State;
        private readonly MipsDebugger m_Debugger = new MipsDebugger();
        private readonly MemoryManager m_MMU;


        const uint RDRAM_SIZE_4MB = 0x400000;
        const uint RDRAM_SIZE_8MB = 0x800000;


        /* TODO: When we do have a cache system, 
         * some conditions of the cache can break
         * atomic operations (LLBit is set back to 0)
         */

        protected InterpreterBaseR4300I(BaseDisassembler disassembler) : base(disassembler)
        {
            m_CoreClock = new Clock(10, 2);

            m_Cop0Regs = new ControlRegisters();
            m_State = new ExecutionStateR4300I(m_Cop0Regs);
            m_Coprocessor0 = new SystemController(m_Cop0Regs, State);
            m_MMU = new MemoryManager(m_Coprocessor0);

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
            .Map(TLBWrite, TLBWI, TLBWR)
            .Map(TLBProbe, TLBP)
            .Map(TLBRead, TLBR)
            .Map(Syscall, SYSCALL)

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

            .Finish();

            if (CoreConfig.Current.WorkbenchMode) {
                Debugger.ActivateDebugger();
            }
        }

        public void AttachRcp(RcpCore rcp)
        {
            m_Coprocessor0.AttachInterface(rcp.RcpInterface);
        }

        public virtual void Init() {
            m_MMU.Init();
        }

        protected override void EntryPointHit() {
            base.EntryPointHit();

            /* Report RDRAM Size */
            var traceMode = TraceMode;
            SetTraceMode(Analysis.ProgramTrace.TraceMode.None);
            PhysicalMemory.U32(0x00000318, N64Endianess.U32(RDRAM_SIZE_8MB));
            SetTraceMode(traceMode);

            // TODO: osMemSize is located at 0x3F0 for CIC-6105
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

        public bool BU_ConditionalPending { get; set; }

        public bool BU_ExecuteBranchDelay { get; set; }

        public bool BU_DelaySlotNext { get; set; }

        public long BU_TargetAddress { get; set; }

        public bool BU_UnconditionalPending { get; set; }

        public bool BU_NullifyNext { get; set; }

        public void ClearBranchState()
        {
            BU_ConditionalPending = false;
            BU_UnconditionalPending = false;

            BU_DelaySlotNext = false;
            BU_ExecuteBranchDelay = false;
            BU_NullifyNext = false;
        }

        /********************************************************
         * Processor State Properties
         ********************************************************/

        public SystemController Cop0 => m_Coprocessor0;

        public ControlRegisters Cop0State => m_Cop0Regs;

        public ExecutionStateR4300I StateR4000I => m_State;

        public ExceptionType Exceptions => m_Cop0Regs.Cause.Exception;

        public bool IsAddress64 => m_Cop0Regs.Status.IsAddress64;

        public bool IsOperation64 => m_Cop0Regs.Status.IsOperation64;

        public Clock CoreClock => m_CoreClock;

        // public DataMemory DataMem => m_DataMemory;

        public sealed override ExecutionState State => m_State;


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
            return ReadGPR64(31);
        }

        public void SetOperation64Mode(bool mode)
        {
            m_Cop0Regs.Status.DebugSet_Operation64(mode);
        }

        public void SetExceptionState(ExceptionType type)
        {
            Cop0.RaiseMipsException(type);
        }

        public void SetSyscallException() {
            Cop0.SyscallException();
        }

        public void SetBreakException() {
            Cop0.BreakException();
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

            #if !OPTIMAL
            Debugger.CheckGprWriteBreakpoints(reg);
            #endif
        }

        protected void Writeback64(int reg, ulong value)
        {
            State.SetGpr64(reg, value);

            #if !OPTIMAL
            Debugger.CheckGprWriteBreakpoints(reg);
            #endif
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

        /* --------- FPR Accessors */

        protected float ReadFPR_S(int select) => StateR4000I.FPR.ReadFloatSingle(select);

        protected void WriteFPR_S(int select, float value) => StateR4000I.FPR.WriteFloatSingle(select, RoundHelper.Round(value, State.FCR.RoundMode));

        protected void WriteFPR_SNR(int select, float value) => StateR4000I.FPR.WriteFloatSingle(select, value); // No rounding

        protected double ReadFPR_D(int select) => StateR4000I.FPR.ReadFloatDouble(select);

        protected void WriteFPR_D(int select, double value) => StateR4000I.FPR.WriteFloatDouble(select, RoundHelper.Round(value, State.FCR.RoundMode));

        protected void WriteFPR_DNR(int select, double value) => StateR4000I.FPR.WriteFloatDouble(select, value); // No rounding

        protected uint ReadFPR_W(int select) => StateR4000I.FPR.ReadScalarWord(select);

        protected void WriteFPR_W(int select, uint value) => StateR4000I.FPR.WriteScalarWord(select, value);

        protected ulong ReadFPR_DW(int select) => StateR4000I.FPR.ReadScalarDword(select);

        protected void WriteFPR_DW(int select, ulong value) => StateR4000I.FPR.WriteScalarDword(select, value);

        /* --------- */

        protected ulong ReadHi() => State.GetHi();

        protected ulong ReadLo() => State.GetLo();

        protected void WriteHi(ulong value) => State.SetHi(value);

        protected void WriteLo(ulong value) => State.SetLo(value);

        public override void AttachBootManager(BootManager bootManager)
        {
            bootManager.PCWrite += (pc) => PC = pc;
            bootManager.MemWrite += (addr, val) => PhysicalMemory.U32(addr, val);
            bootManager.RegWrite += State.SetGpr64;
            bootManager.CP0Write += m_Cop0Regs.Write;
        }

        /********************************************************
         * Memory Logic
         ********************************************************/

        #region Memory Logic

        /* For testing-purposes */
        public bool BypassMMU => m_MMU.IsDisabled;

        public void SetMMUBypass(bool bypass) {
            m_MMU.Disable(bypass);
        }

        protected bool TryVirtToPhys(bool isWrite, long virtualAddress, out long physicalAddress) {
            return m_MMU.VirtualToPhysical(isWrite, virtualAddress, out physicalAddress);
        }

        protected void TLBTick() {
            m_MMU.TLB.Tick();
        }

        protected override uint FetchInstruction(long address)
        {
            return PhysicalMemory.U32(address);
        }

        public void AttachMemory(N64MemoryController controller) {
            PhysicalMemory = controller;
        }

        protected TLBCache TLB => m_MMU.TLB;

        // CLEANUP: We need wrapped memory IO calls to still allow data logging

        // private sealed class SimpleVMemStream : VMemStream
        // {
        //     private readonly InterpreterBaseR4300I m_Core;
        //     private bool m_UseCache;
        //     private bool m_IsDataPath;
        //     private readonly uint m_Size = 0xFFFFFFFF;
        //     private readonly Stream m_BaseStream;

        //     public SimpleVMemStream(InterpreterBaseR4300I core, Stream stream, bool isDataPath) : base(stream)
        //     {
        //         m_BaseStream = stream;
        //         m_Core = core;
        //         m_IsDataPath = isDataPath;

        //         if (m_Core.BypassMMU)
        //         {
        //             m_Size = (uint)stream.Length;
        //         }
        //     }

        //     public override long Length => m_Size;

        //     public override int Read(byte[] buffer, int offset, int count)
        //     {
        //         var read = base.Read(buffer, offset, count);

        //         if ((m_Core.IsMemTraceActive || m_Core.InstDebugMode != InstructionDebugMode.None) && m_IsDataPath)
        //         {
        //             m_Core.TraceMemoryHit((uint)(ulong)Position, false, DebugValue(buffer, offset, count));
        //         }

        //         return read;
        //     }

        //     public override void Write(byte[] buffer, int offset, int count)
        //     {
        //         base.Write(buffer, offset, count);

        //         if ((m_Core.IsMemTraceActive || m_Core.InstDebugMode != InstructionDebugMode.None) && m_IsDataPath)
        //         {
        //             m_Core.TraceMemoryHit((uint)(ulong)Position, true, DebugValue(buffer, offset, count));
        //         }
        //     }

        //     private String DebugValue(byte[] buffer, int offset, int size)
        //     {
        //         StringBuilder sb = new StringBuilder();

        //         for (int i = 0; i < size; i++)
        //         {
        //             sb.Append(buffer[offset + i].ToString("X2"));
        //         }

        //         return sb.ToString();
        //     }
        // }

        #endregion

        /********************************************************
         * Debugger Interface
         ********************************************************/

        public override MipsDebugger Debugger => m_Debugger;

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
        public abstract void TLBProbe(DecodedInstruction inst);
        public abstract void TLBWrite(DecodedInstruction inst);
        public abstract void TLBRead(DecodedInstruction inst);
        public abstract void Syscall(DecodedInstruction inst);
    }
}
