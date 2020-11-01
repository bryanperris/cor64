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

    // TODO: Handle exception jumps

    public class ProgramTrace
    {
        private readonly static Logger Log = LogManager.GetCurrentClassLogger();
        private InfoBasicBlock m_Root;
        private readonly BaseDisassembler m_Disassembler;
        private bool m_InServiceHandler;
        private bool m_HandleExceptionReturn;
        private readonly List<InfoBasicBlock> m_Blocks = new List<InfoBasicBlock>();
        private InfoBasicBlock m_CurrentBlock;
        private InfoBasicBlockInstruction m_CurrentInst;
        private bool m_End;
        private bool m_DelaySlot;
        private ulong m_LastBranch;
        private bool m_Frozen;
        private bool m_FrozenBeforeInterrupt;
        private InfoBasicBlockLink m_LastLink;
        private readonly List<InfoBasicBlockLink> m_Links = new List<InfoBasicBlockLink>();
        private InfoBasicBlock m_LastBlock;
        private readonly Dictionary<ulong, InfoBasicBlock> m_InstToBlockMapping = new Dictionary<ulong, InfoBasicBlock>();

        /* Debugging */
        private bool m_DoVerify = false;
        private readonly List<String> m_VerifyLog = new List<string>();
        private int m_VerifyLogPosition = 0;
        private bool m_HaltVerify = false;

        /* Meta attachments */
        private MemoryAccessMeta m_Attachment_MemAccess = null;

        public ulong StoppedAt { get; set; }

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

        public void EnableLogVerfication() {
            m_DoVerify = true;
        }

        public ProgramTrace(BaseDisassembler disassembler)
        {
            m_Disassembler = disassembler;
        }

        private void BeginNewBlock(ulong address) {
            m_End = false;
            m_Frozen = false;
            m_CurrentBlock = new InfoBasicBlock(address);

            #if DEBUG_TRACE_LOG
            Console.WriteLine("Create Block {0:X8}", m_CurrentBlock.Address);
            #endif

            m_Blocks.Add(m_CurrentBlock);

            if (m_Blocks.Count == 1)
            {
                m_Root = m_CurrentBlock;
            }
        }

        private void FinishBlock(DecodedInstruction inst) {
            bool isExceptionReturnEnding = m_HandleExceptionReturn;

            /* First determine if we truly are executing the delay slot */
            if (!m_CurrentBlock.EndsWithExceptionReturn && !m_DelaySlot && m_LastBranch + 4 == inst.Address)
            {
                m_DelaySlot = true;
            }
            else
            {
                /* End the block and move on to the next when we have moved out of the delay slot or eret */
                if ((m_DelaySlot && m_LastBranch + 4 != inst.Address) || m_CurrentBlock.EndsWithExceptionReturn) {
                    m_LastBlock = m_CurrentBlock;

                    /* We are leaving a block via ERET (interrupted / normal) */
                    if (m_HandleExceptionReturn) {
                        if (m_InServiceHandler) {
                            m_Frozen = m_FrozenBeforeInterrupt;
                        }

                        m_HandleExceptionReturn = false;
                        m_InServiceHandler = false;
                    }

                    /* Check if the instruction has been associated with a block already */
                    if (m_InstToBlockMapping.ContainsKey(inst.Address))
                    {
                        m_CurrentBlock = m_InstToBlockMapping[inst.Address];
                        m_Frozen = true;

                        #if DEBUG_TRACE_LOG
                        Console.WriteLine("Jumping to mapped inst {0:X8}", inst.Address);
                        #endif
                    }
                    else
                    {
                        /* We jumped somewhere new */
                        m_Frozen = false;
                        m_CurrentBlock = null;

                        #if DEBUG_TRACE_LOG
                        Console.WriteLine("Cleared current block after block end (inst {0:X8} is not mapped)", inst.Address);
                        #endif
                    }

                    /* Ok we are on the next block */
                    m_End = false;
                    m_DelaySlot = false;
                }
            }
        }

        public void AppendInstruction(DecodedInstruction inst, bool nullified, bool inInterrupt)
        {
            #if DEBUG_TRACE_LOG
            var mapped = m_InstToBlockMapping.ContainsKey(inst.Address);

            Console.WriteLine("TRACE INST: [{2}] {0:X8} {1}", inst.Address, m_Disassembler.GetFullDisassembly(inst),
            mapped ? " M" : "UM");
            #endif

            if (m_DoVerify) {
                m_VerifyLog.Add(new InfoBasicBlockInstruction(m_Disassembler, inst, nullified).ToString());
            }

            if (inInterrupt) {
                // If we just interrupted in execution, start building a new block for the interrupt handler
                if (!m_InServiceHandler) {
                    m_HandleExceptionReturn = false;
                    m_InServiceHandler = true;

                    var interruptedBlock = m_CurrentBlock;

                    m_FrozenBeforeInterrupt = m_Frozen;
                    m_Frozen = m_InstToBlockMapping.ContainsKey(inst.Address);

                    if (!m_Frozen) {
                        BeginNewBlock(inst.Address);
                        m_CurrentBlock.StartsInterruptServicing = true;
                    }
                    else {
                        m_CurrentBlock = m_InstToBlockMapping[inst.Address];
                    }

                    // Check if interrupt repeated and needs frozen mode

                    /* [Normal Block @ X] -> [Interrupt Handler Block] */
                    var handlerLink = new InfoBlockInterruptedLink(m_CurrentBlock, 0, interruptedBlock.Size - 1);

                    m_Links.Add(handlerLink);
                    interruptedBlock.AppendBlockLink(handlerLink);

                    #if DEBUG_TRACE_LOG
                    Console.WriteLine("Link block {0:X8} to interrupt block {1:X8} +{2}", interruptedBlock.Address, m_CurrentBlock.Address, handlerLink.BlockOffset);
                    #endif
                }
            }

            /* End of block indicated via delay slot signal */
            if (m_End)
            {
                FinishBlock(inst);
            }

            /* Check if we need to create a new block */
            if (m_CurrentBlock == null)
            {
                BeginNewBlock(inst.Address);
            }

            /* Link blocks if not null */
            if (m_LastBlock != null)
            {
                InfoBasicBlockLink link;

                if (inst.Address != m_CurrentBlock.Address) {
                    var addr = inst.Address;
                    var byteOffset = addr - m_CurrentBlock.Address;
                    int blockIndex = (int)byteOffset / 4;
                    link = new InfoBasicBlockLink(m_CurrentBlock, blockIndex);
                }
                else {
                    link = new InfoBasicBlockLink(m_CurrentBlock, 0);
                }

                m_LastLink = link;
                m_Links.Add(m_LastLink);
                m_LastBlock.AppendBlockLink(link);

                #if DEBUG_TRACE_LOG
                Console.WriteLine("Link block {0:X8} to block {1:X8} +{2}", m_LastBlock.Address, m_CurrentBlock.Address, link.BlockOffset);
                #endif

                m_LastBlock = null;
            }

            if (!m_Frozen)
            {
                m_CurrentInst = new InfoBasicBlockInstruction(m_Disassembler, inst);

                /* This happens when a block overlaps another block, they just get merged */
                if (m_InstToBlockMapping.ContainsKey(inst.Address))
                {
                    var originalBlock = m_InstToBlockMapping[inst.Address];
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
                        m_InstToBlockMapping[originalBlock.Address + i] = originalBlock;
                    }

                    m_Frozen = true;
                    m_Blocks.Remove(m_CurrentBlock);
                    m_CurrentBlock = originalBlock;
                }
                else
                {
                    m_CurrentBlock.Append(m_CurrentInst);
                    m_InstToBlockMapping.Add(inst.Address, m_CurrentBlock);

                    // #if DEBUG_TRACE_LOG
                    // Console.WriteLine("Add inst {0:X8} to block {1:X8}", inst.Address, m_CurrentBlock.Address);
                    // #endif
                }
            }
            else
            {
                if (!m_InstToBlockMapping.ContainsKey(inst.Address)) {
                    throw new InvalidOperationException(String.Format("Inst wasn't found in the block map: {0:X8} {1}", inst.Address, m_Disassembler.GetFullDisassembly(inst)));
                }

                /* We don't modify anything here, just update the current block/inst we are in, so notes can be added to them */

                m_CurrentBlock = m_InstToBlockMapping[inst.Address];
                var blockIndex = (int) ((inst.Address - m_CurrentBlock.Address) / 4);

                if (blockIndex >= m_CurrentBlock.InstructionList.Count) {
                    throw new InvalidOperationException(String.Format("instruction is out of range of block: {0:X8} {1}", inst.Address, m_Disassembler.GetFullDisassembly(inst)));
                }

                m_CurrentInst = m_CurrentBlock.InstructionList[blockIndex];
            }

            if (inst.IsBranch)
            {
                m_End = true;
                m_LastBranch = inst.Address;
                m_HandleExceptionReturn = inst.Opcode.StartsWith("eret");
            }

            /* Process metadata attachments */
            if (m_Attachment_MemAccess != null)
            {
                m_CurrentInst.AppendMemoryAccess(m_Attachment_MemAccess);
                m_Attachment_MemAccess = null;
            }

            m_CurrentInst.AppendNullifyUsage(nullified);

#if TRACE_LOG_HALT
            if (m_VerifyLog.Count > 0 && m_VerifyLog[^1] != m_CurrentInst.ToString())
            {
                throw new InvalidOperationException();
            }
#endif
        }

        public IList<String> GenerateTraceLog()
        {
            List<String> traceLog = new List<string>();
            Dictionary<ulong, int> blockHitCounter = new Dictionary<ulong, int>();
            var currBlock = m_Root;
            var blockOffset = 0;
            bool inServiceHandler = false;

            while (currBlock != null)
            {
                #if DEBUG_TRACE_LOG
                Console.WriteLine("TRACEGEN: Current Block {0:X8}", currBlock.Address);
                #endif

                var code = currBlock.GetCodeRecordSet();
                var links = currBlock.Links;

                if (currBlock.StartsInterruptServicing) {
                    inServiceHandler = true;
                    traceLog.Add("/* Exception Handler Begin */");
                }

                if (!blockHitCounter.ContainsKey(currBlock.Address))
                {
                    blockHitCounter.Add(currBlock.Address, 0);
                }

                var linkIndex = blockHitCounter[currBlock.Address];

                InfoBlockInterruptedLink interruptLink = null;

                // Check if the intruction was interrupted
                if (linkIndex < links.Count) {
                    interruptLink = links[linkIndex] as InfoBlockInterruptedLink;
                }

                for (int i = blockOffset; i < code.Length; i++)
                {
                    var codeLine = code[i];

                    // When execution was stopped during the last block, don't trace beyond the halt
                    if (m_CurrentBlock.Address == currBlock.Address && codeLine.Address > StoppedAt) {
                        currBlock = null;
                        break;
                    }

                    codeLine.AddToLog(traceLog);


                    if (m_DoVerify && m_VerifyLogPosition < m_VerifyLog.Count) {
                        var raw = m_VerifyLog[m_VerifyLogPosition++];
                        var l = codeLine.ToString();

                        traceLog.Add(raw + " ~");

                        if (!m_HaltVerify && !l.Equals(raw)) {
                            traceLog.Add("Mismatched instructions");
                            m_HaltVerify = true;
                        }

                     //   traceLog[^1] = raw + "                   " + traceLog[^1];
                    }

                    codeLine.IncrementUsageRef();

                    if (!inServiceHandler && interruptLink != null) {
                        // We hit an interruption instruction in the block
                        if (interruptLink.InterruptedInst == i) {
                            break;
                        }
                    }
                }

                if (currBlock == null) {
                    break;
                }

                if (inServiceHandler && currBlock.EndsWithExceptionReturn) {
                    inServiceHandler = false;
                    traceLog.Add("/* Exception Handler End */");
                }

                if (linkIndex < links.Count)
                {
                    /* Get the next link */
                    var link = links[linkIndex];

                    // Check for repeating links that are adjecent to each other
                    if (linkIndex + 1 < links.Count &&
                       link.TargetAddress == links[linkIndex + 1].TargetAddress &&
                       link.TargetAddress == currBlock.Address + (ulong)link.BlockOffset)
                    {
                        int repeat = 1;
                        var oldLinkList = links.ToList();

                        for (int i = linkIndex + 1; i < oldLinkList.Count; i++) {

                            if (oldLinkList[linkIndex].TargetAddress != oldLinkList[i].TargetAddress) {
                                var sizeForRepeated = currBlock.Size - link.BlockOffset;
                                traceLog.Add(String.Format("( Repeats for {0} time(s) )", repeat));
                                blockHitCounter[currBlock.Address] += repeat ;
                                link = links[linkIndex + repeat];
                                m_VerifyLogPosition += repeat * sizeForRepeated;
                                break;
                            }

                            repeat++;
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

        public bool HasContent => m_Blocks.Count > 0;

        public TraceDetails Details { get; set; }

        public IList<String> TestLog => m_VerifyLog;

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
