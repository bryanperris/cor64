using cor64.Debugging;
using cor64.IO;
using cor64.Mips.Analysis;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
        private BaseDisassembler m_Disassembler;
        protected ulong m_Pc;
        private Stream m_IMemory;
        private String m_LastDisasm;
        private StringBuilder m_ExecutionLog = new StringBuilder();
        private ProgramTrace m_TraceLog;
        public event Action<DecodedInstruction> TraceStep;
        private ExecutionState m_State;
        private Stopwatch m_InstructionStopWatch = new Stopwatch();
        private ChipInterface m_Interface;
        private bool m_DebugMode;
        private bool m_BootCode;
        protected DecodedInstruction m_LastInst;
        private ulong m_ProgramStart;


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

        /// <summary>
        /// Call this to determine when the program's bootloader is finished
        /// </summary>
        /// <param name="address"></param>
        public void SetProgramEntryPoint(ulong address)
        {
            m_BootCode = true;
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

        public DecodedInstruction LastInst => m_LastInst;

        public abstract void Step();

        public void HookInterface(N64MemoryController controller)
        {
            m_Interface = new ChipInterface(controller);
            controller.HookCpu(m_Interface);
        }

        public virtual void AttachIStream(StreamEx memoryStream)
        {
            m_IMemory = memoryStream;
            memoryStream.AlignmentMode = true;
            m_Disassembler.SetStreamSource(memoryStream);
        }

        protected Stream IMemoryStream => m_IMemory;

        public abstract void AttachDStream(StreamEx memory);

        public abstract void AttachBootManager(BootManager bootManager);

        protected DecodedInstruction Decode()
        {
            DecodedInstruction decode = m_Disassembler.Disassemble(m_Pc);

            if (m_DebugMode && decode.Op.Family != OperationFamily.Null)
            {
                m_LastDisasm = m_Disassembler.GetFullDisassembly(decode);
                RecordAsmEntry(decode);
            }

            return decode;
        }

        private void RecordAsmEntry(DecodedInstruction dinst)
        {
            m_TraceLog.AppendInstruction(dinst);
            TraceStep?.Invoke(dinst);
        }

        protected void AddMemAccess(ulong address, String note)
        {
            m_TraceLog.AddInstructionMemAccess(address, note);
        }

        public String GetExecutionLog()
        {
            return m_TraceLog.GenerateTraceLog();
        }

        public virtual void DumpLogInfo(StringBuilder sb)
        {
            MemoryDebugger memoryDebugger = new MemoryDebugger();

            sb.AppendLine("Program Counter: " + m_Pc.ToString("X8"));
            sb.AppendLine("Register Hi: " + State.GetHi().ToString("X16"));
            sb.AppendLine("Register Lo: " + State.GetLo().ToString("X16"));
            sb.Append("Last Data Memory Access: ");
            sb.Append(LastDataAddress.ToString("X8"));
            sb.Append(" (");
            sb.Append(memoryDebugger.GetMemName((uint)LastDataAddress));
            sb.AppendLine(")");

            sb.AppendLine("--- GPR Table ---");

            for (int i = 0; i < 32; i++)
            {
                sb.AppendLine(String.Format("GPR {0}: {1:X16}", GetGPRName(i), m_State.GetGpr64(i)));
            }

            sb.AppendLine("----\n");

            sb.AppendLine(" --- CPU Disassembly Dump ---");

            var head = m_Pc - (10 * 4);

            if (head < 0)
            {
                head = 0;
            }

            for (ulong i = head; i <= m_Pc; i+=4)
            {
                sb.Append(i.ToString("X8"));
                sb.Append(" ");

                try
                {
                    var inst = m_Disassembler.Disassemble(i);
                    sb.AppendLine(m_Disassembler.GetFullDisassembly(inst));
                }
                catch
                {
                    sb.AppendLine("Inaccessable memory");
                }
            }

            sb.AppendLine("----\n");

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

        protected BaseDisassembler Disassembler => m_Disassembler;

        public ProgramTrace TraceLog => m_TraceLog;

        public virtual String CurrentDisassembly => m_LastDisasm;

        public ExecutionState State => m_State;

        public ChipInterface Interface => m_Interface;

        public bool DebugMode => m_DebugMode;

        public bool InBootMode => m_BootCode;
    }
}
