using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.Mips.R4300I
{
    public static class CTS
    {
        public const int CP0_REG_INDEX = 0;
        public const int CP0_REG_RANDOM = 1;
        public const int CP0_REG_ENTRY_LO_0 = 2;
        public const int CP0_REG_ENTRY_LO_1 = 3;
        public const int CP0_REG_CONTEXT = 4;
        public const int CP0_REG_PAGEMASK = 5;
        public const int CP0_REG_WIRED = 6;
        public const int CP0_REG_BADVADDR = 8;
        public const int CP0_REG_COUNT = 9;
        public const int CP0_REG_ENTRY_HI = 10;
        public const int CP0_REG_COMPARE = 11;
        public const int CP0_REG_SR = 12;
        public const int CP0_REG_CAUSE = 13;
        public const int CP0_REG_EPC = 14;
        public const int CP0_REG_PRID = 15;
        public const int CP0_REG_CONFIG = 16;
        public const int CP0_REG_LLADDR = 17;
        public const int CP0_REG_WATCH_LO = 18;
        public const int CP0_REG_WATCH_HI = 19;
        public const int CP0_REG_XCONTEXT = 20;
        public const int CP0_REG_ECC = 26;
        public const int CP0_REG_CACHE_ERR = 27;
        public const int CP0_REG_TAG_LO = 28;
        public const int CP0_REG_TAG_HI = 29;
        public const int CP0_REG_ERROR_EPC = 30;
    }
}
