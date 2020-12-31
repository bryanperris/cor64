using cor64.Debugging;
using cor64.IO;
using cor64.Mips.Analysis;
using cor64.Mips.R4300I;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using static cor64.Cartridge;

// TODO: Disposal pattern for streams

namespace cor64.Mips
{
    public abstract class BaseInterpreter
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private ulong m_Pc;
        private Stream m_IMemory;
        private ulong m_ProgramStart;
        protected Profiler m_Profiler = new Profiler();

        private ExecutionState m_ProtectedState;

        public event Action<DecodedInstruction> TraceStep;

        protected BaseInterpreter(BaseDisassembler disassembler)
        {
            State = new ExecutionState();
            Disassembler = disassembler;
            
            TraceLog = new ProgramTrace(disassembler)
            {
                FilterInterruptHandlers = true
            };
        }

        protected void EnterProtectedState() {
            m_ProtectedState = State;
            State = new ExecutionState();
        }

        protected void LeaveProtectedState() {
            State = m_ProtectedState;
        }

        public virtual void SafeSetPC(ulong address)
        {
            m_Pc = address;
        }

        protected ulong PC {
            get => m_Pc;

            set {
                m_Pc = value;
            }
        }

        public ulong ReadPC()
        {
            return m_Pc;
        }

        /// <summary>
        /// Call this to determine when the program's bootloader is finished
        /// </summary>
        /// <param name="address"></param>
        public void SetProgramEntryPoint(ulong address)
        {
            InBootMode = true;
            m_ProgramStart = address;
        }

        public void OverrideCoreState(ExecutionState state)
        {
            State = state;
        }

        public void SetDebuggingMode(bool mode)
        {
            DebugMode = mode;
        }

        public void SetTraceMode(ProgramTrace.TraceMode mode)
        {
            TraceMode = mode;

            if (mode != ProgramTrace.TraceMode.None)
            {
                Log.Debug("Execution trace logging has been enabled");
            }
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

        public DecodedInstruction CurrentInst { get; protected set; }

        public abstract void Step();

        public virtual void AttachIStream(Stream memoryStream)
        {
            m_IMemory = memoryStream;
            Disassembler.SetStreamSource(memoryStream);
        }

        public abstract void AttachDStream(Stream memory);

        public abstract void AttachBootManager(BootManager bootManager);

        protected virtual void EntryPointHit()
        {
            InBootMode = false;
            Log.Debug("**** Program Entry Point Hit ****");
        }

        protected DecodedInstruction Decode()
        {
            DecodedInstruction decode = Disassembler.Disassemble(m_Pc);

            if (InBootMode)
            {
                if (m_ProgramStart != 0 && m_Pc == m_ProgramStart)
                {
                    EntryPointHit();
                }
            }

            return decode;
        }

        protected void TraceInstruction(DecodedInstruction decode, bool nullifed, bool inInterrupt)
        {
            if (TraceMode != ProgramTrace.TraceMode.None && decode.Op.Family != OperationFamily.Null && IsTraceActive)
            {
                TraceLog.AppendInstruction(decode, nullifed, inInterrupt);
                TraceStep?.Invoke(decode);
            }
        }

        protected void TraceInstruction(DecodedInstruction decode, bool nullifed)
        {
            TraceInstruction(decode, nullifed, false);
        }

        protected void TraceMemoryHit(ulong address, bool isWrite, String val)
        {
            if (IsMemTraceActive)
                TraceLog.AddInstructionMemAccess(address, isWrite, val);

            if (core_InstDebugMode != InstructionDebugMode.None) {
                core_MemAccessNote = new MemoryAccessMeta(address, isWrite, val);
            }
        }

        public virtual String GetGPRName(int i) {
            return i.ToString();
        }

        protected bool ValidateInstruction(DecodedInstruction decoded)
        {
            return !decoded.IsInvalid && !decoded.IsNull;
        }

        protected bool ComputeBranchCondition(bool isDword, ulong source, ulong target, RegBoundType copSelect,  ArithmeticOp compareOp)
        {
            bool condition = false;

            bool EQ_ZERO()
            {
                return source == 0;
            }

            bool GT_ZERO()
            {
                if (isDword)
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
                if (isDword)
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

        public static void RegTransferGprHelper(DecodedInstruction inst, out int gprSource, out int gprTarget)
        {
            gprSource = inst.Target;
            gprTarget = inst.Destination;

            if (inst.Op.XferSource == RegBoundType.Gpr)
            {
                switch (inst.Op.OperandFmt)
                {
                    default: break;
                    case OperandType.R_S: gprSource = inst.Source; break;
                }
            }

            if (inst.Op.XferTarget == RegBoundType.Gpr)
            {
                // RSP: CP0 -> GPR
                if (inst.Op.OperandFmt == OperandType.RspCp0_CT) {
                    gprSource = inst.Destination;
                    gprTarget = inst.Target;
                    return;
                }

                // RSP: GPR -> CP0
                if (inst.Op.OperandFmt == OperandType.RspCp0_TC) {
                    gprSource = inst.Target;
                    gprTarget = inst.Destination;
                    return;
                }

                switch (inst.Op.OperandFmt)
                {
                    default: break;
                    case OperandType.R_D: gprTarget = inst.Destination; break;
                    case OperandType.Cop0_CT:
                    case OperandType.Cop0_TC:
                    case OperandType.Cop1_CT:
                    case OperandType.Cop1_TC:
                    //case OperandType.VU_CT:
                    case OperandType.VU_FromCtrl:
                    //case OperandType.VU_TC:
                    case OperandType.VU_ToCtrl:
                    case OperandType.Cop1_FromCtrl:
                    case OperandType.Cop1_ToCtrl:
                        {
                            gprSource = inst.Target;
                            gprTarget = inst.Target;
                            break;
                        }
                }
            }
        }

        public virtual String Description
        {
            get {
                return this.ToString();
            }
        }

        public BaseDisassembler Disassembler { get; }

        public ProgramTrace TraceLog { get; }

        public ExecutionState State { get; private set; }

        public bool DebugMode { get; private set; }

        protected InstructionDebugMode core_InstDebugMode;

        protected MemoryAccessMeta core_MemAccessNote;
        
        public void SetInstructionDebugMode(InstructionDebugMode mode)
        {
            core_InstDebugMode = mode;
        }

        public bool InBootMode { get; protected set; } = true;

        public ProgramTrace.TraceMode TraceMode { get; private set; }

        public InstructionDebugMode InstDebugMode => core_InstDebugMode;

        public bool IsTraceActive => TraceMode == ProgramTrace.TraceMode.Full || (TraceMode == ProgramTrace.TraceMode.ProgramOnly && !InBootMode);

        public bool IsMemTraceActive => IsTraceActive && ((TraceLog.Details & ProgramTrace.TraceDetails.MemoryAccess) == ProgramTrace.TraceDetails.MemoryAccess);

        protected bool EnableCp0 { get; set; } = true;

        protected bool EnableCp1 { get; set; } = true;

        protected bool EnableCp2 { get; set; } = true;

        protected Stream IMemoryStream => m_IMemory;
    }
}
