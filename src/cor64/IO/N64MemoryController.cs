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
        private bool m_Debug = false;
        private BlockDevice m_CartRom; // Domain 1, Address 2 region
        private MemoryDebugger m_MemDebugger = new MemoryDebugger();
        private UnifiedMemModel<BlockDevice> m_MemModel = new UnifiedMemModel<BlockDevice>();
        private long m_CountWriters;
        private long m_CountReaders;
        private FastMemMap m_FastMemMap = new FastMemMap();

        private Action<uint, byte[], int, int> m_Read;
        private Action<uint, byte[], int, int> m_Write;

        private const int DUMMY_SECTION_SIZE = 0x100000;

        public N64MemoryController()
        {
            Interface_MI = new MipsInterface(this);
            m_Read = m_FastMemMap.Read;
            m_Write = m_FastMemMap.Write;
        }

        public void UseSafeAccess()
        {
            m_Read = _ReadMemAligned;
            m_Write = _WriteMemAligned;
        }

        public void Init()
        {
            m_MemModel.RDRAM = new Rdram();
            m_MemModel.RDRAMRegs = new RdramRegisters(this);
            m_MemModel.SPRegs = new SignalProcessorMemory(this);
            m_MemModel.DPCmdRegs = new DummyMemory(DUMMY_SECTION_SIZE, "Display Command Interface"); ;
            m_MemModel.DpSpanRegs = new DummyMemory(DUMMY_SECTION_SIZE, "Display Span Interface"); ;
            m_MemModel.MIRegs = Interface_MI;
            m_MemModel.VIRegs = new Video(this, Interface_MI);
            m_MemModel.AIRegs = new DummyMemory(DUMMY_SECTION_SIZE, "Audio Interface");
            m_MemModel.PIRegs = new PIMemory(this);
            m_MemModel.RIRegs = new RdramInterface(this);
            m_MemModel.SIRegs = new SerialMemory(this);
            m_MemModel.Cart = m_CartRom;
            m_MemModel.PIF = new PIFMemory(this);
            m_MemModel.Init();

            m_FastMemMap.Init(m_MemModel);
        }


        internal void DebugMode()
        {
            m_Debug = true;
        }

        public void Read(long address, byte[] buffer, int offset, int count)
        {
            Interlocked.Increment(ref m_CountReaders);

            m_Read((uint)address, buffer, offset, count);

            Interlocked.Decrement(ref m_CountReaders);
        }

        public void Write(long address, byte[] buffer, int offset, int count)
        {
            Interlocked.Increment(ref m_CountWriters);

            m_Write((uint)address, buffer, offset, count);

            Interlocked.Decrement(ref m_CountWriters);
        }

        public Stream CreateMemoryStream()
        {
            var stream = new _InternalStream(this);
            return stream;
        }

        public void MountCartridge(Cartridge cartridge)
        {
            m_CartRom = cartridge.GetBlockDevice();
        }

        private void _ReadMemAligned(uint address, byte[] buffer, int offset, int count)
        {
            var blkDevice = m_MemModel.GetDevice(address);
            var blkOffset = m_MemModel.GetDeviceOffset(address);

            if (blkDevice == null)
            {
                throw new IOException(String.Format("Device block for read not found for {0:X4} ({1})", blkDevice, m_MemDebugger.GetMemName(address)));
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
                throw new IOException(String.Format("Device block for write not found for {0:X4} ({1})", blkDevice, m_MemDebugger.GetMemName(address)));
            }

            blkDevice.BaseAddress = address;
            blkDevice.Write(blkOffset, buffer, offset, count);
        }

        /* Used by DMA operations */
        public Task<int> MemoryCopy(uint srcAdress, uint destAddress, int len)
        {
            var copyTask = new Task<int>(() =>
            {
                int count = 0;

                byte[] buffer = new byte[4];

                for (int i = 0; i < len / 4; i++)
                {
                    Read(srcAdress + (i * 4), buffer, 0, 4);
                    Write(destAddress + +(i * 4), buffer, 0, 4);
                    count += 4;
                }

                return count;
            });

            /* For now, run on same thread */
            copyTask.RunSynchronously();

            return copyTask;
        }

        public MipsInterface Interface_MI { get; }

        public SignalProcessorMemory Interface_SP => (SignalProcessorMemory)m_MemModel.SPRegs;

        public Video Interface_VI => (Video)m_MemModel.VIRegs;

        public Rdram RDRAM => (Rdram)m_MemModel.RDRAM;

        private sealed class _InternalStream : Stream
        {
            private N64MemoryController m_Controller;

            public _InternalStream(N64MemoryController controller)
            {
                m_Controller = controller;
            }

            public override bool CanRead => true;

            public override bool CanSeek => true;

            public override bool CanWrite => true;

            public override long Length => 0xFFFFFFFFU;

            public override long Position { get; set; }

            public override void Flush()
            {
                throw new NotImplementedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                while (Interlocked.Read(ref m_Controller.m_CountWriters) > 0)
                {
                    Thread.Sleep(200);
                }

                m_Controller.Read(Position, buffer, offset, count);
                return count;
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                return offset;
            }

            public override void SetLength(long value)
            {
                throw new NotImplementedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                while (Interlocked.Read(ref m_Controller.m_CountReaders) > 0)
                {
                    Thread.Sleep(200);
                }

                m_Controller.Write(Position, buffer, offset, count);
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
