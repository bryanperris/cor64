using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace cor64.Mips.Analysis
{
    public class InfoBasicBlock : BasicBlock<InfoBasicBlockInstruction>
    {
        private List<InfoBasicBlockLink> m_Links = new List<InfoBasicBlockLink>();

        public InfoBasicBlock(ulong address) : base(address, 1024 / 4)
        {

        }

        public InfoBasicBlockLink LinkBlock(InfoBasicBlock basicBlock, int offset)
        {
            var link = new InfoBasicBlockLink(basicBlock, offset);
            m_Links.Add(link);
            return link;
        }

        public void MergeBlockToHead(InfoBasicBlock block)
        {
            var code = block.GetCodeRecordSet();

            for (int i = 0; i < code.Length; i++)
            {
                RecordSet.Insert(i, code[i]);
                SetAddress(Address - 4);
            }
        }

        public IReadOnlyList<InfoBasicBlockLink> Links => m_Links;

        internal void RemoveLastLink()
        {
            m_Links.RemoveAt(m_Links.Count - 1);
        }

        internal void RemoveLink(int offset)
        {
            m_Links.RemoveAt(offset);
        }

        internal void UpdateLinks(IReadOnlyList<InfoBasicBlockLink> newLinks)
        {
            m_Links.Clear();
            m_Links.AddRange(newLinks);
        }
    }
}
