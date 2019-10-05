using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64
{
    public static class N64CTS
    {
        public const uint ADDR_RDRAM =      0x00000000;
        public const uint ADDR_RDRAM_REGS = 0x03F00000;
        public const uint ADDR_SP_REGS    = 0x04000000;
        public const uint ADDR_DP_CMD_REGS = 0x041000000;
        public const uint ADDR_DP_SPAN_REGS =0x042000000;
        public const uint ADDR_MI_REGS = 0x04300000;
        public const uint ADDR_VI_REGS = 0x04400000;
        public const uint ADDR_AI_REGS = 0x04500000;
        public const uint ADDR_PI_REGS = 0x04600000;
        public const uint ADDR_RI_REGS = 0x04700000;
        public const uint ADDR_SI_REGS = 0x04800000;
        public const uint ADDR_CART_D2A1 = 0x05000000;
        public const uint ADDR_CART_D1A1 = 0x06000000;
        public const uint ADDR_CART_D2A2 = 0x08000000;
        public const uint ADDR_CART_D1A2 = 0x10000000;
        public const uint ADDR_PIF = 0x1FC00000;
        public const uint ADDR_CART_D1A3 = 0x1FD00000;
        public const uint ADDR_SYSAD = 0x80000000;
    }
}
