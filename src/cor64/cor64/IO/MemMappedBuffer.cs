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
        public event Action Write;

        public MemMappedBuffer(int size, MemModel mode = MemModel.SINGLE_READ_WRITE)
        {
            Size = size;
            m_BufferA = new PinnedBuffer(size);
            m_BufferB = m_BufferA;
            Write += EmptyHandler;

            switch (mode)
            {
                case MemModel.SINGLE_READ_WRITE:
                    {
                        CanRead = true;
                        CanWrite = true;
                        break;
                    }
                case MemModel.SINGLE_READONLY:
                    {
                        CanRead = true;
                        break;
                    }
                case MemModel.SINGLE_WRITEONLY:
                    {
                        CanWrite = true;
                        break;
                    }
                case MemModel.DUAL_READ_WRITE:
                    {
                        CanRead = true;
                        CanWrite = true;
                        m_BufferB = new PinnedBuffer(size);
                        break;
                    }
            }
        }

        private void EmptyHandler()
        {

        }

        public virtual void WriteNotify()
        {
            Write.Invoke();
        }

        public IntPtr ReadPtr => m_BufferB.GetPointer();

        public IntPtr WritePtr => m_BufferA.GetPointer();

        public int Size { get; }

        public long AssignedAddress { get; set; }

        public bool CanRead { get; }

        public bool CanWrite { get; }
    }
}
