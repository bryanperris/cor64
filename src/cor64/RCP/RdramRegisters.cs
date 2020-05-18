using cor64.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
        RDRAM_BASE_REG - 0x03F00000

        0x03F0 0000 to 0x03F0 0003  RDRAM_CONFIG_REG or
                                    RDRAM_DEVICE_TYPE_REG
        0x03F0 0004 to 0x03F0 0007  RDRAM_DEVICE_ID_REG
        0x03F0 0008 to 0x03F0 000B  RDRAM_DELAY_REG
        0x03F0 000C to 0x03F0 000F  RDRAM_MODE_REG
        0x03F0 0010 to 0x03F0 0013  RDRAM_REF_INTERVAL_REG
        0x03F0 0014 to 0x03F0 0017  RDRAM_REF_ROW_REG
        0x03F0 0018 to 0x03F0 001B  RDRAM_RAS_INTERVAL_REG
        0x03F0 001C to 0x03F0 001F  RDRAM_MIN_INTERVAL_REG
        0x03F0 0020 to 0x03F0 0023  RDRAM_ADDR_SELECT_REG
        0x03F0 0024 to 0x03F0 0027  RDRAM_DEVICE_MANUF_REG
        0x03F0 0028 to 0x03FF FFFF  Unknown
*/

namespace cor64.RCP
{
    public class RdramRegisters : PerpherialDevice
    {
        private MemMappedBuffer m_Config = new MemMappedBuffer(4);
        private MemMappedBuffer m_DeviceId = new MemMappedBuffer(4);
        private MemMappedBuffer m_Delay = new MemMappedBuffer(4);
        private MemMappedBuffer m_Mode = new MemMappedBuffer(4);
        private MemMappedBuffer m_RefInterval = new MemMappedBuffer(4);
        private MemMappedBuffer m_RefRow = new MemMappedBuffer(4);
        private MemMappedBuffer m_RasInterval = new MemMappedBuffer(4);
        private MemMappedBuffer m_MinInterval = new MemMappedBuffer(4);
        private MemMappedBuffer m_AddressSelect = new MemMappedBuffer(4);
        private MemMappedBuffer m_Manuf = new MemMappedBuffer(4);


        public RdramRegisters(N64MemoryController controller) : base(controller, 0x100000)
        {
            Map(m_Config, m_DeviceId, m_Delay, m_Mode, m_RefInterval, m_RefRow, m_RasInterval, m_MinInterval, m_AddressSelect, m_Manuf);

            m_Config.RegisterValue = 0xB4190010;
            m_Delay.RegisterValue = 0x2B3B1A0B;
            m_RasInterval.RegisterValue = 0x101C0A04;
        }
    }
}
