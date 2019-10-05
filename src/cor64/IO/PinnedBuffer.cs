using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace cor64.IO
{
    public class PinnedBuffer : IDisposable
    {
        private byte[] m_Buffer;
        private GCHandle m_Handle;
        private IntPtr m_Ptr;
        private bool m_Disposed = false;

        public PinnedBuffer(int size)
        {
            m_Buffer = new byte[size];
            m_Handle = GCHandle.Alloc(m_Buffer, GCHandleType.Pinned);
            m_Ptr = m_Handle.AddrOfPinnedObject();
        }

        public void CopyInto(byte[] buffer)
        {
            Marshal.Copy(buffer, 0, m_Ptr, buffer.Length);
        }

        public byte this[int index]
        {
            get {
                return m_Buffer[index];
            }

            set {
                m_Buffer[index] = value;
            }
        }

        public void Clear()
        {
            Array.Clear(m_Buffer, 0, m_Buffer.Length);
        }

        public IntPtr GetPointer()
        {
            return m_Ptr;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!m_Disposed)
            {
                if (disposing)
                {
                    m_Buffer = null;
                    m_Ptr = IntPtr.Zero;
                }

                m_Handle.Free();

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
