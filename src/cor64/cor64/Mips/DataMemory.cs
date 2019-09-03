using cor64.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static cor64.IO.StreamEx;

namespace cor64.Mips
{
    public class DataMemory : IDisposable
    {
        private StreamEx m_DataStream;
        private byte[] m_DataBuffer;
        private GCHandle m_BufferHandle;
        private IntPtr m_BufferPtr;
        public long LastAddress { get; private set; }

        public DataMemory(StreamEx stream)
        {
            m_DataStream = stream;
            m_DataBuffer = new byte[8];
            m_BufferHandle = GCHandle.Alloc(m_DataBuffer, GCHandleType.Pinned);
            m_BufferPtr = m_BufferHandle.AddrOfPinnedObject();
        }

        public StreamEx BaseStream => m_DataStream;

        public void ReadData(long address, int size, bool aligned)
        {
            // Clear buffer
            Data64 = 0;

            try
            {
                /* 32-bit Constraint */
                address = (uint)address;
                LastAddress = address;

                if (size > m_DataBuffer.Length)
                {
                    throw new ArgumentException();
                }

                m_DataStream.AlignmentMode = aligned;
                m_DataStream.Position = address;
                m_DataStream.Read(m_DataBuffer, 0, size);

                //Console.WriteLine("Read {0:X16} from {1:X8}", Data64, address);
            }
            catch (Exception e)
            {
                throw new EmuException("ReadData: Hit an exception for " + address.ToString("X8"), e);
            }
        }

        public void WriteData(long address, int size, bool aligned)
        {
            try
            {
                address = (uint)address;
                LastAddress = address;

                if (size > m_DataBuffer.Length)
                {
                    throw new ArgumentException();
                }

                //Console.WriteLine("Write {0:X16} to {1:X8}", Data64, address);

                m_DataStream.AlignmentMode = aligned;
                m_DataStream.Position = address;
                m_DataStream.Write(m_DataBuffer, 0, size);
            }
            catch (Exception e)
            {
                throw new EmuException("WriteData: Hit an exception for " + address.ToString("X8"), e);
            }
        }

        public byte Data8
        {
            get => m_BufferPtr.AsType_8();
            set => m_BufferPtr.AsType_8(value);
        }

        public ushort Data16
        {
            get => m_BufferPtr.AsType_16();
            set => m_BufferPtr.AsType_16(value);
        }

        public uint Data32
        {
            get => m_BufferPtr.AsType_32();
            set => m_BufferPtr.AsType_32(value);
        }

        public ulong Data64
        {
            get => m_BufferPtr.AsType_64();
            set => m_BufferPtr.AsType_64(value);
        }

        public ushort Data16Swp
        {
            get => m_BufferPtr.AsType_16Swp();
            set => m_BufferPtr.AsType_16Swp(value);
        }

        public uint Data32Swp
        {
            get => m_BufferPtr.AsType_32Swp();
            set => m_BufferPtr.AsType_32Swp(value);
        }

        public ulong Data64Swp
        {
            get => m_BufferPtr.AsType_64Swp();
            set => m_BufferPtr.AsType_64Swp(value);
        }

        public byte[] ReadBuffer()
        {
            byte[] buffer = new byte[m_DataBuffer.Length];
            Array.Copy(m_DataBuffer, buffer, buffer.Length);
            return buffer;
        }
        

        #region IDisposable Support
        private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    m_DataStream.Dispose();
                }

                m_BufferPtr = IntPtr.Zero;
                m_BufferHandle.Free();

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        // ~Memory() {
        //   // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
        //   Dispose(false);
        // }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(true);
            // TODO: uncomment the following line if the finalizer is overridden above.
            // GC.SuppressFinalize(this);
        }
        #endregion
    }
}
