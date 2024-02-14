using System;
using cor64.Mips;
using cor64.IO;
using NLog;
using cor64.Debugging;
using cor64.PIF;

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
    public class SerialController : N64MemoryDevice{
        private MipsInterface m_RcpInterface;

        private readonly static Logger Log = LogManager.GetCurrentClassLogger();

        private readonly MemMappedBuffer m_DramAddressReg = new MemMappedBuffer();
        private readonly MemMappedBuffer m_PifRead64Reg = new MemMappedBuffer(4, MemMappedBuffer.MemModel.SINGLE_WRITEONLY);
        private readonly MemMappedBuffer m_PifWrite64Reg = new MemMappedBuffer(4, MemMappedBuffer.MemModel.SINGLE_WRITEONLY);
        private readonly MemMappedBuffer m_StatusReg = new MemMappedBuffer();

        private readonly DmaEngine m_DmaEngine = new DmaEngine("SI");
        private readonly PIFController m_PIF;

        private const int SIZE = 64;
        private const uint ADDR = 0x1FC007C0;
        private const int OFFSET = 0x7C0;

        public event Action ReadJoycons;

        public SerialController(N64MemoryController controller) : base(controller, 0x100000)
        {
            m_PIF = controller.PIF;

            StaticMap(m_DramAddressReg, m_PifRead64Reg);
            StaticMap(8);
            StaticMap(m_PifWrite64Reg);
            StaticMap(4);
            StaticMap(m_StatusReg);

            m_StatusReg.Write += StatusChanged;

            // PIF RAM -> DRAM
            m_PifRead64Reg.Write += PifDmaRead;

            // DRAM -> PIF RAM
            m_PifWrite64Reg.Write += PifDmaWrite;
        }

        public override string Name => "Serial Interface";

        public void AttachInterfaces(MipsInterface rcpInterface) {
            m_RcpInterface = rcpInterface;
        }

        public override void AttachDma() {
            m_DmaEngine.AttachMemory(ParentController.RDRAM, ParentController.PIF);
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
            // RDRAM -> PIF RAM

            uint dram = m_DramAddressReg.RegisterValue & 0x1FFFFFFF;

            // Log.Debug("WR64 = {0:X8}", m_PifWrite64Reg.RegisterValue);

            m_DmaEngine.StartMonitoring(true, dram, OFFSET, SIZE);

            m_DmaEngine.DirectCopy_DramToRcp((int)dram, OFFSET, SIZE);
            // m_PIF.ReadCommandByte();
            // PrintRam();

            m_DmaEngine.StopMonitoring();

            m_RcpInterface.SetInterrupt(MipsInterface.INT_SI, true);
            m_StatusReg.ReadonlyRegisterValue |= 0x1000;
        }

        private void PifDmaRead() {
            // PIF RAM -> RDRAM

            uint dram = m_DramAddressReg.RegisterValue & 0x1FFFFFFF;

            // The PIF command is read before DMA starts
            // VR4300 preps PIF RAM (command + argument)
            // PIF process's the request, the response is written back into PIF RAM before the DMA transfer
            m_PIF.ReadCommandByte();
            ReadJoycons?.Invoke();
            m_PIF.ProcessPifCommands();

            m_DmaEngine.StartMonitoring(false, OFFSET, dram, SIZE);

            m_DmaEngine.DirectCopy_RcpToDram(OFFSET, (int)dram, SIZE);

            m_DmaEngine.StopMonitoring();

            m_RcpInterface.SetInterrupt(MipsInterface.INT_SI, true);
            m_StatusReg.ReadonlyRegisterValue |= 0x1000;
        }

        private void PrintRam() {
            Console.WriteLine("\n------------------------------");
            var ram = m_PIF.ReadRam();
            for (int i = 0; i < 64; i++) {
                Console.Write(ram[i].ToString("X2"));
            }
            Console.WriteLine("\n------------------------------\n");
        }
    }
}