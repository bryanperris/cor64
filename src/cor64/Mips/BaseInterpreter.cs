using cor64.Debugging;
using cor64.IO;
using cor64.Mips.Analysis;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

/* TODO: List
 * Debugging
 * -----------
 * Debugger Step, Step In/Out, etc should use breakpoints for these functions
 * Breakpoints need to store the old instruction, insert the BP and then trigger an interrupt
 * Every Step (or cached/compiled instruction) has to check interrupts
 * Solve on how timing counts will work
 * Interrupts: Scan RDRAM and fill in vector table (HLE interrupt handlers will need special mem hooks)
 * Interrupts: Figure out how to emulate the IPL based vectors
 * Cache: Need a way to cache data memory in a cache line (if we want that)
 * Cache: Cache decoded instructions in the base, and other impementations can just provide their own and skip ours
 * Memory: When requesting a read/write to the MMU, we need to process the virtual address and then figure out what stream to access as RAM
 * Memory: We may need to wrap things so there is a layer of emulated DMA business
 * 
 */

/* 
 * Some names notes:
 * CoreEvents - Management of interrupts, timing etc
 * MemManager - Management of virtual TLB, cache table, memory access, virtual memory stuff
 * CoreCache - Management of decoded instructions, and self modification aware
 * BaseDebugger - A way to maanger debugger state: set breakpoints, execution log, etc
 */

// TODO: Disposal pattern for streams

namespace cor64.Mips
{
    public abstract class BaseInterpreter
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private BaseDisassembler m_Disassembler;
        protected ulong m_Pc;
        private Stream m_IMemory;
        private StringBuilder m_ExecutionLog = new StringBuilder();
        private ProgramTrace m_TraceLog;
        public event Action<DecodedInstruction> TraceStep;
        private ExecutionState m_State;
        private Stopwatch m_InstructionStopWatch = new Stopwatch();
        private MipsInterface m_Interface;
        private bool m_DebugMode;
        private ulong m_ProgramStart;
        private ProgramTrace.TraceMode m_TraceMode;

        protected BaseInterpreter(BaseDisassembler disassembler)
        {
            m_State = new ExecutionState();
            m_Disassembler = disassembler;
            m_TraceLog = new ProgramTrace(disassembler);
        }

        public virtual void SafeSetPC(ulong address)
        {
            m_Pc = address;
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
            m_State = state;
        }

        public void SetDebuggingMode(bool mode)
        {
            m_DebugMode = mode;
        }

        public void SetTraceMode(ProgramTrace.TraceMode mode)
        {
            m_TraceMode = mode;

            if (mode != ProgramTrace.TraceMode.None)
            {
                Log.Debug("Execution trace logging has been enabled");
            }
        }

        public DecodedInstruction CurrentInst { get; protected set; }

        public abstract void Step();

        public virtual void AttachIStream(StreamEx memoryStream)
        {
            m_IMemory = memoryStream;
            memoryStream.AlignmentMode = true;
            m_Disassembler.SetStreamSource(memoryStream);
        }

        protected Stream IMemoryStream => m_IMemory;

        public abstract void AttachDStream(StreamEx memory);

        public abstract void AttachBootManager(BootManager bootManager);

        /* The method is reponsible for setting up the reported rdram size */
        protected abstract void ReportRdramSize();

        protected virtual void EntryPointHit()
        {
            InBootMode = false;
            Log.Debug("**** Program Entry Point Hit ****");
            ReportRdramSize();
        }

        protected DecodedInstruction Decode()
        {
            DecodedInstruction decode = m_Disassembler.Disassemble(m_Pc);

            if (InBootMode)
            {
                if (m_ProgramStart != 0 && m_Pc == m_ProgramStart)
                {
                    EntryPointHit();
                }
            }

            return decode;
        }

        public bool IsTraceActive => m_TraceMode == ProgramTrace.TraceMode.Full || (m_TraceMode == ProgramTrace.TraceMode.ProgramOnly && !InBootMode);

        public bool IsMemTraceActive => IsTraceActive && ((m_TraceLog.Details & ProgramTrace.TraceDetails.MemoryAccess) == ProgramTrace.TraceDetails.MemoryAccess);

        protected void TraceInstruction(DecodedInstruction decode, bool nullifed)
        {
            if (m_TraceMode != ProgramTrace.TraceMode.None && decode.Op.Family != OperationFamily.Null && IsTraceActive)
            {
                m_TraceLog.AppendInstruction(decode, nullifed);
                TraceStep?.Invoke(decode);
            }
        }

        protected void AddMemAccess(ulong address, bool isWrite, String val)
        {
            if (IsMemTraceActive)
                m_TraceLog.AddInstructionMemAccess(address, isWrite, val);
        }

        public virtual String GetGPRName(int i) {
            return i.ToString();
        }

        protected ulong LastDataAddress { get; set; }

        public virtual String Description
        {
            get {
                return this.ToString();
            }
        }

        public BaseDisassembler Disassembler => m_Disassembler;

        public ProgramTrace TraceLog => m_TraceLog;

        public ExecutionState State => m_State;

        public MipsInterface Interface => m_Interface;

        public bool DebugMode => m_DebugMode;

        public bool InBootMode { get; protected set; } = true;

        public ProgramTrace.TraceMode TraceMode => m_TraceMode;

        public virtual IDictionary<string, string> SnapSave()
        {
            StringBuilder sb = new StringBuilder();

            Dictionary<string, string> snap = new Dictionary<string, string>
            {
                { "pc", m_Pc.ToString("X16") },
                { "lo", m_State.GetLo().ToString("X16") },
                { "hi", m_State.GetHi().ToString("X16") },
                { "llbit", m_State.LLBit ? "1" : "0" }
            };

            for (int i = 0; i < 32; i++)
            {
                sb.Clear();
                sb.Append(m_State.GetGpr64(i).ToString("X16"));
                sb.Append(";");
                sb.Append(((long)m_State.GetGpr64(i)).ToString());
                sb.Append(";");
                sb.Append(((int)m_State.GetGpr32(i)).ToString());

                snap.Add("gpr" + i.ToString(), sb.ToString());
            }

            for (int i = 0; i < 32; i++)
            {
                sb.Clear();
                sb.Append(m_State.GetFprDW(i).ToString("X16"));
                sb.Append(";");
                sb.Append(DoubleConverter.ToExactString(m_State.GetFprD(i)));
                

                snap.Add("fpr" + i.ToString(), sb.ToString());
            }

            return snap;
        }
    }
}
