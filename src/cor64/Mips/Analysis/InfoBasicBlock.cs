using System.Reflection.Emit;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace cor64.Mips.Analysis
{
    public class InfoBasicBlock : BasicBlock<InfoBasicBlockInstruction>
    {
        private readonly List<InfoBasicBlockLink> m_Links = new List<InfoBasicBlockLink>();

        public InfoBasicBlock(ulong address) : base(address)
        {

        }

        public void AppendBlockLink(InfoBasicBlockLink link)
        {
            m_Links.Add(link);
        }

        public void MergeBlockToHead(InfoBasicBlock block)
        {
            var code = block.GetCodeRecordSet();

            for (int i = 0; i < code.Length; i++)
            {
                RecordSet.Insert(i, code[i]);
                Address -= 4;
            }

            for (int i = 0; i < block.Links.Count; i++) {
                m_Links.Add(block.Links[i]);
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

        public bool StartsInterruptServicing { get; set; }

        public bool EndsWithExceptionReturn {
            get {
                return InstructionList.Last().Inst.Opcode.StartsWith("eret");
            }
        }
    }
}
