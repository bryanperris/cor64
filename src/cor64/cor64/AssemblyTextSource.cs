using cor64.BassSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64
{
    public class AssemblyTextSource : ISource
    {
        StringBuilder m_StringBuilder = new StringBuilder();
        List<String> m_AssemblyLines = new List<string>();
        private String m_Name;

        public String Name => m_Name;

        public AssemblyTextSource(String name)
        {
            m_Name = name;
            m_StringBuilder.Clear();
            m_AssemblyLines.Clear();
        }

        public IList<String> AssemblyLines => m_AssemblyLines;

        private void Copy()
        {
            m_StringBuilder.Clear();

            for (int i = 0; i < m_AssemblyLines.Count; i++) {
                String line = m_AssemblyLines[i] + "\n";
                m_StringBuilder.Insert(m_StringBuilder.Length, line);
            }
        }

        public Stream getStream()
        {
            var s = new MemoryStream();
            StreamWriter writer = new StreamWriter(s);
            Copy();
            writer.Write(m_StringBuilder.ToString());
            writer.Flush();
            return s;
        }

        public static AssemblyTextSource operator +(AssemblyTextSource src, String value)
        {
            src.AssemblyLines.Add(value);
            return src;
        }

        public static AssemblyTextSource operator -(AssemblyTextSource src, String value)
        {
            src.AssemblyLines.Remove(value);
            return src;
        }
    }
}
