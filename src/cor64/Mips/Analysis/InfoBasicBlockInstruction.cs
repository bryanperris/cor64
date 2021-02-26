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
        private List<bool> m_NullifiedList = new List<bool>();
        private int m_HitCounter;
        private StringBuilder m_StringBuilder = new StringBuilder();
        private BaseDisassembler m_Disassembler;

        public InfoBasicBlockInstruction(BaseDisassembler disassembler, DecodedInstruction inst, bool isNullified)
        {
            m_Disassembler = disassembler;
            Inst = inst;
            Address = inst.Address;
            m_MemAccessList = new List<MemoryAccessMeta>();
            AppendNullifyUsage(isNullified);
        }

        public InfoBasicBlockInstruction(BaseDisassembler disassembler, DecodedInstruction inst)
        {
            m_Disassembler = disassembler;
            Inst = inst;
            Address = inst.Address;
            m_MemAccessList = new List<MemoryAccessMeta>();
        }

        public void AppendMemoryAccess(MemoryAccessMeta memoryAccessMeta)
        {
            m_MemAccessList.Add(memoryAccessMeta);
        }

        public MemoryAccessMeta GetLastMemAccessMeta() {
            if (m_MemAccessList.Count <= 0)
                return null;

            return m_MemAccessList[m_MemAccessList.Count - 1];
        }

        public DecodedInstruction Inst { get; }

        public ulong Address { get; }

        public void IncrementUsageRef()
        {
            m_HitCounter++;
        }

        public void ResetUsageRef()
        {
            m_HitCounter = 0;
        }

        public void AppendNullifyUsage(bool isNullified)
        {
            m_NullifiedList.Add(isNullified);
        }

        public void AddToLog(List<String> log)
        {
            m_StringBuilder.Clear();
            bool startComment = false;

            void CheckComment()
            {
                if (!startComment)
                {
                    startComment = true;
                    m_StringBuilder.Append("     // ");
                }
            }

            if (m_HitCounter < m_NullifiedList.Count && m_NullifiedList[m_HitCounter])
            {
                m_StringBuilder.AppendFormat("{0} [NULLIFIED]", ToString());
            }
            else
            {
                m_StringBuilder.Append(ToString());
            }

            if (m_MemAccessList.Count > 0 && m_HitCounter < m_MemAccessList.Count)
            {
                CheckComment();

                m_StringBuilder.Append(m_MemAccessList[m_HitCounter].ReadMeta());
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
