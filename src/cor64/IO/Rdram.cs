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
        private const int NORMAL_SIZE = 4 * 1024 * 1024;
        private const int MAXSIZE = 2 * NORMAL_SIZE;

        private readonly PinnedBuffer m_Ram = new PinnedBuffer(MAXSIZE);
        private readonly PinnedBuffer m_HiddenRam = new PinnedBuffer(NORMAL_SIZE);
        private readonly PinnedBuffer m_DummyRead = new PinnedBuffer(4);
        private readonly PinnedBuffer m_DummyWrite = new PinnedBuffer(4);

        public override long Size => 0x03EFFFFF + 1;

        public IntPtr GetRamPointer(int offset)
        {
            return m_Ram.GetPointer().Offset(offset);
        }

        public sealed override void Read(long position, byte[] buffer, int offset, int count)
        {
            int memOffset = (int)position;
            var ptr = m_Ram.GetPointer().Offset(memOffset);

            // XXX: This check ensures RDRAM boundary correctly functions
            if (position >= MAXSIZE)
            {
                return;
            }

            Marshal.Copy(ptr, buffer, offset, count);
        }

        public sealed override void Write(long position, byte[] buffer, int offset, int count)
        {
            int memOffset = (int)position;
            var ptr = m_Ram.GetPointer().Offset(memOffset);

            // XXX: This check ensures RDRAM boundary correctly functions
            if (position >= MAXSIZE)
            {
                return;
            }

            Marshal.Copy(buffer, offset, ptr, count);
        }

        public override IntPtr[] GetReadPointerMap()
        {
            var map = new IntPtr[Size / 4];

            for (int i = 0; i < map.Length; i++)
            {
                int pos = i * 4;

                if (pos < MAXSIZE)
                {
                    map[i] = IntPtr.Add(m_Ram.GetPointer(), pos);
                }
                else
                {
                    map[i] = m_DummyRead.GetPointer();
                }
            }

            return map;
        }

        public override IntPtr[] GetWritePointerMap()
        {
            var map = new IntPtr[Size / 4];

            for (int i = 0; i < map.Length; i++)
            {
                int pos = i * 4;

                if (pos < MAXSIZE)
                {
                    map[i] = IntPtr.Add(m_Ram.GetPointer(), pos);
                }
                else
                {
                    map[i] = m_DummyWrite.GetPointer();
                }
            }

            return map;
        }

        // The hidden bits (9th bit on RDRAM bus) is used by the RDP Zbuffer

        public void HiddenBitWrite(int offset, byte bit) {
            m_HiddenRam.GetPointer().Offset(offset).AsType_8(bit);
        }

        public byte HiddenBitRead(int offset) {
            return m_HiddenRam.GetPointer().Offset(offset).AsType_8();
        }

        public int HiddenLength => m_HiddenRam.Size;
    }
}
