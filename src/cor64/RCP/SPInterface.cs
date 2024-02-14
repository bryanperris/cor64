using System.Threading;
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

    // ultra notes
    #define SP_CLR_YIELD		SP_CLR_SIG0
    #define SP_SET_YIELD		SP_SET_SIG0
    #define SP_STATUS_YIELD		SP_STATUS_SIG0
    #define SP_CLR_YIELDED		SP_CLR_SIG1
    #define SP_SET_YIELDED		SP_SET_SIG1
    #define SP_STATUS_YIELDED	SP_STATUS_SIG1
    #define SP_CLR_TASKDONE		SP_CLR_SIG2
    #define SP_SET_TASKDONE		SP_SET_SIG2
    #define SP_STATUS_TASKDONE	SP_STATUS_SIG2
    #define	SP_CLR_RSPSIGNAL	SP_CLR_SIG3
    #define	SP_SET_RSPSIGNAL	SP_SET_SIG3
    #define	SP_STATUS_RSPSIGNAL	SP_STATUS_SIG3
    #define	SP_CLR_CPUSIGNAL	SP_CLR_SIG4
    #define	SP_SET_CPUSIGNAL	SP_SET_SIG4
    #define	SP_STATUS_CPUSIGNAL	SP_STATUS_SIG4
	 */
    public class SPInterface : N64MemoryDevice
    {
        private readonly static Logger Log = LogManager.GetCurrentClassLogger();
        private readonly MemMappedBuffer m_SPMemory = new MemMappedBuffer(0x2000);
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

        private readonly DmaEngine m_DmaEngine = new DmaEngine("SP");

        private readonly BitFiddler m_SizeFiddler = new BitFiddler();
        private const int SIZE_FIELD_LEN = 0;
        private const int SIZE_FIELD_COUNT = 1;
        private const int SIZE_FIELD_SKIP = 2;

        public SPStatusRegister Status { get; }

        public event Action<uint> PCSet;

        private readonly EventWaitHandle m_CpuRspWait = new EventWaitHandle(false, EventResetMode.ManualReset);

        public class MemExports {
        public MemExports(IntPtr rDRAMPtr, IntPtr dMEMPtr, IntPtr dramAddressPtr, IntPtr writeLenPtr, IntPtr fullPtr, IntPtr semaphorePtr, IntPtr bistPtr) 
        {
            this.RDRAMPtr = rDRAMPtr;
            this.DMEMPtr = dMEMPtr;
            this.DramAddressPtr = dramAddressPtr;
            this.WriteLenPtr = writeLenPtr;
            this.FullPtr = fullPtr;
            this.SemaphorePtr = semaphorePtr;
            this.BistPtr = bistPtr;
   
        }
            public IntPtr RDRAMPtr { get; }
            public IntPtr IMEMPtr { get; }
            public IntPtr DMEMPtr { get; }
            public IntPtr SpMemAddressPtr { get; }
            public IntPtr DramAddressPtr { get; }
            public IntPtr ReadLenPtr { get; }
            public IntPtr WriteLenPtr { get; }
            public IntPtr StatusPtr { get; }
            public IntPtr FullPtr { get; }
            public IntPtr BusyPtr { get; }
            public IntPtr SemaphorePtr { get; }
            public IntPtr PCPtr { get; }
            public IntPtr BistPtr { get; }

            public MemExports(SPInterface iface) {
                RDRAMPtr = iface.ParentController.RDRAM.GetRamPointer(0);
                DMEMPtr = iface.m_SPMemory.ReadPtr;
                IMEMPtr = iface.m_SPMemory.ReadPtr.Offset(0x1000);
                SpMemAddressPtr = iface.m_SpMemAddress.ReadPtr;
                DramAddressPtr = iface.m_DramAddress.ReadPtr;
                ReadLenPtr = iface.m_ReadLen.ReadPtr;
                WriteLenPtr = iface.m_WriteLen.ReadPtr;
                StatusPtr = iface.m_Status.ReadPtr;
                FullPtr = iface.m_Full.ReadPtr;
                BusyPtr = iface.m_Busy.ReadPtr;
                SemaphorePtr = iface.m_Semaphore.ReadPtr;
                PCPtr = iface.m_PC.ReadPtr;
                BistPtr = iface.m_Bist.ReadPtr;
            }
        }

        public SPInterface(N64MemoryController controller) : base (controller, 0x100000)
        {
            StaticMap(m_SPMemory);
            StaticMap(0x3E000);
            StaticMap(m_SpMemAddress, m_DramAddress, m_ReadLen, m_WriteLen, m_Status, m_Full, m_Busy, m_Semaphore);
            StaticMap(0x3FFE0);
            StaticMap(m_PC, m_Bist);

            Status = new SPStatusRegister(m_Status);

            m_PC.Write += () => {
                m_PC.RegisterValue &= 0xFFC;
                PCSet?.Invoke(m_PC.RegisterValue);
            };

            RestoreDmaHandlers();

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

            // m_Semaphore.Write += () => {
            //     m_Semaphore.RegisterValue = 0;
            //     m_CpuRspWait.Set();
            // };

            // m_Semaphore.Read += () => {
            //     m_Semaphore.RegisterValue = 0;
            //     m_CpuRspWait.WaitOne();
            //     m_CpuRspWait.Reset();
            // };

            m_Semaphore.Write += () => {
                m_Semaphore.RegisterValue = 0;
            };

            m_Semaphore.Read += () => {
                m_Semaphore.RegisterValue = 0;
            };


            m_SizeFiddler.DefineField(0, 12);
            m_SizeFiddler.DefineField(12, 8);
            m_SizeFiddler.DefineField(20, 12);

            m_DramAddress.Write += () => {
                if (m_DramAddress.RegisterValue == 0x000174C0 || m_DramAddress.RegisterValue == 0x0007C440) {
                    Console.WriteLine("yooooooooo");
                    throw new Exception();
                }
            };
        }

        public override void AttachDma() {
            m_DmaEngine.AttachMemory(ParentController.RDRAM, ParentController.SPRegs);
        }

        public void DmaReadOverride(Action callback) {
            m_ReadLen.ClearWriters();
            m_ReadLen.Write += callback;
        }

        public void DmaWriteOverride(Action callback) {
            m_WriteLen.ClearWriters();
            m_WriteLen.Write += callback;
        }

        public void RestoreDmaHandlers() {
            m_ReadLen.ClearWriters();
            m_WriteLen.ClearWriters();
            m_ReadLen.Write += ReadLengthWrite;
            m_WriteLen.Write += WriteLengthWrite;
        }

        public void RegWriteFromRsp(int select, uint value) {
            m_RegSelects[select].RegisterValue = value;
            m_RegSelects[select].WriteNotify();
        }

        public uint ReadRegForRsp(int select) {
            m_RegSelects[select].ReadNotify();
            return m_RegSelects[select].ReadonlyRegisterValue;
        }

        /// <summary>
        /// SP Memory -> RDRAM Memory
        /// </summary>
        private void WriteLengthWrite()
        {
            m_DramAddress.RegisterValue &= ~7U;
            m_SpMemAddress.RegisterValue &= ~3U;

            uint register = m_WriteLen.RegisterValue;
            uint len = (m_SizeFiddler.X(SIZE_FIELD_LEN, ref register) + 1) | 7;
            uint count = m_SizeFiddler.X(SIZE_FIELD_COUNT, ref register) + 1;
            uint skip = m_SizeFiddler.X(SIZE_FIELD_SKIP, ref register);

            // Log.Debug("SP DMA Write: {3:X8} Len={0}, Count={1}, Skip={2}", len, count, skip, len);

            m_DmaEngine.StartMonitoring(false, m_SpMemAddress.RegisterValue, m_DramAddress.RegisterValue, (int)len);

            // if (skip % 8 != 0) {
            //     skip = 0;
            // }

            // Force length alignment
            // size = (size + 7U) & ~7U;

            // Check for length overflowing
            // if (((m_SpMemAddress.RegisterValue & 0xFFF) + size) > 0x1000) {
            //     size = 0x1000 - (m_SpMemAddress.RegisterValue & 0xFFF);
            // }

            // skip += len;

            // do {
            //     uint i = 0;
            //     count--;
            //     do {
            //         uint srcOffset = (count * len + (m_SpMemAddress.RegisterValue + i)) & 0x00001FF8;
            //         uint dstOffset = (count * skip + (m_DramAddress.RegisterValue + i)) & 0x00FFFFF8;
            //         i += 8;
            //         if (dstOffset > 0x007FFFFFU) continue;
            //         m_DmaEngine.DirectCopy_RcpToDram((int)srcOffset, (int)dstOffset, 8);
            //     }
            //     while (i < len);
            // } while (count > 0);

            for (; count > 0; count--) {
                DestAddress = m_DramAddress.RegisterValue & 0x007FFFF8;
                SourceAddress = m_SpMemAddress.RegisterValue & 0x00001FF8;

                byte[] block = new byte[len];

                m_DmaEngine.ReadRcp((int)SourceAddress, block, 0, (int)len);
                m_DmaEngine.WriteDram((int)DestAddress, block, 0, (int)len);

                m_DramAddress.RegisterValue += len;
                m_SpMemAddress.RegisterValue += len;
            }

            ClearDmaBusy();

            m_DmaEngine.StopMonitoring();
        }

        /// <summary>
        /// RDRAM Memory -> SP Memory
        /// </summary>
        private void ReadLengthWrite()
        {
            m_DramAddress.RegisterValue &= ~7U;
            m_SpMemAddress.RegisterValue &= ~3U;

            uint len = m_ReadLen.RegisterValue;
            uint size = m_SizeFiddler.X(SIZE_FIELD_LEN, ref len) + 1;
            uint count = m_SizeFiddler.X(SIZE_FIELD_COUNT, ref len) + 1;
            uint skip = m_SizeFiddler.X(SIZE_FIELD_SKIP, ref len);

            m_DmaEngine.StartMonitoring(true, m_DramAddress.RegisterValue, m_SpMemAddress.RegisterValue, (int)len);


            // Log.Debug("SP DMA Read: {3:X8} Len={0}, Count={1}, Skip={2}", size, count, skip, len);

            if (size == 1) {
                size = 7;
            }

            if (skip % 8 != 0) {
                skip = 0;
            }

            // Force length alignment
            size = (size + 7U) & ~7U;

            // Check for length overflowing
            if (((m_SpMemAddress.RegisterValue & 0xFFF) + size) > 0x1000) {
                size = 0x1000 - (m_SpMemAddress.RegisterValue & 0xFFF);
            }

            for (; count > 0; count--) {
                SourceAddress = m_DramAddress.RegisterValue & 0x007FFFFC;
                DestAddress =  m_SpMemAddress.RegisterValue & 0x00001FFC;

                byte[] block = new byte[size];

                m_DmaEngine.ReadDram((int)SourceAddress, block, 0, block.Length);
                m_DmaEngine.WriteRcp((int)DestAddress, block, 0, block.Length);

                m_DramAddress.RegisterValue += size + skip;
                m_SpMemAddress.RegisterValue += size;
            }

            ClearDmaBusy();

            m_DmaEngine.StopMonitoring();
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

        public void SetPC(uint pc) => m_PC.RegisterValue = pc;

        public uint SPMemAddr => m_SpMemAddress.RegisterValue;

        public uint DramMemAddr => m_SpMemAddress.RegisterValue;

        public uint ReadLen => m_ReadLen.RegisterValue;

        public uint WriteLen => m_WriteLen.RegisterValue;

        public bool ReadSignal(int select) {
            int mask = 1 << select;
            return ((m_Status.ReadonlyRegisterValue >> 7) & mask) != 0;
        }

        public Stream CreateIMemorySream() {
            return new RspMemory(m_SPMemory.BufferA, true);
        }

        public Stream CreateDMemorySream() {
            return new RspMemory(m_SPMemory.BufferA, false);
        }

        public MemExports ExportPointers() => new MemExports(this);

        public override string Name => "SP Interface";
    }
}
