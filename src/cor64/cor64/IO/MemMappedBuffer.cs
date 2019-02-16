using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.IO
{
    public class MemMappedBuffer
    {
        public enum MemModel
        {
            SINGLE_READ_WRITE,  // 1 read-writable buffer is shared between the CPU and device
            SINGLE_READONLY,    // 1 read-only buffer is shared between the CPU and device
            SINGLE_WRITEONLY,   // 1 write-only buffer is shared between the CPU and device
            DUAL_READ_WRITE,    // 2 read-writables buffers are shared between the CPU and device: CPU writes are read into the devuce, device writes are readout to CPU
        }

        private PinnedBuffer m_BufferA;
        private PinnedBuffer m_BufferB;
        private bool m_CPUCanRead;
        private bool m_CPUCanWrite;
        public event Action CPUWrite;
        private int m_Size;

        public MemMappedBuffer(int size, MemModel mode = MemModel.SINGLE_READ_WRITE)
        {
            m_Size = size;
            m_BufferA = new PinnedBuffer(size);
            m_BufferB = m_BufferA;

            switch (mode)
            {
                case MemModel.SINGLE_READ_WRITE:
                    {
                        m_CPUCanRead = true;
                        m_CPUCanWrite = true;
                        break;
                    }
                case MemModel.SINGLE_READONLY:
                    {
                        m_CPUCanRead = true;
                        break;
                    }
                case MemModel.SINGLE_WRITEONLY:
                    {
                        m_CPUCanWrite = true;
                        break;
                    }
                case MemModel.DUAL_READ_WRITE:
                    {
                        m_CPUCanRead = true;
                        m_CPUCanWrite = true;
                        m_BufferB = new PinnedBuffer(size);
                        break;
                    }
            }
        }

        public byte ReadByte(int offset)
        {
            if (m_CPUCanRead)
                return m_BufferA[offset];
            else
                return 0;
        }

        public void WriteByte(int offset, byte value)
        {
            if (m_CPUCanWrite)
                m_BufferB[offset] = value;
        }

        public void NotifyCPUWrite()
        {
            CPUWrite?.Invoke();
        }

        public IntPtr ReadPtr => m_BufferB.GetPointer();

        public IntPtr WritePtr => m_BufferA.GetPointer();

        public int Size => m_Size;

        public uint TaggedAddress { get; set; }
    }
}
