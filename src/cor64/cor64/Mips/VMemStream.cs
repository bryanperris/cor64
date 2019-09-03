using cor64.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.Mips
{
    public abstract class VMemStream : StreamEx
    {
        private StreamEx m_BaseStream;

        protected VMemStream(StreamEx streamEx)
        {
            m_BaseStream = streamEx;
        }

        public override bool AlignmentMode {
            get => m_BaseStream.AlignmentMode;
            set => m_BaseStream.AlignmentMode = value;
        }

        protected abstract long TranslateAddress(long address);

        public override int Read(byte[] buffer, int offset, int count)
        {
            m_BaseStream.Position = TranslateAddress((uint)Position);
            return m_BaseStream.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            m_BaseStream.Position = TranslateAddress((uint)Position);
            m_BaseStream.Write(buffer, offset, count);
        }


        public override void Flush()
        {
            throw new NotImplementedException();
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override bool CanRead => true;

        public override bool CanSeek => true;

        public override bool CanWrite => true;

        public override long Length => 0xFFFFFFFF;

        public Stream BaseStream => m_BaseStream;
    }
}
