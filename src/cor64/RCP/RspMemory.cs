using System.Runtime.InteropServices;
using System;
using System.IO;
using cor64.IO;

namespace cor64.RCP
{
    public class RspMemory : Stream
    {
        private readonly UnmanagedBuffer m_Buffer;
        private readonly IntPtr m_Ptr;

        public RspMemory(UnmanagedBuffer rspMemoryBuffer, bool isImem) {
            m_Buffer = rspMemoryBuffer;

            if (isImem) {
                m_Ptr = rspMemoryBuffer.GetPointer().Offset(0x1000);
            }
            else {
                m_Ptr = rspMemoryBuffer.GetPointer();
            }
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override long Length => m_Buffer.Size;

        public override long Position { get; set; }

        public override void Flush()
        {
            
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            Marshal.Copy(m_Ptr.Offset((int)Position), buffer, offset, count);
            return count;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return offset;
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Marshal.Copy(buffer, offset, m_Ptr.Offset((int)Position), count);
        }
    }
}