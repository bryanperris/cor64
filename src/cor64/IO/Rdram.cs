using cor64.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace cor64.IO
{
    public class Rdram : N64MemoryDevice
    {
        private const int NORMAL_SIZE = 4 * 1024 * 1024;
        private const int MAXSIZE = 2 * NORMAL_SIZE;

        private readonly UnmanagedBuffer m_Ram = new(MAXSIZE);
        private readonly UnmanagedBuffer m_HiddenRam = new(NORMAL_SIZE);

        public Rdram(N64MemoryController controller) : base(controller, 0x3F00000) {

        }

        public IntPtr GetRamPointer(int offset)
        {
            return m_Ram.GetPointer().Offset(offset);
        }

        protected override N64AlignedPtr DynamicReadMap(int offset) {
            if (offset < MAXSIZE) {
                return N64AlignedPtr.FromPtr(m_Ram.GetPointer() + offset);
            }

            return N64AlignedPtr.DUMMY;
        }

        protected override N64AlignedPtr DynamicWriteMap(int offset) {
            if (offset < MAXSIZE) {
                return N64AlignedPtr.FromPtr(m_Ram.GetPointer() + offset);
            }

            return N64AlignedPtr.DUMMY;
        }

        public void Clear() {
            m_Ram.Clear();
            m_HiddenRam.Clear();
        }

        // The hidden bits (9th bit on RDRAM bus) is used by the RDP Zbuffer

        public void HiddenBitWrite(int offset, byte bit) {
            m_HiddenRam.GetPointer().Offset(offset).AsType_8(bit);
        }

        public byte HiddenBitRead(int offset) {
            return m_HiddenRam.GetPointer().Offset(offset).AsType_8();
        }

        public int HiddenLength => m_HiddenRam.Size;

        public override string Name => "RDRAM";
    }
}
