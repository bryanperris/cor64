using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace cor64.IO
{
    public class MemoryBlock : BlockDevice
    {
        MemoryStream m_Memory;
        private long m_Size;
        private string m_Name;
        
        public MemoryBlock(long size)
        {
            m_Memory = new MemoryStream();
            m_Size = size;
        }

        public override long Size => m_Size;


        protected override void Read(long position, byte[] buffer, int offset, int count)
        {
			m_Memory.Position = position;
			m_Memory.Read(buffer, offset, count);
        }

		protected override void Write(long position, byte[] buffer, int offset, int count)
        {
			m_Memory.Position = position;
            m_Memory.Write(buffer, offset, count);
        }
    }
}
