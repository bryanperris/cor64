using System.Data;
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

        public void AddToLog(List<String> log) {
            m_StringBuilder.Clear();
            bool startComment = false;

            void CheckComment() {
                if (!startComment) {
                    startComment = true;
                    m_StringBuilder.Append("     // ");
                }
            }

            m_StringBuilder.Append(this.ToString());

            if (m_MemAccessList.Count > 0 && m_MemRef < m_MemAccessList.Count)
            {
                CheckComment();

                m_StringBuilder.Append(m_MemAccessList[m_MemRef].ReadMeta());
                m_StringBuilder.Append("; ");
            }

            log.Add(m_StringBuilder.ToString());
        }

        public override string ToString()
        {
            return String.Format("{0:X8} {1}", Address, m_Disassembler.GetFullDisassembly(Inst));
        }
    }
}
