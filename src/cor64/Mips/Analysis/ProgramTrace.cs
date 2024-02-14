using System.Diagnostics;
using System.IO;
using System.Net.NetworkInformation;
using cor64.Debugging;
using cor64.Mips.R4300I;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace cor64.Mips.Analysis
{
    public class ProgramTrace
    {
        private readonly static Logger Log = LogManager.GetCurrentClassLogger();

        // We keep track of blocks in sequence, every new block
        private readonly List<InfoBasicBlock> m_Blocks = new List<InfoBasicBlock>();
        private readonly BaseDisassembler m_Disassembler;
        private InfoBasicBlock m_CurrentBlock;
        private bool m_Interrupted;
        private long? m_EntryPoint;
        private InfoBasicBlock m_BlockWithDelaySlot;

        public bool EnableFullTracing { get; set; } = false;

        public bool DisableAdvancedTraceReduction { get; set; }

        public event Action<long, DecodedInstruction> OnInstTrace;

        public enum TraceMode
        {
            None,
            Full,
            ProgramOnly
        }

        [Flags]
        public enum TraceDetails
        {
            None = 0,
            MemoryAccess = 0b1
        }

        public ProgramTrace(BaseDisassembler disassembler)
        {
            m_Disassembler = disassembler;
        }

        public void Interrupt() {
            if (m_CurrentBlock != null) {
                // Finish the block, prepare for new block
                m_CurrentBlock.Finish(BlockEnding.Interrupted);
                BlockFinalize();
            }

            m_Interrupted = true;
        }

        public void AppendInstruction(long address, DecodedInstruction inst)
        {
            OnInstTrace?.Invoke(address, inst);

            if (m_CurrentBlock != null && !m_CurrentBlock.FullyFinished && m_CurrentBlock.Ending == BlockEnding.NormalJump) {
                // There are 2 types of normal jump endings
                // 1. active delay slot
                // 2. nullified delays slot

                if (address == m_CurrentBlock.GetLastInst().Address + 4) {
                    // Executing delay slot
                    m_CurrentBlock.AppendDelaySlot(new InfoBasicBlockInstruction(m_CurrentBlock, address, inst));
                    m_BlockWithDelaySlot = m_CurrentBlock;
                    BlockFinalize();
                    return;  // We can return after adding the delay slot
                }
                else {
                    m_BlockWithDelaySlot = null;
                    m_CurrentBlock.SetNullifiedDelaySlot();
                    BlockFinalize();
                }
            }

            /* ----- Time for a new block ----- */
            if (m_CurrentBlock == null)
            {
                m_CurrentBlock = new InfoBasicBlock(this, address);

                // Check if last block was interrupted
                if (m_Interrupted) {
                    m_CurrentBlock.IsExceptionHandler = true;
                    m_Interrupted = false;
                }
            }

            bool blockWillEnd = inst.IsBranch && m_CurrentBlock.Ending == BlockEnding.None;

            m_CurrentBlock.Append(new InfoBasicBlockInstruction(m_CurrentBlock, address, inst));

            if (blockWillEnd)
            {
                bool isEret = inst.Opcode.StartsWith("eret");

                if (isEret) {
                    m_CurrentBlock.Finish(BlockEnding.ExceptionReturn);
                    BlockFinalize();

                }
                else {
                    m_CurrentBlock.Finish(BlockEnding.NormalJump);
                }

                // TODO: Debug log block ending
            }
        }

        internal void MakeEntryNote(long pc)
        {
            m_EntryPoint = pc;
        }

        // Run some optimal checks and add block to the list
        public void BlockFinalize() {
            // Check if the current block is a repeat of the last
            if (!EnableFullTracing && m_CurrentBlock != null && m_Blocks.Count > 0) {

                // Simple block duplication check
                var lastBlock = m_Blocks[^1];
                if (lastBlock.Equals(m_CurrentBlock)) {
                    // Identical
                    lastBlock.Repeat++;
                    m_CurrentBlock = null;
                    return;
                }
            }

            m_Blocks.Add(m_CurrentBlock);
            m_CurrentBlock = null;
        }

        public IList<String> GenerateTraceLog()
        {
            List<String> traceLog = new List<string>();
            bool inServiceHandler = false;
            StringBuilder traceLineBuilder = new StringBuilder();

            if (!EnableFullTracing && !DisableAdvancedTraceReduction) {
                // Perform some post-processing block reduction
                TraceOptimizer.TrimMultiBlockLoops(this, m_Blocks);
            }

            for (int i = 0; i < m_Blocks.Count; i++)
            {
                var block = m_Blocks[i];

                // Just a note block
                if (block is NoteBasicBlock noteBlock)
                {
                    traceLog.Add(noteBlock.Note);
                    continue;
                }

                var code = block.GetCodeRecordSet();

                if (!inServiceHandler && block.IsExceptionHandler) {
                    inServiceHandler = true;
                    traceLog.Add("/* Exception Handler Begin */");
                }

                for (int j = 0; j < code.Length; j++)
                {
                    var tracedInstruction = code[j];
                    traceLineBuilder.Length = 0;
                    traceLineBuilder.Append(tracedInstruction.ToString());

                    var memAccessNote = block.LookupMemAccess(j);

                    if (memAccessNote != null) {
                        traceLineBuilder.Append($"  // {memAccessNote.ReadMeta()}");
                    }

                    if (m_EntryPoint != null && m_EntryPoint.Value == tracedInstruction.Address) {
                        traceLog.Add("/* Program Entry Point */");
                    }

                    traceLog.Add(traceLineBuilder.ToString());
                }

                if (block.Repeat > 0) {
                    traceLog.Add($"( Repeats for {block.Repeat} time(s) )");
                }

                if (inServiceHandler && block.Ending == BlockEnding.ExceptionReturn) {
                    inServiceHandler = false;
                    traceLog.Add("/* Exception Handler End */");
                }
            }

            return traceLog;
        }

        public void TraceMemoryAccess(ulong address, bool isWrite, String val)
        {
            if ((Details & TraceDetails.MemoryAccess) == TraceDetails.MemoryAccess)
            {
                if (m_CurrentBlock != null) {
                    if (m_CurrentBlock.Size > 0) {
                        m_CurrentBlock.SetMemAccess(
                            m_CurrentBlock.Size - 1,
                            new MemoryAccessMeta(address, isWrite, val));
                    }
                    return;
                }

                if (m_BlockWithDelaySlot != null) {
                        m_BlockWithDelaySlot.SetMemAccess(
                            m_BlockWithDelaySlot.Size - 1,
                            new MemoryAccessMeta(address, isWrite, val));
                    m_BlockWithDelaySlot = null;
                }
            }
        }

        public bool HasContent => m_Blocks.Count > 0;

        public TraceDetails Details { get; set; }

        public BaseDisassembler Disassembler => m_Disassembler;

        public bool FilterInterruptHandlers { get; set; }
    }
}
