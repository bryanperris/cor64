using System;
using cor64.Mips;
using cor64.IO;
using NLog;

/*
 0x0480 0000 to 0x048F FFFF  Serial interface (SI) registers:
 ------------------------------------------------------------
        SI_BASE_REG - 0x04800000

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
        0x0480 001C to 0x048F FFFF  Unused
*/

namespace cor64.RCP {
    public class SerialController : PerpherialDevice{
        private MipsInterface m_RcpInterface;

        private readonly static Logger Log = LogManager.GetCurrentClassLogger();

        private readonly MemMappedBuffer m_DramAddressReg = new MemMappedBuffer();
        private readonly MemMappedBuffer m_PifRead64Reg = new MemMappedBuffer();
        private readonly MemMappedBuffer m_PifWrite64Reg = new MemMappedBuffer();
        private readonly MemMappedBuffer m_StatusReg = new MemMappedBuffer();

        public SerialController(N64MemoryController controller) : base(controller, 0x100000)
        {
            Map(m_DramAddressReg, m_PifRead64Reg);
            Map(8);
            Map(m_PifWrite64Reg);
            Map(4);
            Map(m_StatusReg);

            m_StatusReg.Write += StatusChanged;

            // PIF RAM -> DRAM
            m_PifRead64Reg.Write += PifDmaRead;

            // DRAM -> PIF RAM
            m_PifWrite64Reg.Write += PifDmaWrite;
        }

        public void AttachInterfaces(MipsInterface rcpInterface) {
            m_RcpInterface = rcpInterface;
        }

        public void Init() {
           
        }

        public void SignalSiReady() {
            m_RcpInterface.SetInterrupt(MipsInterface.INT_SI, true);
        }

        private void StatusChanged() {
            m_RcpInterface.ClearInterrupt(MipsInterface.INT_SI);
        }

        private void PifDmaWrite() {
            Log.Debug("TODO: PIF DMA Write");
            ReportFinish();
        }

        private void PifDmaRead() {
            Log.Debug("TODO: PIF DMA Read");
            ReportFinish();
        }

        private void ReportFinish() {
            m_RcpInterface.SetInterrupt(MipsInterface.INT_SI, true);
        }
    }
}