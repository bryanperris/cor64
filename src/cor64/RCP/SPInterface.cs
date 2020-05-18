using System;
using System.IO;
using cor64.Debugging;
using cor64.IO;
using NLog;

namespace cor64.RCP
{
    /* 0x00000 to 0x00FFF: Data Memory
	 * 0x01000 to 0x01FFF: Instruction Memory
	 * 0x02000 to 0x3FFFF: Unused (Zero)
	 * 0x40000 to 0x40003: SP_MEM_ADDR_REG
	 * 0x40004 to 0x40007: SP_DRAM_ADDR_REG
	 * 0x40008 to 0x4000B: SP_RD_LEN_REG
	 * 0x4000C to 0x4000F: SP_WR_LEN_REG
	 * 0x40010 to 0x40013: SP_STATUS_REG
	 * 0x40014 to 0x40017: SP_DMA_FULL_REG
	 * 0x40018 to 0x4001B: SP_DMA_BUSY_REG
	 * 0x4001C to 0x4001F: SP_SEMAPHORE_REG
	 * 0x40020 to 0x7FFFF: Unused (Zero)
	 * 0x80000 to 0x80003: SP_PC_REG
	 * 0x80004 to 0x80007: SP_IBIST_REG
	 * 0x80008 to 0xFFFFF: Unused (Zero)
	 */
    public class SPInterface : PerpherialDevice
    {
        private readonly static Logger Log = LogManager.GetCurrentClassLogger();
        private readonly MemMappedBuffer m_DMemory = new MemMappedBuffer(0x1000);
        private readonly MemMappedBuffer m_IMemory = new MemMappedBuffer(0x1000);
        private readonly MemMappedBuffer m_MemAddress = new MemMappedBuffer(4);
        private readonly MemMappedBuffer m_DramAddress = new MemMappedBuffer(4);
        private readonly MemMappedBuffer m_ReadLen = new MemMappedBuffer(4);
        private readonly MemMappedBuffer m_WriteLen = new MemMappedBuffer(4);
        private readonly MemMappedBuffer m_Status = new MemMappedBuffer(4, MemMappedBuffer.MemModel.DUAL_READ_WRITE);
        private readonly MemMappedBuffer m_Full = new MemMappedBuffer(4, MemMappedBuffer.MemModel.SINGLE_READONLY);
        private readonly MemMappedBuffer m_Busy = new MemMappedBuffer(4, MemMappedBuffer.MemModel.SINGLE_READONLY);
        private readonly MemMappedBuffer m_Semaphore = new MemMappedBuffer(4);
        private readonly MemMappedBuffer m_PC = new MemMappedBuffer(4);
        private readonly MemMappedBuffer m_Bist = new MemMappedBuffer(4);

        public SPStatusRegister Status { get; }

        public SPInterface(N64MemoryController controller) : base (controller, 0x100000)
        {
            Map(m_DMemory, m_IMemory);
            Map(0x3E000);
            Map(m_MemAddress, m_DramAddress, m_ReadLen, m_WriteLen, m_Status, m_Full, m_Busy, m_Semaphore);
            Map(0x3FFE0);
            Map(m_PC, m_Bist);
            
            Status = new SPStatusRegister(m_Status);

            m_ReadLen.Write += ReadLengthWrite;
            m_WriteLen.Write += WriteLengthWrite;

            m_Semaphore.RegisterValue = 1;
        }

        public static uint FixAddress(uint address) {
            return address & 0x0FFFFFFF;
        }

        /// <summary>
        /// SP Memory -> Data Memory
        /// </summary>
        private void WriteLengthWrite()
        {
            SourceAddress = FixAddress(m_MemAddress.RegisterValue);
            DestAddress = m_DramAddress.RegisterValue;
            int size = (int)m_WriteLen.RegisterValue + 1;

            Debugger.Current.Track_DmaOperation("SP", SourceAddress, DestAddress, size);

            TransferBytesAsync(size);
        }

        /// <summary>
        /// Data Memory -> SP Memory
        /// </summary>
        private void ReadLengthWrite()
        {
            SourceAddress = m_DramAddress.RegisterValue;
            DestAddress = FixAddress(m_MemAddress.RegisterValue);
            int size = (int)m_ReadLen.RegisterValue + 1;

            Debugger.Current.Track_DmaOperation("SP", SourceAddress, DestAddress, size);

            TransferBytesAsync(size);
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
