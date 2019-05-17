using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
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
        private bool m_CanRead;
        private bool m_CanWrite;
        public event Action MemWrite;
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
                        m_CanRead = true;
                        m_CanWrite = true;
                        break;
                    }
                case MemModel.SINGLE_READONLY:
                    {
                        m_CanRead = true;
                        break;
                    }
                case MemModel.SINGLE_WRITEONLY:
                    {
                        m_CanWrite = true;
                        break;
                    }
                case MemModel.DUAL_READ_WRITE:
                    {
                        m_CanRead = true;
                        m_CanWrite = true;
                        m_BufferB = new PinnedBuffer(size);
                        break;
                    }
            }
        }

        public void Read(byte[] buffer, int srcOffset, int dstOffset, int count)
        {
            if (m_CanRead)
            {
                Marshal.Copy(m_BufferA.GetPointer().Offset(srcOffset), buffer, dstOffset, count);
            }
        }

        public void Write(byte[] buffer, int srcOffset, int dstOffset, int len)
        {
            if (m_CanWrite)
            {
                Marshal.Copy(buffer, srcOffset, m_BufferA.GetPointer().Offset(dstOffset), len);
            }
        }

        public void OnMemWrite()
        {
            MemWrite?.Invoke();
        }

        public IntPtr ReadPtr => m_BufferB.GetPointer();

        public IntPtr WritePtr => m_BufferA.GetPointer();

        public int Size => m_Size;

        public long AssignedAddress { get; set; }
    }
}
