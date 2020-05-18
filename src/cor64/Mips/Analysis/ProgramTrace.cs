using cor64.Debugging;
using cor64.Mips.R4300I;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace cor64.Mips.Analysis
{

    // TODO: Handle exception jumps

    public class ProgramTrace
    {
        private readonly static Logger Log = LogManager.GetCurrentClassLogger();
        private readonly Dictionary<ulong, InfoBasicBlock> m_InstMapping = new Dictionary<ulong, InfoBasicBlock>();
        private InfoBasicBlock m_Root;
        private readonly List<InfoBasicBlock> m_Blocks = new List<InfoBasicBlock>();
        private readonly List<String> m_TestLog = new List<string>();
        private readonly BaseDisassembler m_Disassembler;
        private readonly List<InfoBasicBlockLink> m_Links = new List<InfoBasicBlockLink>();
        private InfoBasicBlockLink m_LastLink;
        private bool m_InServiceHandler;

        /* Builder State */
        private InfoBasicBlock m_CurrentBlock;
        private InfoBasicBlockInstruction m_CurrentInst;
        private bool m_EndOfBlock = false;
        private bool m_InDelaySlot = false;
        private bool m_EndedWithEret = false;
        private ulong m_LastBranch = 0;
        private bool m_Freeze;
        private InfoBasicBlock m_LastBlock;

        /* Debugging */
        private bool m_TestMode = false;

        /* Meta attachments */
        private MemoryAccessMeta m_Attachment_MemAccess = null;

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

        public void SetTestMode() {
            m_TestMode = true;
        }

        public ProgramTrace(BaseDisassembler disassembler)
        {
            m_Disassembler = disassembler;
        }

        public void AppendInstruction(DecodedInstruction inst, bool nullified)
        {
            if (m_TestMode) {
                var label = m_Disassembler.GetLabel(inst.Address);

                if (!String.IsNullOrEmpty(label)) {
                    m_TestLog.Add(label + ":");
                }

                m_TestLog.Add(new InfoBasicBlockInstruction(m_Disassembler, inst, nullified).ToString());
            }

            /* End of block indicated via delay slot signal */
            if (m_EndOfBlock)
            {
                /* First determine if we truly are executing the delay slot */
                if (!m_EndedWithEret && !m_InDelaySlot && m_LastBranch + 4 == inst.Address)
                {
                    m_InDelaySlot = true;
                }
                else
                {
                    /* End the block and move on to the next when we have moved out of the delay slot */
                    if ((m_InDelaySlot && m_LastBranch + 4 != inst.Address) || m_EndedWithEret) {
                        m_LastBlock = m_CurrentBlock;

                        /* Check if the instruction has been associated with a block already */
                        if (m_InstMapping.ContainsKey(inst.Address))
                        {
                            m_CurrentBlock = m_InstMapping[inst.Address];
                            m_Freeze = true;
                        }
                        else
                        {
                            /* We jumped somewhere new */
                            m_Freeze = false;
                            m_CurrentBlock = null;
                        }

                        /* Ok we are on the next block */
                        m_EndOfBlock = false;
                        m_InDelaySlot = false;
                        m_EndedWithEret = false;
                    }
                }
            }

            /* Append the instruction to the current block */
            if (m_CurrentBlock == null)
            {
                m_CurrentBlock = new InfoBasicBlock(inst.Address);
                m_Blocks.Add(m_CurrentBlock);

                if (m_Blocks.Count == 1)
                {
                    m_Root = m_CurrentBlock;
                }
            }

            /* Link blocks if not null */
            if (m_LastBlock != null)
            {
                InfoBasicBlockLink link;
                int blockIndex = 0;

                /* Subtract 4 because hitting this spot should be after end of block */
                if (inst.Address != m_CurrentBlock.Address) {
                    var byteOffset = inst.Address - 4 - m_CurrentBlock.Address;
                    blockIndex = (int)byteOffset / 4;
                    link = new InfoBasicBlockLink(m_CurrentBlock, blockIndex);
                }
                else {
                    link = new InfoBasicBlockLink(m_CurrentBlock, 0);
                }

                /* Detect a simple loop */
                if (m_LastLink != null && link.LinkedBlock.Address == m_LastLink.LinkedBlock.Address /*&& link.BlockOffset == m_LastLink.BlockOffset*/) {
                    m_LastLink.IncrementRepeat();
                }
                else {
                    m_LastLink = link;
                    m_Links.Add(m_LastLink);
                    m_LastBlock.AppendBlockLink(link);
                }

                m_LastBlock = null;
            }

            if (!m_Freeze)
            {
                m_CurrentInst = new InfoBasicBlockInstruction(m_Disassembler, inst, nullified);

                /* This happens when a block overlaps another block, they just get merged */
                if (m_InstMapping.ContainsKey(inst.Address))
                {
                    var originalBlock = m_InstMapping[inst.Address];
                    var originalBlockAddress = originalBlock.Address;

                    /* Update block linking */
                    foreach (var blockLink in m_Links) {
                        /* Update links that have to be shifted to the correct offset */
                        if (blockLink.TargetAddress == originalBlockAddress) {
                            blockLink.Modify(originalBlock, blockLink.BlockOffset + m_CurrentBlock.InstructionList.Count);
                        }

                        /* Update links that point to the block that will be removed */
                        if (blockLink.TargetAddress == m_CurrentBlock.Address) {
                            blockLink.Modify(originalBlock, 0);
                        }
                    }

                    /* Merge the new block into the existing one */
                    originalBlock.MergeBlockToHead(m_CurrentBlock);

                    /* Update the instuction mapping */
                    for (uint i = 0; i < originalBlock.Size; i++)
                    {
                        m_InstMapping[originalBlock.Address + i] = originalBlock;
                    }

                    m_Freeze = true;
                    m_Blocks.Remove(m_CurrentBlock);
                    m_CurrentBlock = originalBlock;
                }
                else
                {
                    m_CurrentBlock.Append(m_CurrentInst);
                    m_InstMapping.Add(inst.Address, m_CurrentBlock);
                }
            }
            else
            {
                if (!m_InstMapping.ContainsKey(inst.Address)) {
                    throw new InvalidOperationException(String.Format("Instruction wasn't found in the instruction map: {0:X8} {1}", inst.Address, m_Disassembler.GetFullDisassembly(inst)));
                }

                /* We don't modify anything here, just update the current block/inst we are in, so notes can be added to them */

                m_CurrentBlock = m_InstMapping[inst.Address];
                var blockIndex = (int) ((inst.Address - m_CurrentBlock.Address) / 4);
                m_CurrentInst = m_CurrentBlock.InstructionList[blockIndex];
            }

            if (inst.IsBranch)
            {
                m_EndOfBlock = true;
                m_LastBranch = inst.Address;
            }

            if (inst.Opcode.StartsWith("eret")) {
                m_EndedWithEret = true;
                m_EndOfBlock = true;
                m_LastBranch = inst.Address;
            }

            /* Process metadata attachments */
            if (m_Attachment_MemAccess != null)
            {
                m_CurrentInst.AppendMemoryAccess(m_Attachment_MemAccess);
                m_Attachment_MemAccess = null;
            }
        }

        public IList<String> GenerateTraceLog()
        {
            List<String> traceLog = new List<string>();
            Dictionary<ulong, int> blockHitCounter = new Dictionary<ulong, int>();
            var currBlock = m_Root;
            var blockOffset = 0;

            while (currBlock != null)
            {
                var code = currBlock.GetCodeRecordSet();
                var links = currBlock.Links;

                if (!blockHitCounter.ContainsKey(currBlock.Address))
                {
                    blockHitCounter.Add(currBlock.Address, 0);
                }

                var linkIndex = blockHitCounter[currBlock.Address];

                for (int i = blockOffset; i < code.Length; i++)
                {
                    var codeLine = code[i];
                    codeLine.IncrementUsageRef();

                    var label = m_Disassembler.GetLabel(codeLine.Address, false);

                    if (!String.IsNullOrEmpty(label)) {
                        traceLog.Add(label + ":");
                    }

                    if (FilterInterruptHandlers) {
                        if (codeLine.Inst.Address == 0x80000180) {
                            m_InServiceHandler = true;
                        }
                    }

                    if (m_InServiceHandler) {
                        if (codeLine.Inst.Op.ID == Opcodes.ERET.ID) {
                            m_InServiceHandler = false;
                            continue;
                        }

                        continue;
                    }

                    codeLine.AddToLog(traceLog);
                }

                if (linkIndex < links.Count)
                {
                    /* Get the next link */
                    var link = links[linkIndex];

                    /* Does it repeat the last block? */
                    if (link.Repeat > 0) {
                        traceLog.Add(String.Format("( Repeats for {0} time(s) )", link.Repeat));
                        blockHitCounter[currBlock.Address] += link.Repeat;

                        if (link.LinkedBlock.Address == currBlock.Address) {
                            if (linkIndex + 1 < link.LinkedBlock.Links.Count) {
                                link = links[linkIndex + 1];
                            }
                        }
                    }
                    else {
                        blockHitCounter[currBlock.Address]++;
                    }

                    currBlock = link.LinkedBlock;
                    blockOffset = link.BlockOffset;
                }
                else
                {
                    break;
                }
            }

            return traceLog;
        }

        public void AddInstructionMemAccess(ulong address, bool isWrite, String val)
        {
            if ((Details & TraceDetails.MemoryAccess) == TraceDetails.MemoryAccess)
            {
                m_Attachment_MemAccess = new MemoryAccessMeta(address, isWrite, val);
            }
        }

        public int Size => m_Blocks.Count;

        public TraceDetails Details { get; set; }

        public IList<String> TestLog => m_TestLog;

        public bool FilterInterruptHandlers { get; set; }

        private void AddJumpArrow(IList<String> input, int start, int end, int repeat)
        {
            int maxLineLength = 0;

            for (int i = start; i <= end; i++)
            {
                maxLineLength = Math.Max(maxLineLength, input[i].Length);
            }

            for (int i = start; i <= end; i++)
            {
                int count = Math.Abs(maxLineLength - input[i].Length);

                if (count > 0)
                {
                    input[i] += new string(' ', count);
                }

                if (i == start)
                {
                    input[i] += " <------+";

                    if (repeat > 0)
                        input[i] += String.Format("[{0}]", repeat);
                }
                else if (i == end)
                {
                    input[i] += "       -+";
                }
                else
                {
                    input[i] += "        |";
                }
            }
        }
    }
}
