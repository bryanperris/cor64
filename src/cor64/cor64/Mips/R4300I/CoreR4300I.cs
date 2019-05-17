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
using NLog;

using static cor64.Mips.R4300I.Opcodes;

public enum DebugInstMode
{
    None,
    Full,
    ProgramOnly
}


namespace cor64.Mips.R4300I
{
    public abstract class CoreR4300I : BaseInterpreter
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private Action<DecodedInstruction>[] m_CallTable;
        private SystemController m_Coprocessor0;
        private Clock m_CoreClock;
        protected DataMemory m_DataMemory;
        private Profiler m_Profiler = new Profiler();
        public bool StartedWithProfiler { get; protected set; }

        /* TODO: When we do have a cache system, 
         * some conditions of the cache can break
         * atomic operations (LLBit is set back to 0)
         */

        protected bool EnableCp0 { get; set; } = true;
        protected bool EnableCp1 { get; set; } = true;

        /* Debugging */
        private HashSet<int> m_GPRWriteBreakpoints = new HashSet<int>();
        private HashSet<int> m_GPRReadBreakpoints = new HashSet<int>();
        protected DebugInstMode core_InstDebugMode;
        private ulong m_EntryPoint;

        protected CoreR4300I(BaseDisassembler disassembler) : base(disassembler)
        {
            m_CallTable = new Action<DecodedInstruction>[OpcodeFactory.LastID + 1];

            m_CoreClock = new Clock(10, 2);
            m_Coprocessor0 = new SystemController(State);
            m_Coprocessor0.Attach_State(Interface, m_CoreClock);
            m_Coprocessor0.Attach_Callbacks(() => m_Pc, () => BranchDelay);
            m_Coprocessor0.ExceptionJump += ExceptionJump;

            Map(Add32, ADD, ADDU, ADDI, ADDIU);
            Map(BitwiseLogic, AND, ANDI, OR, ORI, NOR, XOR, XORI);
            Map(Add64, DADD, DADDI, DADDIU, DADDU);
            Map(Divide32, DIV, DIVU);
            Map(Divide64, DDIV, DDIVU);
            Map(Multiply32, MULT, MULTU);
            Map(Multiply64, DMULT, DMULTU);
            Map(Shift32, SLLV, SRLV, SRAV, SLL, SRL, SRA);
            Map(Shift64, DSLLV, DSRLV, DSRAV, DSLL, DSRL, DSRA, DSLL32, DSRL32, DSRA32);
            Map(Subtract32, SUB, SUBU);
            Map(Subtract64, DSUB, DSUBU);
            Map(SetOnLessThan, SLT, SLTI, SLTIU, SLTU);
            Map(TransferReg, MFHI, MFLO, MTHI, MTLO, MTC0, MTC1, MFC0, MFC1, DMTC0, DMTC1, DMFC0, DMFC1, CTC1, CFC1);
            Map(Branch, BC1F, BC1FL, BC1T, BC1TL, BEQ, BEQL, BGEZ, BGEZAL, BGEZALL, BGEZL, BGTZ, BGTZL, BLEZ, BLEZL, BLTZ, BLTZAL, BLTZALL, BLTZL, BNE, BNEL);
            Map(Jump, J, JAL, JR, JALR);
            Map(Store, SB, SH, SD, SW, SWL, SWR, SC, SCD);
            Map(Load, LUI, LB, LBU, LH, LHU, LW, LWU, LWL, LWR, LD, LDL, LDR, LL, LLD);
            Map(Cache, CACHE);
            Map(Sync, SYNC);

            /* FPU Hooks */
            Map(FloatLoad, LDC1, LWC1);
            Map(FloatStore, SWC1, SDC1);
            Map(Add, ADD_FPU);
            Map(Subtract, SUB_FPU);
            Map(Multiply, MUL_FPU);
            Map(Divide, DIV_FPU);
            Map(SqrRoot, SQRT);
            Map(Abs, ABS);
            Map(Mov, MOV);
            Map(Neg, NEG);
            Map(Round, ROUND_L, ROUND_W);
            Map(Truncate, TRUNC_L, TRUNC_W);
            Map(Ceil, CEIL_L, CEIL_W);
            Map(Floor, FLOOR_L, FLOOR_W);
            Map(Convert, CVT_D, CVT_L, CVT_S, CVT_W);
            Map(Condition, C_F, C_UN, C_EQ, C_UEQ, C_OLT, C_ULT, C_OLE, C_ULE, C_SF, C_NGLE, C_SEQ, C_NGL, C_LT, C_NGE, C_LE, C_NGT);
        }

        /********************************************************
         * Processor Core Logic
         ********************************************************/

        #region Core Logic

