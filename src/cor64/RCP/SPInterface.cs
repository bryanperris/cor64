using System;
using System.IO;
using cor64.Debugging;
using cor64.IO;
using NLog;

namespace cor64.RCP
{
    /* 
        SP_BASE_REG - 0x04040000

        0x0400 0000 to 0x0400 0FFF  SP_DMEM read/write
        0x0400 1000 to 0x0400 1FFF  SP_IMEM read/write
        0x0400 2000 to 0x0403 FFFF  Unused
        0x0404 0000 to 0x0404 0003  SP_MEM_ADDR_REG
                 Master, SP memory address
           (RW): [11:0] DMEM/IMEM address
                 [12] 0=DMEM,1=IMEM
        0x0404 0004 to 0x0404 0007  SP_DRAM_ADDR_REG
                 Slave, SP DRAM DMA address
           (RW): [23:0] RDRAM address
        0x0404 0008 to 0x0404 000B  SP_RD_LEN_REG
                 SP read DMA length
           (RW): [11:0] length
                 [19:12] count
                 [31:20] skip                 
                      direction: I/DMEM <- RDRAM
        0x0404 000C to 0x0404 000F  SP_WR_LEN_REG
                 SP write DMA length
           (RW): [11:0] length
                 [19:12] count
                 [31:20] skip                 
                      direction: I/DMEM to RDRAM
        0x0404 0010 to 0x0404 0013  SP_STATUS_REG
                 SP status
            (W): [0]  clear halt          (R): [0]  halt
                 [1]  set halt                 [1]  broke
                 [2]  clear broke              [2]  dma busy
                 [3]  clear intr               [3]  dma full
                 [4]  set intr                 [4]  io full
                 [5]  clear sstep              [5]  single step
                 [6]  set sstep                [6]  interrupt on break
                 [7]  clear intr on break      [7]  signal 0 set
                 [8]  set intr on break        [8]  signal 1 set
                 [9]  clear signal 0           [9]  signal 2 set
                 [10] set signal 0             [10] signal 3 set
                 [11] clear signal 1           [11] signal 4 set
                 [12] set signal 1             [12] signal 5 set
                 [13] clear signal 2           [13] signal 6 set
                 [14] set signal 2             [14] signal 7 set
                 [15] clear signal 3
                 [16] set signal 3
                 [17] clear signal 4
                 [18] set signal 4
                 [19] clear signal 5
                 [20] set signal 5
                 [21] clear signal 6
                 [22] set signal 6
                 [23] clear signal 7
                 [24] set signal 7
        0x0404 0014 to 0x0404 0017  SP_DMA_FULL_REG
                 SP DMA full
            (R): [0] valid bit
                     dma full
        0x0404 0018 to 0x0404 001B  SP_DMA_BUSY_REG
                 SP DMA busy
            (R): [0] valid bit
                     dma busy
        0x0404 001C to 0x0404 001F  SP_SEMAPHORE_REG
                 SP semaphore
            (R): [0] semaphore flag (set on read)
            (W): [] clear semaphore flag
        0x0404 0020 to 0x0407 FFFF  Unused
        0x0408 0000 to 0x0408 0003  SP_PC_REG
                 SP PC
           (RW): [11:0] program counter
        0x0408 0004 to 0x0408 0007  SP_IBIST_REG
                 SP IMEM BIST REG
            (W): [0] BIST check           (R): [0] BIST check
                 [1] BIST go                   [1] BIST go
                 [2] BIST clear                [2] BIST done
                                               [6:3] BIST fail
        0x0408 0008 to 0x040F FFFF  Unused
	 */
    public class SPInterface : PerpherialDevice
    {
        private readonly static Logger Log = LogManager.GetCurrentClassLogger();
        private readonly MemMappedBuffer m_DMemory = new MemMappedBuffer(0x1000);
        private readonly MemMappedBuffer m_IMemory = new MemMappedBuffer(0x1000);
        private readonly MemMappedBuffer m_SpMemAddress = new MemMappedBuffer(4);
        private readonly MemMappedBuffer m_DramAddress = new MemMappedBuffer(4);
        private readonly MemMappedBuffer m_ReadLen = new MemMappedBuffer(4);
        private readonly MemMappedBuffer m_WriteLen = new MemMappedBuffer(4);
        private readonly MemMappedBuffer m_Status = new MemMappedBuffer(4, MemMappedBuffer.MemModel.DUAL_READ_WRITE);
        private readonly MemMappedBuffer m_Full = new MemMappedBuffer(4, MemMappedBuffer.MemModel.SINGLE_READONLY);
        private readonly MemMappedBuffer m_Busy = new MemMappedBuffer(4, MemMappedBuffer.MemModel.SINGLE_READONLY);
        private readonly MemMappedBuffer m_Semaphore = new MemMappedBuffer(4);
        private readonly MemMappedBuffer m_PC = new MemMappedBuffer(4);
        private readonly MemMappedBuffer m_Bist = new MemMappedBuffer(4);

        private readonly MemMappedBuffer[] m_RegSelects;

        private readonly BitFiddler m_SizeFiddler = new BitFiddler();
        private const int SIZE_FIELD_LEN = 0;
        private const int SIZE_FIELD_COUNT = 1;
        private const int SIZE_FIELD_SKIP = 2;

