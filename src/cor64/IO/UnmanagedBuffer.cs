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

    public class UnmanagedBuffer : IDisposable
    {
        private IntPtr m_Ptr;
        private bool m_Disposed = false;
        private readonly Memory<byte> m_MemoryBlock;
        private readonly UnmanagedBuffer m_Parent;

        public UnmanagedBuffer(int size)
        {
            Size = size;
            m_Ptr = Marshal.AllocHGlobal(size);
            Clear();
        }

        public UnmanagedBuffer(UnmanagedBuffer parent, int offset, int size) {
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

        public bool IsDisposed => m_Disposed || (m_Parent?.m_Disposed == true);

        protected virtual void Dispose(bool disposing)
        {
            if (!IsDisposed)
            {
                if (disposing)
                {
                    m_Ptr = IntPtr.Zero;
                }

                Marshal.FreeHGlobal(m_Ptr);

                m_Disposed = true;
            }
        }

        ~UnmanagedBuffer()
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
