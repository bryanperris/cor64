using cor64.Debugging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace cor64.Mips.Analysis
{

    // TODO: Handle exception jumps

    public class ProgramTrace
    {
        private Dictionary<ulong, InfoBasicBlock> m_BlockRef = new Dictionary<ulong, InfoBasicBlock>();
        private InfoBasicBlock m_Root;
        private List<InfoBasicBlock> m_Blocks = new List<InfoBasicBlock>();
        private List<String> m_RawTraceLog = new List<string>();
        private BaseDisassembler m_Disassembler;

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

        /* Program Informaton */
        private ulong m_EntryPoint;

        /* Builder State */
        private InfoBasicBlock m_CurrentBlock;
        private InfoBasicBlockInstruction m_CurrentInst;
        private bool m_EndOfBlock = false;
        private bool m_InDelaySlot = false;
        private ulong m_LastBranch = 0;
        private bool m_Freeze;
        private InfoBasicBlock m_LastBlock;
        private InfoBasicBlockLink m_LastLink;

        /* Debugging */
        private bool m_DebugCheck = false;

        public ProgramTrace(BaseDisassembler disassembler)
        {
            m_Disassembler = disassembler;
        }

        public void AppendInstruction(DecodedInstruction inst, bool nullified)
        {
            //String blockHasInst = m_BlockRef.ContainsKey(inst.Address) ? "CACHED" : "APPEND";

            if (m_DebugCheck)
                m_RawTraceLog.Add(new InfoBasicBlockInstruction(m_Disassembler, inst, nullified).ToString());

            /* End of block indicated via delay slot signal */
            if (m_EndOfBlock)
            {
                /* First determine if we truly are executing the delay slot */
                if (!m_InDelaySlot && m_LastBranch + 4 == inst.Address)
                {
                    m_InDelaySlot = true;
                    //Console.WriteLine("IN DELAY SLOT");
                }
                else
                {
                    m_LastBlock = m_CurrentBlock;

                    if (m_BlockRef.ContainsKey(inst.Address))
                    {
                        var block = m_BlockRef[inst.Address];
                        m_CurrentBlock = block;
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

                    //Console.WriteLine("NEXT BLOCK");
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
                m_LastLink = m_LastBlock.LinkBlock(m_CurrentBlock, (int)(inst.Address - m_CurrentBlock.Address) / 4);
                m_LastBlock = null;
            }

            if (!m_Freeze)
            {
                m_CurrentInst = new InfoBasicBlockInstruction(m_Disassembler, inst, nullified);
                m_CurrentBlock.Append(m_CurrentInst);

                if (m_BlockRef.ContainsKey(inst.Address))
                {
                    /* This happens when a block overlaps another block, they just get merged */

                    /* Merge the new block into the existing one */
                    var block = m_BlockRef[inst.Address];
                    block.MergeBlockToHead(m_CurrentBlock);

                    /* Fix the block ref table */
                    for (ulong i = block.Address; i <= (block.Address + (ulong)block.Size); i++)
                    {
                        m_BlockRef[i] = block;
                    }

                    /* Fix block linking */
                    // TODO: Is it safe to assume block link offset is always 0?
                    if (m_LastLink != null && m_LastLink.LinkedBlock == m_CurrentBlock)
                    {
                        m_LastLink.Modify(block, 0);
                    }

                    m_Freeze = true;
                    m_Blocks.Remove(m_CurrentBlock);
                    m_CurrentBlock = block;
                }
                else
                {
                    m_BlockRef.Add(inst.Address, m_CurrentBlock);
                }
            }
            else
            {
                var blockIndex = (inst.Address - m_CurrentBlock.Address) / 4;

                if ((int)blockIndex >= m_CurrentBlock.InstructionList.Count)
                {
                    throw new ArgumentOutOfRangeException("block index is out of range");
                }

                m_CurrentInst = m_CurrentBlock.InstructionList[(int)blockIndex];
            }

            if (inst.IsBranch)
            {
                m_EndOfBlock = true;
                m_LastBranch = inst.Address;
            }

            /* Process metadata attachments */
            if (m_Attachment_MemAccess != null)
            {
                m_CurrentInst.AppendMemoryAccess(m_Attachment_MemAccess);
                m_Attachment_MemAccess = null;
            }

            //String end = m_EndOfBlock ? "BLOCKEND" : "";
            //String frozen = m_Freeze ? "FROZEN" : "NOTFROZEN";
            //String status = blockHasInst + " " + frozen + " " + end;
            //Console.WriteLine("{0:X8} {1} {2}", inst.Address, m_Disassembler.GetFullDisassembly(inst), status);
        }

        internal void Backtrack()
        {
            throw new NotImplementedException();
        }

        public string GenerateTraceLog()
        {
            StringBuilder stringBuilder = new StringBuilder();
            Dictionary<ulong, int> blockHitCounter = new Dictionary<ulong, int>();
            var currBlock = m_Root;
            var offset = 0;

            while (currBlock != null)
            {
                var code = currBlock.GetCodeRecordSet();
                var links = currBlock.Links;

                if (!blockHitCounter.ContainsKey(currBlock.Address))
                {
                    blockHitCounter.Add(currBlock.Address, 0);
                }

                var blockHit = blockHitCounter[currBlock.Address];

                for (int i = offset; i < code.Length; i++)
                {
                    var codeLine = code[i];
                    stringBuilder.Append(codeLine.ToString());
                    codeLine.IncrementUsageRef();
                }

                if (blockHit < links.Count)
                {
                    var oldBlock = currBlock;
                    currBlock = links[blockHit].LinkedBlock;
                    offset = links[blockHit].BlockOffset;
                    blockHitCounter[oldBlock.Address]++;
                }
                else
                {
                    break;
                }
            }

            return stringBuilder.ToString();
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