        public SPStatusRegister Status { get; }

        public event Action<uint> PCSet;

        private uint m_ActualSpAddress;

        public SPInterface(N64MemoryController controller) : base (controller, 0x100000)
        {
            Map(m_DMemory, m_IMemory);
            Map(0x3E000);
            Map(m_SpMemAddress, m_DramAddress, m_ReadLen, m_WriteLen, m_Status, m_Full, m_Busy, m_Semaphore);
            Map(0x3FFE0);
            Map(m_PC, m_Bist);

            Status = new SPStatusRegister(m_Status);

            m_PC.Write += () => {
                PCSet?.Invoke(m_PC.RegisterValue);
            };

            m_DramAddress.Write += () => {
                m_DramAddress.RegisterValue &= 0x00FFFFFF;
                m_DramAddress.RegisterValue &= ~7U;
            };

            m_SpMemAddress.Write += () => {
                m_SpMemAddress.RegisterValue &= 0x00001FFF;
                m_SpMemAddress.RegisterValue &= ~7U;
                m_ActualSpAddress = 0x04000000 | m_SpMemAddress.RegisterValue;
            };

            m_ReadLen.Write += ReadLengthWrite;
            m_WriteLen.Write += WriteLengthWrite;

            m_RegSelects = new MemMappedBuffer[] {
                m_SpMemAddress,   // $c0
                m_DramAddress,  // $c1
                m_ReadLen,      // $c2
                m_WriteLen,     // $c3
                m_Status,       // $c4
                m_Full,         // $c5
                m_Busy,         // $c6
                m_Semaphore     // $c7
            };

            m_Semaphore.Write += () => m_Semaphore.RegisterValue = 0;

            m_SizeFiddler.DefineField(0, 12);
            m_SizeFiddler.DefineField(12, 8);
            m_SizeFiddler.DefineField(20, 12);
        }

        public void RegWriteFromRsp(int select, uint value) {
            m_RegSelects[select].RegisterValue = value;
            m_RegSelects[select].WriteNotify();
        }

        public uint ReadRegForRsp(int select) {
            return m_RegSelects[select].ReadonlyRegisterValue;
        }

        /// <summary>
        /// SP Memory -> RDRAM Memory
        /// </summary>
        private void WriteLengthWrite()
        {
            SourceAddress = m_ActualSpAddress;
            DestAddress = m_DramAddress.RegisterValue;

            uint len = m_WriteLen.RegisterValue;
            uint size = m_SizeFiddler.X(SIZE_FIELD_LEN, ref len);
            uint count = m_SizeFiddler.X(SIZE_FIELD_COUNT, ref len) + 1;
            uint skip = m_SizeFiddler.X(SIZE_FIELD_SKIP, ref len);

            // Log.Debug("SP DMA Write: {3:X8} Len={0}, Count={1}, Skip={2}", size, count, skip, len);

            if (size < 1) {
                size = 7;
            }

            if (skip % 8 != 0) {
                skip = 0;
            }

            // Force length alignment
            size = (size + 7U) & ~7U;

            for (; count > 0; count--) {

                TransferBytes((int)size);

                Debugger.Current.ReportDmaFinish("SP", false, SourceAddress, DestAddress, (int)size);

                SourceAddress += size ;
                DestAddress += size + skip;
            }

            ClearDmaBusy();
        }

        /// <summary>
        /// RDRAM Memory -> SP Memory
        /// </summary>
        private void ReadLengthWrite()
        {
            SourceAddress = m_DramAddress.RegisterValue;
            DestAddress = m_ActualSpAddress;

            uint len = m_ReadLen.RegisterValue;
            uint size = m_SizeFiddler.X(SIZE_FIELD_LEN, ref len);
            uint count = m_SizeFiddler.X(SIZE_FIELD_COUNT, ref len) + 1;
            uint skip = m_SizeFiddler.X(SIZE_FIELD_SKIP, ref len);

            // Log.Debug("SP DMA Read: {3:X8} Len={0}, Count={1}, Skip={2}", size, count, skip, len);

            if (size < 1) {
                size = 7;
            }

            if (skip % 8 != 0) {
                skip = 0;
            }

            // Force length alignment
            size = (size + 7U) & ~7U;

            for (; count > 0; count--) {

                TransferBytes((int)size);

                Debugger.Current.ReportDmaFinish("SP", true, SourceAddress, DestAddress, (int)size);

                SourceAddress += size + skip;
                DestAddress += size;
            }

            ClearDmaBusy();
        }

        private void ClearDmaBusy() {
            m_Busy.ReadonlyRegisterValue = 0;
        }

        public void SetStatus(uint value)
        {
            m_Status.RegisterValue = value;
        }

        // TODO: Implement status reg for SP and map it map2 A and B

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {

            }

            base.Dispose(disposing);
        }

        public uint PC => m_PC.RegisterValue;

        public Stream CreateIMemorySream() {
            return new RspMemory(m_IMemory.BufferA);
        }

        public Stream CreateDMemorySream() {
            return new RspMemory(m_DMemory.BufferA);
        }
    }
}
