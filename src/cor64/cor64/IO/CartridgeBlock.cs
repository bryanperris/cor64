using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;

namespace cor64.IO
{
    /* This block should be mapped to Domain 1, Address 2 region of memory */
    public class CartridgeBlock : BlockDevice
    {
        private Stream m_SourceStream;

        public CartridgeBlock(Stream streamSource)
        {
            m_SourceStream = streamSource;
        }

		protected override void Read(long position, byte[] buffer, int offset, int count)
        {
			m_SourceStream.Position = position;

			Array.Clear(buffer, offset, count);

            while (count > 0)
            {
                if (m_SourceStream.Position < Size)
                {
                    buffer[offset++] = (byte)m_SourceStream.ReadByte();
                }

				count--;
            }
        }

		protected override void Write(long position, byte[] buffer, int offset, int count)
		{
			m_SourceStream.Position = position;

            while (count > 0)
            {
                if (m_SourceStream.Position < Size)
                {
					m_SourceStream.WriteByte(buffer[offset++]);
                }

                count--;
            }
		}

		public override long Size => 0x0FC00000;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                m_SourceStream.Dispose();
            }

            base.Dispose(disposing);
        }
	}
}
