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

        private readonly PinnedBuffer m_BufferA;
        private readonly PinnedBuffer m_BufferB;
        public event Action Write;

        public MemMappedBuffer(int size, MemModel mode = MemModel.SINGLE_READ_WRITE)
        {
            Size = size;
            m_BufferA = new PinnedBuffer(size);
            m_BufferB = m_BufferA;
            Write = EmptyHandler;

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

        public MemMappedBuffer(int offset, int size, PinnedBuffer parent) {
            Size = size;
            m_BufferA = new PinnedBuffer(parent, offset, size);
            m_BufferB = m_BufferA;
            Write = EmptyHandler;
            CanRead = true;
            CanWrite = true;
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

        public PinnedBuffer BufferA => m_BufferA;

        public String Tag { get; set; }

        public uint RegisterValue
        {
            get
            {
                if (CoreConfig.Current.ByteSwap)
                {
                    return WritePtr.AsType_32Swp();
                }
                else
                {
                    return WritePtr.AsType_32();
                }
            }

            set
            {
                if (CoreConfig.Current.ByteSwap)
                {
                    WritePtr.AsType_32Swp(value);
                }
                else
                {
                    WritePtr.AsType_32(value);
                }
            }
        }

        public uint ReadonlyRegisterValue
        {
            get
            {
                if (CoreConfig.Current.ByteSwap)
                {
                    return ReadPtr.AsType_32Swp();
                }
                else
                {
                    return ReadPtr.AsType_32();
                }
            }

            set
            {
                if (CoreConfig.Current.ByteSwap)
                {
                    ReadPtr.AsType_32Swp(value);
                }
                else
                {
                    ReadPtr.AsType_32(value);
                }
            }
        }

        public int Size { get; }

        public long AssignedAddress { get; set; }

        public bool CanRead { get; }

        public bool CanWrite { get; }
    }
}
