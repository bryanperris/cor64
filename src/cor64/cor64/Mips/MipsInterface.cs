using cor64.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/* The Mips Interface (MI)
 * ---------------------------
 * This is the CPU chip interface used by the N64 hardware
 * - Interrupts: The main board triggers these interrupts from external events, only one is used as a timer and its managed by the processor itself
 * 
 *         MI_BASE_REG - 0x04300000

        0x0430 0000 to 0x0430 0003  MI_INIT_MODE_REG or MI_MODE_REG
                 MI init mode
            (W): [6:0] init length        (R): [6:0] init length
                 [7] clear init mode           [7] init mode
                 [8] set init mode             [8] ebus test mode
                 [9/10] clr/set ebus test mode [9] RDRAM reg mode
                 [11] clear DP interrupt
                 [12] clear RDRAM reg
                 [13] set RDRAM reg mode
        0x0430 0004 to 0x0430 0007  MI_VERSION_REG or MI_NOOP_REG
                 MI version
            (R): [7:0] io
                 [15:8] rac
                 [23:16] rdp
                 [31:24] rsp
        0x0430 0008 to 0x0430 000B  MI_INTR_REG
                 MI interrupt
            (R): [0] SP intr
                 [1] SI intr
                 [2] AI intr
                 [3] VI intr
                 [4] PI intr
                 [5] DP intr
        0x0430 000C to 0x0430 000F  MI_INTR_MASK_REG
                 MI interrupt mask
            (W): [0/1] clear/set SP mask  (R): [0] SP intr mask
                 [2/3] clear/set SI mask       [1] SI intr mask
                 [4/5] clear/set AI mask       [2] AI intr mask
                 [6/7] clear/set VI mask       [3] VI intr mask
                 [8/9] clear/set PI mask       [4] PI intr mask
                 [10/11] clear/set DP mask     [5] DP intr mask
        0x0430 0010 to 0x043F FFFF  Unused

 */

namespace cor64.Mips
{
    public class MipsInterface : PerpherialDevice
    {
        private MemMappedBuffer m_Mode = new MemMappedBuffer(4, MemMappedBuffer.MemModel.DUAL_READ_WRITE);
        private MemMappedBuffer m_Version = new MemMappedBuffer(4, MemMappedBuffer.MemModel.SINGLE_READONLY);
        private MemMappedBuffer m_Interrupt = new MemMappedBuffer(4, MemMappedBuffer.MemModel.SINGLE_READONLY);
        private MemMappedBuffer m_Mask = new MemMappedBuffer(4, MemMappedBuffer.MemModel.DUAL_READ_WRITE);

        public MipsInterface(N64MemoryController controller) : base(controller, 0x100000)
        {
            Map(m_Mode, m_Version, m_Interrupt, m_Mask);
        }

        public void SetVersion(uint value)
        {
            m_Version.ReadPtr.AsType_32Swp(value);
        }

        public bool Int0
        {
            get;
            set;
        }

        public bool Int1 { get; set; }

        public bool Int2 { get; set; }

        public bool Int3 { get; set; }

        public bool Int4 { get; set; }

        public bool Int5 { get; set; }
    }
}
