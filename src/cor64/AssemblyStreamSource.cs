using cor64.BassSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64
{
    public class AssemblyStreamSource : NamedAssemblySource
    {
        private readonly String m_Name;
        private readonly Stream m_Source;

        public override String Name => m_Name;

        public AssemblyStreamSource(String name, Stream source)
        {
            m_Name = name;
            m_Source = source;
        }

        public override Stream GetStream()
        {
            return m_Source;
        }
    }
}
