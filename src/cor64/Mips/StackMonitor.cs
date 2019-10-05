using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.Mips
{
    public class StackMonitor
    {
        private Stack<ulong> m_StackPointers = new Stack<ulong>();

        public void PointerUpdate(ulong value)
        {
            if (m_StackPointers.Count == 0)
            {
                m_StackPointers.Push(value);
            }
            else
            {
                if (value == m_StackPointers.Peek())
                {
                    return;
                }
                else if (value > m_StackPointers.Peek())
                {
                    m_StackPointers.Push(value);
                }

                else if (value < m_StackPointers.Peek())
                {
                    while (m_StackPointers.Count > 0 && value < m_StackPointers.Peek())
                    {
                        m_StackPointers.Pop();
                    }

                    if (m_StackPointers.Count > 0 && value == m_StackPointers.Peek())
                    {
                        return;
                    }

                    m_StackPointers.Push(value);
                }
            }

        }

        public String DumpStack()
        {
            StringBuilder sb = new StringBuilder();

            // Make a copy
            var copy = new Stack<ulong>(m_StackPointers);

            for (int i = 0; i < copy.Count; i++)
            {
                sb.AppendLine(m_StackPointers.Pop().ToString("X8"));
            }

            return sb.ToString();
        }
    }
}
