using cor64.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
        RI_BASE_REG - 0x04700000

        0x0470 0000 to 0x0470 0003  RI_MODE_REG
                 RI mode
           (RW): [1:0] operating mode
                 [2] stop T active
                 [3] stop R active
        0x0470 0004 to 0x0470 0007  RI_CONFIG_REG
                 RI config
           (RW): [5:0] current control input
                 [6] current control enable
        0x0470 0008 to 0x0470 000B  RI_CURRENT_LOAD_REG
                 RI current load
            (W): [] any write updates current control register
        0x0470 000C to 0x0470 000F  RI_SELECT_REG
                 RI select
           (RW): [2:0] receive select
                 [2:0] transmit select
        0x0470 0010 to 0x0470 0013  RI_REFRESH_REG or RI_COUNT_REG
                 RI refresh
           (RW): [7:0] clean refresh delay
                 [15:8] dirty refresh delay
                 [16] refresh bank
                 [17] refresh enable
                 [18] refresh optimize
        0x0470 0014 to 0x0470 0017  RI_LATENCY_REG
                 RI latency
           (RW): [3:0] DMA latency/overlap
        0x0470 0018 to 0x0470 001B  RI_RERROR_REG
                 RI error
            (R): [0] nack error
                 [1] ack error
        0x0470 001C to 0x0470 001F  RI_WERROR_REG
                 RI error
            (W): [] any write clears all error bits
        0x0470 0020 to 0x047F FFFF  Unused
*/

namespace cor64.RCP
{
    public class RdramInterface : PerpherialDevice
    {
        private MemMappedBuffer m_Mode = new MemMappedBuffer(4);
        private MemMappedBuffer m_Config = new MemMappedBuffer(4);
        private MemMappedBuffer m_CurrentLoad = new MemMappedBuffer(4, MemMappedBuffer.MemModel.SINGLE_WRITEONLY);
        private MemMappedBuffer m_Select = new MemMappedBuffer(4);
        private MemMappedBuffer m_Refresh = new MemMappedBuffer(4);
        private MemMappedBuffer m_Latency = new MemMappedBuffer(4);
        private MemMappedBuffer m_ReadError = new MemMappedBuffer(4, MemMappedBuffer.MemModel.SINGLE_READONLY);
        private MemMappedBuffer m_WriteError = new MemMappedBuffer(4, MemMappedBuffer.MemModel.SINGLE_WRITEONLY);

        public RdramInterface(N64MemoryController controller) : base(controller, 0x100000)
        {
            Map(m_Mode, m_Config, m_CurrentLoad, m_Select, m_Refresh, m_Latency, m_ReadError, m_WriteError);

            // Use this to force the IPL to skip RDRAM configruation
            //m_Select.ReadPtr.AsType_32Swp(1);
        }
    }
}
