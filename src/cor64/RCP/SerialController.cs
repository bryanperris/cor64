using System;
using cor64.Mips;
using cor64.IO;
using NLog;
using cor64.Debugging;

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
        private readonly MemMappedBuffer m_PifRead64Reg = new MemMappedBuffer(4, MemMappedBuffer.MemModel.SINGLE_WRITEONLY);
        private readonly MemMappedBuffer m_PifWrite64Reg = new MemMappedBuffer(4, MemMappedBuffer.MemModel.SINGLE_WRITEONLY);
        private readonly MemMappedBuffer m_StatusReg = new MemMappedBuffer();

        private const uint SIZE = 64;
        private const uint ADDR = 0x1FC007C0;

        public event Action PifRamWrite;

        public SerialController(N64MemoryController controller) : base(controller, 0x100000)
        {
            Map(m_DramAddressReg, m_PifRead64Reg);
            Map(8);
            Map(m_PifWrite64Reg);
            Map(4);
            Map(m_StatusReg);

            m_DramAddressReg.Write += () => {
                m_DramAddressReg.RegisterValue &= 0x00FFFFFF;
                m_DramAddressReg.RegisterValue &= ~3U;
            };

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
            m_StatusReg.ReadonlyRegisterValue |= 0x1000;
        }

        private void StatusChanged() {
            m_RcpInterface.ClearInterrupt(MipsInterface.INT_SI);
            m_StatusReg.ReadonlyRegisterValue &= ~0x1000U;
        }

        private void PifDmaWrite() {
            // RDRAM <- PIF RAM
            SourceAddress = ADDR;
            DestAddress = m_DramAddressReg.RegisterValue;

            TransferBytes((int)SIZE);
            EmuDebugger.Current.ReportDmaFinish("PIF", false, SourceAddress, DestAddress, (int)SIZE);

            m_DramAddressReg.RegisterValue += SIZE;
            m_RcpInterface.SetInterrupt(MipsInterface.INT_SI, true);
            m_StatusReg.ReadonlyRegisterValue |= 0x1000;
        }

        private void PifDmaRead() {
            // RDRAM -> PIF RAM
            SourceAddress = m_DramAddressReg.RegisterValue;
            DestAddress = ADDR;

            TransferBytes((int)SIZE);
            EmuDebugger.Current.ReportDmaFinish("PIF", true, SourceAddress, DestAddress, (int)SIZE);

            m_DramAddressReg.RegisterValue += SIZE;
            m_RcpInterface.SetInterrupt(MipsInterface.INT_SI, true);
            m_StatusReg.ReadonlyRegisterValue |= 0x1000;

            PifRamWrite?.Invoke();
        }
    }
}