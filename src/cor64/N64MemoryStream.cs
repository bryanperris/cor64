using System;
using System.IO;
using System.Threading;
using cor64.IO;

namespace cor64
{
        public sealed class N64MemoryStream : Stream, RdramHidden
        {
            private readonly N64MemoryController m_Controller;

            public N64MemoryStream(N64MemoryController controller)
            {
                m_Controller = controller;
            }

            public override bool CanRead => true;

            public override bool CanSeek => true;

            public override bool CanWrite => true;

            public override long Length => 0xFFFFFFFFU;

            public override long Position { get; set; }

            public int HiddenLength => m_Controller.RDRAM.HiddenLength;

            public override void Flush()
            {
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                for (int i = 0; i < count; i++) {
                    buffer[offset + i] = m_Controller.U8(Position++);
                }

                return count;
            }

            public int ReadDirect(byte[] buffer, int offset, int count)
            {
                for (int i = 0; i < count; i++) {
                    buffer[offset + i] = m_Controller.DirectReadByte(Position++);
                }

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
                for (int i = 0; i < count; i++) {
                    m_Controller.U8(Position++, buffer[offset + i]);
                }
            }

            public void WriteDirect(byte[] buffer, int offset, int count) {
                for (int i = 0; i < count; i++) {
                    m_Controller.DirectWriteByte(Position++, buffer[offset + i]);
                }
            }

            public override int ReadByte() {
                return m_Controller.U8(Position++);
            }

            public override void WriteByte(byte value) {
               m_Controller.U8(Position++);
            }

            public void HiddenWrite(int address, byte value)
            {
                m_Controller.RDRAM.HiddenBitWrite(address, value);
            }

            public byte HiddenRead(int address)
            {
                return m_Controller.RDRAM.HiddenBitRead(address);
            }

            public void WriteEnter() => m_Controller.WriteLockEnter();
            public void WriteExit() => m_Controller.WriteLockExit();
        }
}
