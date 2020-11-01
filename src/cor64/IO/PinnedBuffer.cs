using System.Buffers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace cor64.IO
{
    // TODO: Implement stream access for this class

    public class PinnedBuffer : IDisposable
    {
        private byte[] m_Buffer;
        private IntPtr m_Ptr;
        private bool m_Disposed = false;
        private readonly Memory<byte> m_MemoryBlock;
        private readonly MemoryHandle m_PinHandle;
        private PinnedBuffer m_Parent;

        public PinnedBuffer(int size)
        {
            Size = size;
            m_Buffer = new byte[size];
            m_MemoryBlock = new Memory<byte>(m_Buffer, 0, m_Buffer.Length);
            m_PinHandle = m_MemoryBlock.Pin();

            unsafe {
                m_Ptr = (IntPtr)m_PinHandle.Pointer;
            }
        }

        public PinnedBuffer(PinnedBuffer parent, int offset, int size) {
            Size = size;
            m_Parent = parent;
            m_Ptr = parent.m_Ptr.Offset(offset);
        }

        public void Clear()
        {
            for (int i = 0; i < Size; i++) {
                Marshal.WriteByte(m_Ptr + i, 0);
            }
        }

        public void CopyTo(Memory<byte> destination) {
            m_MemoryBlock.CopyTo(destination);
        }

        public void CopyInto(byte[] buffer)
        {
            Marshal.Copy(buffer, 0, m_Ptr, buffer.Length);
        }

        public IntPtr GetPointer()
        {
            return m_Ptr;
        }

        public int Size { get; }

        public bool IsDisposed => m_Disposed || (m_Parent != null && m_Parent.m_Disposed);

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    m_Buffer = null;
                    m_Ptr = IntPtr.Zero;
                }

                m_PinHandle.Dispose();

                m_Disposed = true;
            }
        }

        ~PinnedBuffer()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