        private void ExceptionJump(ulong address)
        {
            Log.Debug("Exception jump taken: {0:X16}", address);

            /* Leave the whole branch handler */
            TakeBranch = false;
            BranchDelay = false;
            TargetAddress = 0;
            UnconditionalJump = false;
        }

        protected ulong ReadCp0Value(int select, bool isDwordInst)
        {
            var value = State.Cp0.Read(select);

            /* If running 64-bit mode, with the 32-bit version, then sign extend */
            if (IsOperation64 && !isDwordInst)
            {
                value = (ulong)(int)(uint)value;
            }

            return value;
        }

        private void Map(Action<DecodedInstruction> instAction, params Opcode[] opcodes)
        {
            foreach (var op in opcodes)
            {
                m_CallTable[op.ID] = instAction;
            }
        }

        protected bool ValidateInstruction(DecodedInstruction decoded)
        {
            return !decoded.EmulatorNop && !decoded.IsNull;
        }

        protected Action<DecodedInstruction> GetInstructionMethod(DecodedInstruction decoded)
        {
            return m_CallTable[decoded.Op.ID];
        }

        protected abstract void Add32(DecodedInstruction inst);

        protected abstract void BitwiseLogic(DecodedInstruction inst);

        protected abstract void Add64(DecodedInstruction inst);

        protected abstract void Divide32(DecodedInstruction inst);

        protected abstract void Divide64(DecodedInstruction inst);

        protected abstract void Multiply32(DecodedInstruction inst);

        protected abstract void Multiply64(DecodedInstruction inst);

        protected abstract void Shift32(DecodedInstruction inst);

        protected abstract void Shift64(DecodedInstruction inst);

        protected abstract void Subtract32(DecodedInstruction inst);

        protected abstract void Subtract64(DecodedInstruction inst);

        protected abstract void SetOnLessThan(DecodedInstruction inst);

        protected abstract void TransferReg(DecodedInstruction inst);

        protected abstract void Branch(DecodedInstruction inst);

        protected abstract void Jump(DecodedInstruction inst);

        protected abstract void Store(DecodedInstruction inst);

        protected abstract void Load(DecodedInstruction inst);

        protected abstract void Cache(DecodedInstruction inst);

        protected abstract void Sync(DecodedInstruction inst);

        #endregion

        /********************************************************
         * Core FPU Logic
         ********************************************************/

        #region FPU Opcodes

        protected abstract void FloatLoad(DecodedInstruction inst);

        protected abstract void FloatStore(DecodedInstruction inst);

        protected abstract void Add(DecodedInstruction inst);

        protected abstract void Subtract(DecodedInstruction inst);

        protected abstract void Multiply(DecodedInstruction inst);

        protected abstract void Divide(DecodedInstruction inst);

        protected abstract void SqrRoot(DecodedInstruction inst);

        protected abstract void Abs(DecodedInstruction inst);

        protected abstract void Mov(DecodedInstruction inst);

        protected abstract void Neg(DecodedInstruction inst);

        protected abstract void Round(DecodedInstruction inst);

        protected abstract void Truncate(DecodedInstruction inst);

        protected abstract void Ceil(DecodedInstruction inst);

        protected abstract void Floor(DecodedInstruction inst);

        protected abstract void Convert(DecodedInstruction inst);

        protected abstract void Condition(DecodedInstruction inst);

        #endregion

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

        public bool TakeBranch { get; set; }

        public bool BranchDelay { get; set; }

        public ulong TargetAddress { get; set; }

        protected bool UnconditionalJump { get; set; }

        public bool WillJump => TakeBranch || UnconditionalJump;

        public bool BranchDelaySlot => BranchDelay;

        public ulong BranchTarget => TargetAddress;

        public void ClearBranchUnit()
        {
            TargetAddress = 0;
            BranchDelay = false;
            UnconditionalJump = false;
            TakeBranch = false;
        }

