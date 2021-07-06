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

        private readonly UnmanagedBuffer m_BufferA;
        private readonly UnmanagedBuffer m_BufferB;
        public event Action Write;
        public event Action Read;

        public MemMappedBuffer() : this(4) {
            
        }

        public MemMappedBuffer(int size, MemModel mode = MemModel.SINGLE_READ_WRITE)
        {
            Size = size;
            m_BufferA = new UnmanagedBuffer(size);
            m_BufferB = m_BufferA;
            Write = EmptyHandler;
            Read = EmptyHandler;

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
                        m_BufferB = new UnmanagedBuffer(size);
                        break;
                    }
            }
        }

        public MemMappedBuffer(int offset, int size, UnmanagedBuffer parent) {
            Size = size;
            m_BufferA = new UnmanagedBuffer(parent, offset, size);
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

        public virtual void ReadNotify() {
            Read.Invoke();
        }

        public IntPtr ReadPtr => m_BufferB.GetPointer();

        public IntPtr WritePtr => m_BufferA.GetPointer();

        public UnmanagedBuffer BufferA => m_BufferA;

        public String Tag { get; set; }

        public uint RegisterValue
        {
            #if LITTLE_ENDIAN
            get => WritePtr.AsType_32();

            set => WritePtr.AsType_32(value);
            #else
            get => WritePtr.AsType_32Swp();

            set => WritePtr.AsType_32Swp(value);
            #endif
        }

        public uint ReadonlyRegisterValue
        {
            #if LITTLE_ENDIAN
            get => ReadPtr.AsType_32();

            set => ReadPtr.AsType_32(value);
            #else
            get => ReadPtr.AsType_32Swp();

            set => ReadPtr.AsType_32Swp(value);
            #endif
        }

        public int Size { get; }

        public long AssignedAddress { get; set; }

        public bool CanRead { get; }

        public bool CanWrite { get; }
    }
}
