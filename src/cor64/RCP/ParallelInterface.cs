using cor64.Debugging;
using cor64.IO;
using cor64.Mips;
using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.RCP
{
    /* Parallel Interface for Cartridge and DiskDrive */

    /*
     0x0460 0000 to 0x046F FFFF  Peripheral interface (PI) registers:
     ----------------------------------------------------------------
        PI_BASE_REG - 0x04600000

        0x0460 0000 to 0x0460 0003  PI_DRAM_ADDR_REG
                 PI DRAM address
           (RW): [23:0] starting RDRAM address
        0x0460 0004 to 0x0460 0007  PI_CART_ADDR_REG
                 PI pbus (cartridge) address
           (RW): [31:0] starting AD16 address
        0x0460 0008 to 0x0460 000B  PI_RD_LEN_REG
                 PI read length
           (RW): [23:0] read data length
        0x0460 000C to 0x0460 000F  PI_WR_LEN_REG
                 PI write length
           (RW): [23:0] write data length
        0x0460 0010 to 0x0460 0013 PI_STATUS_REG
                 PI status
            (R): [0] DMA busy             (W): [0] reset controller
                 [1] IO busy                       (and abort current op)
                 [2] error                     [1] clear intr
        0x0460 0014 to 0x0460 0017  PI_BSD_DOM1_LAT_REG or PI_DOMAIN1_REG
                 PI dom1 latency
           (RW): [7:0] domain 1 device latency
        0x0460 0018 to 0x0460 001B  PI_BSD_DOM1_PWD_REG
                 PI dom1 pulse width
           (RW): [7:0] domain 1 device R/W strobe pulse width
        0x0460 001C to 0x0460 001F  PI_BSD_DOM1_PGS_REG
                 PI dom1 page size
           (RW): [3:0] domain 1 device page size
        0x0460 0020 to 0x0460 0023  PI_BSD_DOM1_RLS_REG
                 PI dom1 release
           (RW): [1:0] domain 1 device R/W release duration
        0x0460 0024 to 0x0460 0027  PI_BSD_DOM2_LAT_REG or PI_DOMAIN2_REG
                 PI dom2 latency
           (RW): [7:0] domain 2 device latency
        0x0460 0028 to 0x0460 002B  PI_BSD_DOM2_PWD_REG
                 PI dom2 pulse width
           (RW): [7:0] domain 2 device R/W strobe pulse width
        0x0460 002C to 0x0460 002F  PI_BSD_DOM2_PGS_REG
                 PI dom2 page size
           (RW): [3:0] domain 2 device page size
        0x0460 0030 to 0x0460 0033  PI_BSD_DOM2_RLS_REG
                 PI dom2 release
           (RW): [1:0] domain 2 device R/W release duration
        0x0460 0034 to 0x046F FFFF  Unused
    */
    public class ParallelInterface : PerpherialDevice
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private MipsInterface m_RcpInterface;
        private MemMappedBuffer m_DramAddress = new MemMappedBuffer(4, MemMappedBuffer.MemModel.SINGLE_READ_WRITE);
        private MemMappedBuffer m_CartAddress = new MemMappedBuffer(4);
        private MemMappedBuffer m_ReadLen = new MemMappedBuffer(4);
        private MemMappedBuffer m_WriteLen = new MemMappedBuffer(4);
        private MemMappedBuffer m_Status = new MemMappedBuffer(4, MemMappedBuffer.MemModel.DUAL_READ_WRITE);
        private MemMappedBuffer m_Dom1Lat = new MemMappedBuffer(4);
        private MemMappedBuffer m_Dom1Pwd = new MemMappedBuffer(4);
        private MemMappedBuffer m_Dom1Pgs = new MemMappedBuffer(4);
        private MemMappedBuffer m_Dom1Rls = new MemMappedBuffer(4);
        private MemMappedBuffer m_Dom2Lat = new MemMappedBuffer(4);
        private MemMappedBuffer m_Dom2Pwd = new MemMappedBuffer(4);
        private MemMappedBuffer m_Dom2Pgs = new MemMappedBuffer(4);
        private MemMappedBuffer m_Dom2Rls = new MemMappedBuffer(4);

        public ParallelInterface(N64MemoryController controller) : base(controller, 0x100000)
        {
            m_ReadLen.Write += ReadLengthWrite;
            m_WriteLen.Write += WriteLengthWrite;
            m_Status.Write += StatusWrite;

            Map(
                m_DramAddress, m_CartAddress, m_ReadLen, m_WriteLen,
                m_Status,
                m_Dom1Lat, m_Dom1Pwd, m_Dom1Pgs, m_Dom1Rls,
                m_Dom2Lat, m_Dom2Pwd, m_Dom2Pgs, m_Dom2Rls);
        }

        public void AttachInterface(MipsInterface rcpInterface) {
            m_RcpInterface = rcpInterface;
        }

        private void StatusWrite() {

            // Reset controller
            if ((m_Status.RegisterValue & 1) != 0) {
                m_Status.ReadonlyRegisterValue = 0;
            }

            if ((m_Status.RegisterValue & 0b10) != 0) {
                // Clear PI interrupt bit write
                m_RcpInterface.ClearInterrupt(MipsInterface.INT_PI);
                m_Status.ReadonlyRegisterValue &= ~8U;
                // Log.Debug("Clear PI interrupt");
            }

            // Undocumented behavior
            if ((m_Status.RegisterValue & 8) != 0) {
                m_Status.ReadonlyRegisterValue |=8;
            }

            m_Status.RegisterValue = 0;
        }

        private void WriteLengthWrite()
        {
            m_DramAddress.RegisterValue &= 0x00FFFFFF;
            m_DramAddress.RegisterValue &= ~1U;
            m_CartAddress.RegisterValue &= ~1U;

            SourceAddress = m_CartAddress.RegisterValue;
            DestAddress = m_DramAddress.RegisterValue;
            int size = (int)m_WriteLen.RegisterValue + 1;

            // Force length alignment
            // size = (size + 7U) & ~7U;

            if ((size % 8) == 0) {
                var off = (int)(DestAddress % 8);

                if (off != 0) {
                    TransferBytesUnaligned(size - off);
                }
                else {
                    TransferBytes(size);
                }
            }
            else {
                TransferBytesUnaligned(size);
            }

            Debugger.Current.ReportDmaFinish("PI", false, SourceAddress, DestAddress, size);

            m_DramAddress.RegisterValue += (uint)size+1;
            m_CartAddress.RegisterValue += (uint)size+1;

            // Some strange address alignment/shift happens here

            m_RcpInterface.SetInterrupt(MipsInterface.INT_PI, true);
            m_Status.ReadonlyRegisterValue |= 8;
        }

        private void ReadLengthWrite()
        {
            m_DramAddress.RegisterValue &= 0x00FFFFFF;
            m_DramAddress.RegisterValue &= ~1U;
            m_CartAddress.RegisterValue &= ~1U;

            SourceAddress = m_DramAddress.RegisterValue;
            DestAddress = m_CartAddress.RegisterValue;
            int size = (int)m_WriteLen.RegisterValue + 1;

            // Force length alignment
            // size = (size + 7U) & ~7U;

            if ((size % 8) == 0) {
                var off = (int)(DestAddress % 8);

                if (off != 0) {
                    TransferBytesUnaligned(size - off);
                }
                else {
                    TransferBytes(size);
                }
            }
            else {
                TransferBytesUnaligned(size);
            }


            Debugger.Current.ReportDmaFinish("PI", true, SourceAddress, DestAddress, size);

            m_DramAddress.RegisterValue += (uint)size;
            m_CartAddress.RegisterValue += (uint)size;

            // This helps pass some DMA unalignement tests..
            m_DramAddress.RegisterValue++;
            m_CartAddress.RegisterValue++;

            m_DramAddress.WriteNotify();
            m_CartAddress.WriteNotify();

            // Some strange address alignment/shift happens here

            m_RcpInterface.SetInterrupt(MipsInterface.INT_PI, true);
            m_Status.ReadonlyRegisterValue |= 8;
        }
    }
}
