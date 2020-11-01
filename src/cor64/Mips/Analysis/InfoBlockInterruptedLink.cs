using System;
namespace cor64.Mips.Analysis
{
    public class InfoBlockInterruptedLink : InfoBasicBlockLink
    {
        public int InterruptedInst { get; }

        public InfoBlockInterruptedLink(InfoBasicBlock block, int offset, int interruptedInst) : base(block, offset)
        {
            InterruptedInst = interruptedInst;
        }


        public override string ToString()
        {
            return String.Format("Linked to interrupting block {0:X8} + {1}", LinkedBlock.Address, BlockOffset);
        }
    }
}
