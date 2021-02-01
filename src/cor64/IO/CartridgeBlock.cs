using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace cor64.IO
{
    /* This block should be mapped to Domain 1, Address 2 region of memory */
    public class CartridgeBlock : BlockDevice
    {
        //private Stream m_SourceStream;
        private UnmanagedBuffer m_RomBuffer;
        private UnmanagedBuffer m_DummyRead = new UnmanagedBuffer(4);
        private int m_RealSize;

        //public CartridgeBlock(Stream streamSource)
        //{
        //    m_SourceStream = streamSource;

        //    m_SourceStream.Position = 0;
        //    byte[] buffer = new byte[m_RomBuffer]
        //}

        public CartridgeBlock(UnmanagedBuffer romBuffer, int size)
        {
            m_RomBuffer = romBuffer;
            m_RealSize = size;
        }

        public sealed override void Read(long position, byte[] buffer, int offset, int count)
        {
            //m_SourceStream.Position = position;

            //Array.Clear(buffer, offset, count);

            //         while (count > 0)
            //         {
            //             if (m_SourceStream.Position < Size)
            //             {
            //                 buffer[offset++] = (byte)m_SourceStream.ReadByte();
            //             }

            //	count--;
            //         }

            Marshal.Copy(IntPtr.Add(m_RomBuffer.GetPointer(), (int)position), buffer, offset, count);

            if (CoreConfig.Current.ByteSwap)
            {
                if (count == 1)
                {
                    Console.WriteLine("Warning: Cartridge byte read");
                }
            }
        }

        public sealed override void Write(long position, byte[] buffer, int offset, int count)
        {
            //m_SourceStream.Position = position;

            //         while (count > 0)
            //         {
            //             if (m_SourceStream.Position < Size)
            //             {
            //		m_SourceStream.WriteByte(buffer[offset++]);
            //             }

            //             count--;
            //         }
        }

        public override long Size => 0x0FC00000;

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                //m_SourceStream.Dispose();
            }

            base.Dispose(disposing);
        }

        public override IntPtr[] GetReadPointerMap()
        {
            var map = new IntPtr[Size / 4];

            for (int i = 0; i < map.Length; i++)
            {
                var pos = i * 4;

                if (pos < m_RealSize)
                {
                    map[i] = IntPtr.Add(m_RomBuffer.GetPointer(), pos);
                }
                else
                {
                    map[i] = m_DummyRead.GetPointer();
                }
            }

            return map;
        }
    }
}
