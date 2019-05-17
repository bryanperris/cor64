using cor64.BassSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64
{
    public class BinarySource : ISource
    {
        private MemoryStream m_BinarySteam;

        public BinarySource(Stream src)
        {
            byte[] b = new byte[src.Length];
            src.Position = 0;
            src.Read(b, 0, b.Length);
            m_BinarySteam = new MemoryStream();
            m_BinarySteam.Write(b, 0, b.Length);
            m_BinarySteam.Position = 0;
        }

        public Stream getStream()
        {
            return m_BinarySteam;
        }
    }
}
