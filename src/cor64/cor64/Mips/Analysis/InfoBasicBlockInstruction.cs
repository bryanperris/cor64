using cor64.Debugging;
using System;
using System.Collections.Generic;
using System.Text;

namespace cor64.Mips.Analysis
{
    public class InfoBasicBlockInstruction
    {
        private List<MemoryAccessMeta> m_MemAccessList;
        private int m_MemRef;
        private StringBuilder m_StringBuilder = new StringBuilder();
        private BaseDisassembler m_Disassembler;
        private bool m_IsNullifed;

        public InfoBasicBlockInstruction(BaseDisassembler disassembler, DecodedInstruction inst, bool nullified)
        {
            m_Disassembler = disassembler;
            Inst = inst;
            Address = inst.Address;
            m_MemAccessList = new List<MemoryAccessMeta>();
            m_IsNullifed = nullified;
        }

        public void AppendMemoryAccess(MemoryAccessMeta memoryAccessMeta)
        {
            m_MemAccessList.Add(memoryAccessMeta);
        }

        public DecodedInstruction Inst { get; }

        public ulong Address { get; }

        public void IncrementUsageRef()
        {
            m_MemRef++;
        }

        public void ResetUsageRef()
        {
            m_MemRef = 0;
        }

        public override string ToString()
        {
            m_StringBuilder.Clear();
            m_StringBuilder.Append(Address.ToString("X8"));
            m_StringBuilder.Append(" ");
            m_StringBuilder.Append(m_IsNullifed ? "(NULLIFIED) " : "");
            m_StringBuilder.AppendLine(m_Disassembler.GetFullDisassembly(Inst));

            if (m_MemAccessList.Count > 0)
            {
                if (m_MemRef < m_MemAccessList.Count)
                {
                    m_StringBuilder.Append("    >> ");
                    m_StringBuilder.AppendLine(m_MemAccessList[m_MemRef].ReadMeta());
                }
            }

            return m_StringBuilder.ToString();
        }
    }
}
