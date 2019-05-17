using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.IO
{
    public abstract class StreamEx : Stream
    {
        private long m_Position;

        public virtual bool AlignmentMode { get; set; }

        public override long Position
        {
            get => m_Position;
            set => m_Position = value;
        }

        public sealed class Wrapper : StreamEx
        {
            private Stream m_BaseStream;

            public Wrapper(Stream stream)
            {
                m_BaseStream = stream;
            }

            public override bool CanRead => m_BaseStream.CanRead;

            public override bool CanSeek => m_BaseStream.CanSeek;

            public override bool CanWrite => m_BaseStream.CanWrite;

            public override long Length => m_BaseStream.Length;

            public override void Flush()
            {
                m_BaseStream.Flush();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return m_BaseStream.Read(buffer, offset, count);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return m_BaseStream.Seek(offset, origin);
            }

            public override void SetLength(long value)
            {
                m_BaseStream.SetLength(value);
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                m_BaseStream.Write(buffer, offset, count);
            }

            public override long Position 
            { 
                get => m_BaseStream.Position; 
                set => m_BaseStream.Position = value; 
            }
        }
    }
}
