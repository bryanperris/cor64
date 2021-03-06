﻿using System;
using System.IO;

namespace cor64.IO
{
    public abstract class ByteSwapStream : Stream
    {
        private Stream m_BaseStream;

        protected ByteSwapStream(Stream baseStream)
        {
            if (baseStream == null)
                throw new ArgumentNullException(nameof(baseStream));

            m_BaseStream = baseStream;
        }

        public sealed override bool CanRead
        {
            get { return m_BaseStream.CanRead; }
        }

        public sealed override bool CanSeek
        {
            get { return m_BaseStream.CanSeek; }
        }

        public sealed override bool CanWrite
        {
            get { return m_BaseStream.CanWrite; }
        }

        public sealed override void Flush()
        {
            m_BaseStream.Flush();
        }

        public sealed override long Length
        {
            get { return m_BaseStream.Length; }
        }

        public sealed override long Position
        {
            get {
                return m_BaseStream.Position;
            }
            set {
                m_BaseStream.Position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            /* Copy a block of data that isn't swapped */
            Byte[] innerBuffer = new Byte[count];

            try {
                m_BaseStream.Read(innerBuffer, 0, count);
            }
            catch (Exception) {
                throw;
            }

            /* TODO: Some error checkign when larger byte swappers crashes on small buffers */

            /* Read into the new buffer swapped */
            for (int i = offset; i < count; i++) {
                buffer[i] = innerBuffer[(Int32)ComputeNextSwapPosition(i - offset)];
            }

            return count;
        }

        public sealed override long Seek(long offset, SeekOrigin origin)
        {
            return m_BaseStream.Seek(offset, origin);
        }

        public sealed override void SetLength(long value)
        {
            m_BaseStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Byte[] innerBuffer = new Byte[count];

            /* Write the data to inner buffer as unswapped */
            for (int i = offset; i < count; i++) {
                innerBuffer[(Int32)ComputeNextSwapPosition(i - offset)] = buffer[i];
            }

            try {
                m_BaseStream.Write(innerBuffer, 0, count);
            }
            catch { throw; }
        }

        public sealed override int ReadByte()
        {
            return m_BaseStream.ReadByte();
        }

        public sealed override void WriteByte(byte value)
        {
            m_BaseStream.WriteByte(value);
        }

        protected abstract Int64 ComputeNextSwapPosition(Int64 position);
    }

    public sealed class Swap16Stream : ByteSwapStream
    {
        public Swap16Stream(Stream baseStream) : base(baseStream)
        {
        }

        protected override long ComputeNextSwapPosition(long position)
        {
            return (position & unchecked(0x7FFFFFFFFFFFFFFEL)) + 1 - (position % 2);
        }
    }

    public sealed class Swap32Stream : ByteSwapStream
    {
        public Swap32Stream(Stream baseStream) : base(baseStream)
        {
        }

        protected override long ComputeNextSwapPosition(long position)
        {
            return (position & unchecked(0x7FFFFFFFFFFFFFFCL)) + 3 - (position % 4);
        }
    }

    public sealed class Swap64Stream : ByteSwapStream
    {
        public Swap64Stream(Stream baseStream) : base(baseStream)
        {
        }

        protected override long ComputeNextSwapPosition(long position)
        {
            return (position & unchecked(0x7FFFFFFFFFFFFFF8L)) + 7 - (position % 8);
        }
    }

    public sealed class SwapAutoStream : ByteSwapStream {
        private readonly Swap16Stream m_S16;
        private readonly Swap32Stream m_S32;
        private readonly Swap64Stream m_S64;
        private readonly Stream m_BaseStream;

        public SwapAutoStream(Stream baseStream) : base(baseStream)
        {
            m_BaseStream = baseStream;
            m_S16 = new Swap16Stream(baseStream);
            m_S32 = new Swap32Stream(baseStream);
            m_S64 = new Swap64Stream(baseStream);
        }

        public override int Read(byte[] buffer, int offset, int count) {
            Stream s;
            
            switch (count) {
                case 1: s = m_BaseStream; break;
                case 2: s = m_S16; break;
                case 4: s = m_S32; break;
                case 8: s = m_S64; break;
                default: return base.Read(buffer, offset, count);
            }

            return s.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count) {
            Stream s = null;

            switch (count) {
                case 2: s = m_S16; break;
                case 4: s = m_S32; break;
                case 8: s = m_S64; break;
                default: {
                    base.Write(buffer, offset, count);
                    return;
                }
            }

            s.Write(buffer, offset, count);
        }

        protected override long ComputeNextSwapPosition(long position)
        {
            return position;
        }
    }
}