        protected bool ComputeBranchCondition(ulong source, ulong target, RegBoundType copSelect,  ArithmeticOp compareOp)
        {
            bool condition = false;

            bool EQ_ZERO()
            {
                return source == 0;
            }

            bool GT_ZERO()
            {
                if (IsOperation64)
                {
                    ulong v = source;
                    return (v >> 63) == 0 && v > 0;
                }
                else
                {
                    uint v = (uint)source;
                    return (v >> 31) == 0 && v > 0;
                }
            }

            bool LT_ZERO()
            {
                if (IsOperation64)
                {
                    ulong v = source;
                    return (v >> 63) == 1 && v > 0;
                }
                else
                {
                    uint v = (uint)source;
                    return (v >> 31) == 1 && v > 0;
                }
            }

            bool COP_FLAG(bool compare)
            {
                switch (copSelect)
                {
                    case RegBoundType.Cp1:
                        {
                            var cond = State.FCR.Condition;

                            if (!compare)
                            {
                                /* Expecting FALSE condition */
                                return !cond && true;
                            }
                            else
                            {
                                /* Expecting TRUE condition */
                                return cond && true;
                            }
                        }

                    default: throw new NotSupportedException("MIPS does not support this kind of unit: " + copSelect.ToString());
                }
            }

            switch (compareOp)
            {
                default: throw new NotSupportedException("MIPS does not support this branch operation");
                case ArithmeticOp.EQUAL: condition = (source == target); break;
                case ArithmeticOp.NOT_EQUAL: condition = (source != target); break;
                case ArithmeticOp.GREATER_THAN: condition = GT_ZERO(); break;
                case ArithmeticOp.LESS_THAN: condition = LT_ZERO(); break;
                case ArithmeticOp.GREATER_THAN_OR_EQUAL: condition = (EQ_ZERO() || GT_ZERO()); break;
                case ArithmeticOp.LESS_THAN_OR_EQUAL: condition = (EQ_ZERO() || LT_ZERO()); break;
                case ArithmeticOp.FALSE: condition = COP_FLAG(false); break;
                case ArithmeticOp.TRUE:  condition = COP_FLAG(true); break;
            }

            return condition;
        }

        /********************************************************
         * Processor State Properties
         ********************************************************/

        public SystemController Cop0 => m_Coprocessor0;

        public ExceptionType Exceptions => State.Cp0.Cause.Exception;

        public bool IsAddress64 => State.Cp0.Status.IsAddress64;

        public bool IsOperation64 => State.Cp0.Status.IsOperation64;

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

        public ulong ReadPC()
        {
            return m_Pc;
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
                m_Pc = pc;
            };

            bootManager.MemWrite += (addr, val) =>
            {
                var debugMode = DebugMode;
                SetDebuggingMode(false);

                m_DataMemory.Data32 = val;
                m_DataMemory.WriteData(addr, 4, true);

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

        public override void AttachIStream(StreamEx memoryStream)
        {
            base.AttachIStream(new SimpleVMemStream(this, memoryStream, false));
        }

        public void OverrideIStream(StreamEx memoryStream)
        {
            base.AttachIStream(memoryStream);
        }

        public override void AttachDStream(StreamEx memoryStream)
        {
            memoryStream.AlignmentMode = true;
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
            private CoreR4300I m_Core;
            private bool m_UseCache;
            private bool m_IsTranslated;
            private bool m_IsDataPath;
            private MemoryDebugger m_MemDebugger = new MemoryDebugger();
            private int m_LastNamedRegionHash = 0;
            private uint m_Size = 0xFFFFFFFF;

            public SimpleVMemStream(CoreR4300I core, StreamEx streamEx, bool isDataPath) : base(streamEx)
            {
                m_Core = core;
                m_IsDataPath = isDataPath;

                if (m_Core.BypassMMU)
                {
                    m_Size = (uint)streamEx.Length;
                }
            }

            public override long Length => m_Size;

            protected override void ReportAccess(bool isRead, long address, int len)
            {
                m_Core.LastDataAddress = (ulong)address;

                if (m_Core.DebugMode && m_IsDataPath)
                {
                    uint addr = (uint)address;
                    var n = m_MemDebugger.GetMemName(addr);
                    var h = n.GetHashCode();

                    if (h != m_LastNamedRegionHash)
                    {
                        m_LastNamedRegionHash = h;
                        m_Core.AddMemAccess(addr, n);

                        String op = isRead ? "Data read from " : "Data write to ";
                        String boot = m_Core.InBootMode ? "BOOTLOADER " : "";

                        Log.Debug("{0} {1:X8} - {2} PMEM {3}", boot, m_Core.m_Pc, op, n);
                    }

                    // TODO: We should compress/trim memory access info when RAM operations take place such as RDRAM, PIF RAM, SP RAM, because they can add a lot of noise
                }
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

        public void AddVBP_ReadGPR(int gpr)
        {
            if (!m_GPRReadBreakpoints.Contains(gpr))
            {
                m_GPRReadBreakpoints.Add(gpr);
            }
        }

        public override string GetGPRName(int i)
        {
            return ABI.GetLabel("o32", ABI.RegType.GPR, i);
        }

        protected void BeginInstructionProfile(DecodedInstruction inst)
        {
            m_Profiler.StartSession(inst.Op);
        }

        protected void EndInstructionProfile()
        {
            m_Profiler.StopSession();
        }

        public String GetProfiledResults()
        {
            return m_Profiler.GenerateReport();
        }

        public void SetInstructionDebugMode(DebugInstMode mode)
        {
            core_InstDebugMode = mode;
        }

        public ulong DebugEntryPoint { get; set; }
    }
}
