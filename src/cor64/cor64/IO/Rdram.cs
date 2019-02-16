using cor64.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace cor64.IO
{
    public class Rdram : BlockDevice
    {
        const int MAXSIZE = 9 * 1024 * 1024;

        /* The N64 can be expanded up to 9MB of memory (9-bit wide bus) */
        private PinnedBuffer m_Ram = new PinnedBuffer(MAXSIZE);

        public override long Size => 0x03EFFFFF + 1;

        public IntPtr GetRamPointer(int offset)
        {
            return m_Ram.GetPointer().Offset(offset);
        }

        protected override void Read(long position, byte[] buffer, int offset, int count)
        {
            int memOffset = (int)position;
            var ptr = m_Ram.GetPointer().Offset(memOffset);

#if SAFECHECKS
            if (memOffset + count >= MAXSIZE)
            {
                var diff = MAXSIZE - (memOffset + count);
                count -= diff;
            }

            if (count <= 0)
            {
                return;
            }
#endif

            Marshal.Copy(ptr, buffer, offset, count);
        }

        protected override void Write(long position, byte[] buffer, int offset, int count)
        {
            int memOffset = (int)position;
            var ptr = m_Ram.GetPointer().Offset(memOffset);

#if SAFECHECKS
            if (memOffset + count >= MAXSIZE)
            {
                var diff = MAXSIZE - (memOffset + count);
                count -= diff;
            }

            if (count <= 0)
            {
                return;
            }
#endif

            Marshal.Copy(buffer, offset, ptr, count);
        }
    }
}
