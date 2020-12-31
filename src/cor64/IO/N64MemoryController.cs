using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using cor64.Debugging;
using cor64.Mips;
using cor64.PIF;
using cor64.RCP;
using NLog;
using static cor64.N64CTS;

namespace cor64.IO
{
    public class N64MemoryController
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private BlockDevice m_CartRom; // Domain 1, Address 2 region
        private readonly static MemoryDebugger s_MemDebugger = new MemoryDebugger();
        private readonly UnifiedMemModel<BlockDevice> m_MemModel = new UnifiedMemModel<BlockDevice>();
        private long m_CountWriters;
        private long m_CountReaders;
        private readonly FastMemMap m_FastMemMap = new FastMemMap();

        [ThreadStatic] private readonly static byte[] s_ReadSingle = { 0 };
        [ThreadStatic] private readonly static byte[] s_WriteSingle = { 0 };

        public N64MemoryController()
        {
            m_MemModel.Cart = new DummyMemory(0xFC00000, "Dummy cartridge");
        }

        public void Init()
        {
            m_MemModel.RDRAM = new Rdram();
            m_MemModel.RIRegs = new RdramInterface(this);
            m_MemModel.RDRAMRegs = new RdramRegisters(this);
            m_MemModel.PIF = new PIFMemory(this);
            m_MemModel.DiskDriveRegisters = new DummyMemory(0x1000000, "Cartridge Domain 2, Address 1");
            m_MemModel.Unused = new DummyMemory(0x700000, "Unused section after SP regs");
            m_MemModel.Init();

            m_FastMemMap.Init(m_MemModel);
        }

        public void Read(long address, byte[] buffer, int offset, int count)
        {
            //Interlocked.Increment(ref m_CountReaders);

            #if SAFE_MEMORY_ACCESS
            _ReadMemAligned((uint)address, buffer, offset, count);
            #else
            m_FastMemMap.Read((uint)address, buffer, offset, count);
            #endif

            //Interlocked.Decrement(ref m_CountReaders);
        }

        public void Write(long address, byte[] buffer, int offset, int count)
        {
            Interlocked.Increment(ref m_CountWriters);

            #if SAFE_MEMORY_ACCESS
            _WriteMemAligned((uint)address, buffer, offset, count);
            #else
            m_FastMemMap.Write((uint)address, buffer, offset, count);
            #endif

            Interlocked.Decrement(ref m_CountWriters);
        }

        public N64MemoryStream CreateMemoryStream()
        {
            var stream = new N64MemoryStream(this);
            return stream;
        }

        public void MountCartridge(Cartridge cartridge)
        {
            m_CartRom = cartridge.GetBlockDevice();
            m_MemModel.Cart = m_CartRom;
        }

        private void _ReadMemAligned(uint address, byte[] buffer, int offset, int count)
        {
            var blkDevice = m_MemModel.GetDevice(address);
            var blkOffset = m_MemModel.GetDeviceOffset(address);

            if (blkDevice == null)
            {
                throw new IOException(String.Format("Device block for read not found for {0:X4} ({1})", blkDevice, s_MemDebugger.GetMemName(address)));
            }

            blkDevice.BaseAddress = address;
            blkDevice.Read(blkOffset, buffer, offset, count);
        }

        private void _WriteMemAligned(uint address, byte[] buffer, int offset, int count)
        {
            var blkDevice = m_MemModel.GetDevice(address);
            var blkOffset = m_MemModel.GetDeviceOffset(address);

            if (blkDevice == null)
            {
                throw new IOException(String.Format("Device block for write not found for {0:X4} ({1})", blkDevice, s_MemDebugger.GetMemName(address)));
            }

            blkDevice.BaseAddress = address;
            blkDevice.Write(blkOffset, buffer, offset, count);
        }

        public int MemoryCopy(uint srcAddress, uint destAddress, int len) {
            int count = 0;

            byte[] buffer = new byte[4];

            for (int i = 0; i < (len / 4); i++)
            {
                var src = srcAddress + (i * 4);
                var dst = destAddress + (i * 4);

                Read(src, buffer, 0, 4);

                #if DEBUG_DMA_HEX
                Log.Debug("Memory Copy {0:X8}->{1:X8} [{2:X2}, {3:X2}, {4:X2}, {5:X2}]",
                           src, dst, buffer[0], buffer[1], buffer[2], buffer[3]);
                #endif

                Write(dst, buffer, 0, 4);
                count += 4;
            }

            return count;
        }

        public int MemoryCopyUnaligned(uint srcAddress, uint destAddress, int len) {
            int count = 0;

            byte[] buffer = new byte[1];

            for (int i = 0; i < len; i++)
            {
                var src = srcAddress + i;
                var dst = destAddress + i;

                Read(src, buffer, 0, 1);

                Write(dst, buffer, 0, 1);

                count++;
            }

            return count;
        }

        /* Used by DMA operations */
        public Task<int> MemoryCopyAsync(uint srcAddress, uint destAddress, int len)
        {
            return new Task<int>(() => MemoryCopy(srcAddress, destAddress, len));
        }

        public Rdram RDRAM => (Rdram)m_MemModel.RDRAM;

        public UnifiedMemModel<BlockDevice> Model => m_MemModel;

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
                // XXX: Lets not lock on readers, only lock on the writer
                // while (Interlocked.Read(ref m_Controller.m_CountWriters) > 0)
                // {
                //     Thread.Sleep(200);
                // }

                m_Controller.Read(Position, buffer, offset, count);
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
                while (Interlocked.Read(ref m_Controller.m_CountReaders) > 0)
                {
                    Thread.Sleep(200);
                }

                m_Controller.Write(Position, buffer, offset, count);
            }

            public override int ReadByte() {
                Read(s_ReadSingle, 0 , 1);
                Position++;
                return s_ReadSingle[0];
            }

            public override void WriteByte(byte value) {
               s_WriteSingle[0] = value;
               Position++;
               Write(s_WriteSingle, 0, 1);
            }

            public void HiddenWrite(int address, byte value)
            {
                m_Controller.RDRAM.HiddenBitWrite(address, value);
            }

            public byte HiddenRead(int address)
            {
                return m_Controller.RDRAM.HiddenBitRead(address);
            }
        }

        public sealed class MemoryCopyException : Exception
        {
            private int m_Count;
            
            internal MemoryCopyException(int count, Exception innerException) : base("DMA copy has thrown an exception!", innerException)
            {
                m_Count = count;
            }

            public int Count;
        }
    }
}
