using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.Debugging
{
    /// <summary>
    /// Records physical memory access
    /// </summary>
    public class MemoryAccessMeta : IDebugMeta
    {
        private static MemoryDebugger s_MemDebugger = new MemoryDebugger();
        private ulong m_Address;
        private bool m_IsWriteAccess;
        private string m_RegionName;
        private StringBuilder m_StringBuilder = new StringBuilder();
        private string m_Value;

        public MemoryAccessMeta(ulong address, bool isWrite, string val)
        {
            m_Address = address;
            m_IsWriteAccess = isWrite;
            m_RegionName = s_MemDebugger.GetMemName((uint)address);
            m_Value = val;
        }

        public string ReadMeta()
        {
            m_StringBuilder.Clear();

            if (m_IsWriteAccess)
                m_StringBuilder.Append("Memory write access to ");
            else
                m_StringBuilder.Append("Memory read access to ");

            m_StringBuilder.Append(m_RegionName);
            m_StringBuilder.Append(": ");
            m_StringBuilder.Append(m_Address.ToString("X8"));

            if (m_Value != null)
            {
                m_StringBuilder.Append(" = ");
                m_StringBuilder.Append(m_Value);
            }

            return m_StringBuilder.ToString();
        }

        public void AppendValue(string value) {
            m_Value += value;
        }

        public string Value => m_Value;

        public override string ToString()
        {
            return m_RegionName;
        }
    }
}
