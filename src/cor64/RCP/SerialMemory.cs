using cor64.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
 *         SI_BASE_REG - 0x04800000

        0x0480 0000 to 0x0480 0003  SI_DRAM_ADDR_REG
                 SI DRAM address
          (R/W): [23:0] starting RDRAM address
        0x0480 0004 to 0x0480 0007  SI_PIF_ADDR_RD64B_REG
                 SI address read 64B
            (W): [] any write causes a 64B DMA write
        0x0480 0008 to 0x0480 000B  Reserved
        0x0480 000C to 0x0480 000F  Reserved
        0x0480 0010 to 0x0480 0013  SI_PIF_ADDR_WR64B_REG
                 SI address write 64B
            (W): [] any write causes a 64B DMA read
        0x0480 0014 to 0x0480 0017  Reserved
        0x0480 0018 to 0x0480 001B  SI_STATUS_REG
                 SI status
            (W): [] any write clears interrupt
            (R): [0] DMA busy
                 [1] IO read busy
                 [2] reserved
                 [3] DMA error
                 [12] interrupt
        0x0480 001C to 0x048F FFFF  Unused*/

namespace cor64.RCP
{
    public class SerialMemory : PerpherialDevice
    {
        private MemMappedBuffer m_DramAddress = new MemMappedBuffer(4);
        private MemMappedBuffer m_PifReadAddress = new MemMappedBuffer(4);
        private MemMappedBuffer m_PifWriteAddress = new MemMappedBuffer(4);
        private MemMappedBuffer m_Status = new MemMappedBuffer(4, MemMappedBuffer.MemModel.DUAL_READ_WRITE);

        public SerialMemory(N64MemoryController controller) : base(controller, 0x100000)
        {
            Map(m_DramAddress, m_PifReadAddress);
            Map(8);
            Map(m_PifWriteAddress);
            Map(4);
            Map(m_Status);
        }
    }
}
