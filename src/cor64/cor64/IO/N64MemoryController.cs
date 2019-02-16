using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
        private ChipInterface m_CPUInterface;
        private MemoryDebugger m_MemDebugger = new MemoryDebugger();
        private UnifiedMemModel<BlockDevice> m_MemModel = new UnifiedMemModel<BlockDevice>();

        public void Init()
        {
            m_MemModel.RDRAM = new Rdram();
            m_MemModel.RDRAMRegs = new DummyMemory(0x100000);
            m_MemModel.SPRegs = new SignalProcessorMemory(this);
            m_MemModel.DPCmdRegs = null;
            m_MemModel.DpSpanRegs = null;
            m_MemModel.MIRegs = m_CPUInterface;
            m_MemModel.VIRegs = new Video(this);
            m_MemModel.AIRegs = new DummyMemory(0x100000);
            m_MemModel.PIRegs = new PIMemory(this);
            m_MemModel.RIRegs = new DummyMemory(0x100000);
            m_MemModel.SIRegs = new SerialMemory(this);
            m_MemModel.Cart = m_CartRom;
            m_MemModel.PIF = new PIFMemory(this);
            m_MemModel.Init();
        }


        internal void DebugMode()
        {
            m_Debug = true;
        }

        public void Read(long address, bool aligned, byte[] buffer, int offset, int count)
        {
            if (aligned)
            {
                _ReadMemAligned((uint)address, buffer, offset, count);
            }
            else
            {
                _ReadMemUnaligned((uint)address, buffer, offset, count);
            }
        }

        public void Write(long address, bool aligned, byte[] buffer, int offset, int count)
        {
            if (aligned)
            {
                _WriteMemAligned((uint)address, buffer, offset, count);
            }
            else
            {
                _WriteMemUnaligned((uint)address, buffer, offset, count);
            }
        }

        public StreamEx CreateMemoryStream()
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

            blkDevice.SafeRead(blkOffset, buffer, offset, count);
        }

        private void _WriteMemAligned(uint address, byte[] buffer, int offset, int count)
        {
            var blkDevice = m_MemModel.GetDevice(address);
            var blkOffset = m_MemModel.GetDeviceOffset(address);

            if (blkDevice == null)
            {
                throw new IOException(String.Format("Device block for write not found for {0:X4} ({1})", blkDevice, m_MemDebugger.GetMemName(address)));
            }

            blkDevice.SafeWrite(blkOffset, buffer, offset, count);
        }

        private void _ReadMemUnaligned(uint address, byte[] buffer, int offset, int count)
        {
            int bytesLeft = count;
            uint cursor = address;
            int bufferCursor = offset;

            while (bytesLeft > 0)
            {
                var blkDevice = m_MemModel.GetDevice(cursor);
                var blkOffset = m_MemModel.GetDeviceOffset(cursor);

                if (blkDevice == null)
                {
                    throw new IOException(String.Format("Device block for unaligned read not found for {0:X4} ({1})", blkDevice, m_MemDebugger.GetMemName(address)));
                }

                blkDevice.SafeRead(blkOffset, buffer, bufferCursor, 1);

                bytesLeft--;
                cursor++;
                bufferCursor++;
            }
        }

        private void _WriteMemUnaligned(uint address, byte[] buffer, int offset, int count)
        {
            int bytesLeft = count;
            uint cursor = address;
            int bufferCursor = offset;

            while (bytesLeft > 0)
            {
                var blkDevice = m_MemModel.GetDevice(cursor);
                var blkOffset = m_MemModel.GetDeviceOffset(cursor);

                if (blkDevice == null)
                {
                    throw new IOException(String.Format("Device block for unaligned write not found for {0:X4} ({1})", blkDevice, m_MemDebugger.GetMemName(address)));
                }

                blkDevice.SafeWrite(blkOffset, buffer, bufferCursor, 1);

                bytesLeft--;
                cursor++;
                bufferCursor++;
            }
        }

        /* Used by DMA operations */
        public Task<int> MemoryCopy(uint srcAdress, uint destAddress, int len)
        {
            var copyTask = new Task<int>(() =>
            {
                int count = 0;
                byte[] buffer = new byte[len];

                try {
                    Read(srcAdress, false, buffer, 0, len);
                    Write(destAddress, false, buffer, 0, len);
                }
                catch (Exception e) {
                    throw new MemoryCopyException(count, e);
                }

                return count;
            });

            /* For now, run on same thread */
            copyTask.RunSynchronously();

            return copyTask;
        }

        public void HookCpu(ChipInterface iface)
        {
            m_CPUInterface = iface;
        }

        public ChipInterface Interface_MI => m_CPUInterface;

        public SignalProcessorMemory Interface_SP => (SignalProcessorMemory)m_MemModel.SPRegs;

        public Video Interface_VI => (Video)m_MemModel.VIRegs;

        public Rdram RDRAM => (Rdram)m_MemModel.RDRAM;

        private sealed class _InternalStream : StreamEx
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

            public override void Flush()
            {
                throw new NotImplementedException();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                m_Controller.Read(Position, AlignmentMode, buffer, offset, count);
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
                m_Controller.Write(Position, AlignmentMode, buffer, offset, count);
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
