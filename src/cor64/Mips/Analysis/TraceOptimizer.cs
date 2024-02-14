using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace cor64.Mips.Analysis
{
    public static class TraceOptimizer
    {

        public static void TrimMultiBlockLoops(ProgramTrace traceLog, IList<InfoBasicBlock> blockList) {
            // First lets make a hash count
            Dictionary<InfoBasicBlock, int> m_BlocksSeen = new Dictionary<InfoBasicBlock, int>();
            bool inLoop = false;
            int listOffset = 0;
            int blocksInLoop = 0;


            for (int i = 0; i < blockList.Count; i++) {
                var block = blockList[i];

                if (block is NoteBasicBlock)
                    continue;

                if (block.IsExceptionHandler) {
                    // We were interrupted (this breaks the pattern)
                    m_BlocksSeen.Clear();
                    inLoop = false;
                    continue;
                }

                if (!inLoop && !m_BlocksSeen.ContainsKey(block))
                {
                    m_BlocksSeen.Add(block, i);
                    continue;
                }
                else {
                    if (!inLoop) {
                        inLoop = true;
                        listOffset = i;
                    }
                }

                if (inLoop && m_BlocksSeen.ContainsKey(block)) {
                    blocksInLoop++;
                }
                else {
                    inLoop = false;

                    int shift = 0;

                    InfoBasicBlock start = null;
                    int loopRepeat = 0;

                    for (int j = listOffset; j < (listOffset + blocksInLoop); j++) {
                        // First fetch the block's duplicate
                        // Then fetch the original based on the duplicate
                        var duplicate = blockList[j - shift];
                        var original = blockList[m_BlocksSeen[duplicate]];

                        if (start == null) {
                            start = original;
                            loopRepeat++;
                        }
                        else {
                            if (start == original)
                                loopRepeat++;
                        }

                        // Removing blocks from the active list should be affect original block indexes

                        blockList.RemoveAt(j - shift);
                        i--; // i is paused at the block after the loop end, shift it back for every block removed
                        shift++;
                    }

                    // Generate the note
                    var note = new NoteBasicBlock(
                        traceLog,
                        $"Trace {start.Address:X8}-->{blockList[i-1].GetLastInst().Address:X8} repeats {loopRepeat} times");

                    blockList.Insert(i, note);

                    m_BlocksSeen.Clear();
                    i--; // allow the stopper block to get checked too

                    blocksInLoop = 0;
                }
            }
        }
    }
}