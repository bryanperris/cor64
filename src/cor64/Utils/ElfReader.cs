using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cor64.IO;

namespace cor64.Utils
{
    public class ElfReader
    {
        private Stream m_Source;
        private int m_FieldSize = 4;
        private bool m_BigEndian = false;

        public ElfReader(Stream stream)
        {
            m_Source = stream;
        }

        private uint Read32(long offset)
        {
            m_Source.Position = offset;

            var s = m_Source;

            if (m_BigEndian)
            {
                s = new Swap32Stream(s);
            }

            return new BinaryReader(s).ReadUInt32();
        }

        public void Open()
        {
            m_Source.Position = 0;

            var reader = new BinaryReader(m_Source);

            /* Read the important bits */
            m_Source.Position = 4;

            if (reader.ReadByte() == 2)
            {
                m_FieldSize = 8;
            }

            if (reader.ReadByte() == 2)
            {
                m_BigEndian = true;
            }
        }
    }
}
