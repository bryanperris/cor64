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

        public static readonly uint[] SWL_MASK = { 0x00000000, 0xFF000000, 0xFFFF0000, 0xFFFFFF00 };
        public static readonly uint[] SWR_MASK = { 0x00FFFFFF, 0x0000FFFF, 0x000000FF, 0x00000000 };
        public static readonly uint[] LWL_MASK = { 0x00000000, 0x000000FF, 0x0000FFFF, 0x00FFFFFF };
        public static readonly uint[] LWR_MASK = { 0xFFFFFF00, 0xFFFF0000, 0xFF000000, 0x0000000 };
        public static readonly ulong[] LDL_MASK = { 0, 0xFF, 0xFFFF, 0xFFFFFF, 0xFFFFFFFF, 0xFFFFFFFFFF, 0xFFFFFFFFFFFF, 0xFFFFFFFFFFFFFF };

        public static readonly int[] SWL_SHIFT = { 0, 8, 16, 24 };
        public static readonly int[] SWR_SHIFT = { 24, 16, 8, 0 };
        public static readonly int[] LWL_SHIFT = { 0, 8, 16, 24 };
        public static readonly int[] LWR_SHIFT = { 24, 16, 8, 0 };
        public static readonly int[] LDL_SHIFT = { 0, 8, 16, 24, 32, 40, 48, 56 };

        public static readonly ulong[] LDR_MASK =
        {
            0xFFFFFFFFFFFFFF00,
            0xFFFFFFFFFFFF0000,
            0xFFFFFFFFFF000000,
            0xFFFFFFFF00000000,
            0xFFFFFF0000000000,
            0xFFFF000000000000,
            0xFF00000000000000,
            0x0000000000000000,
        };

        public static readonly int[] LDR_SHIFT =
        {
            56,
            48,
            40,
            32,
            24,
            16,
            8,
            0
        };

        public static readonly ulong[] SDL_MASK =
        {
            0x0000000000000000,
            0xFF00000000000000,
            0xFFFF000000000000,
            0xFFFFFF0000000000,
            0xFFFFFFFF00000000,
            0xFFFFFFFFFF000000,
            0xFFFFFFFFFFFF0000,
            0xFFFFFFFFFFFFFF00
        };

        public static readonly int[] SDL_SHIFT =
        {
            0,
            8,
            16,
            24,
            32,
            40,
            48,
            56
        };

        public static readonly ulong[] SDR_MASK =
        {
            0x00FFFFFFFFFFFFFF,
            0x0000FFFFFFFFFFFF,
            0x000000FFFFFFFFFF,
            0x00000000FFFFFFFF,
            0x0000000000FFFFFF,
            0x000000000000FFFF,
            0x00000000000000FF,
            0x0000000000000000
        };

        public static readonly int[] SDR_SHIFT =
        {
            56,
            48,
            40,
            32,
            24,
            16,
            8,
            0
        };
    }
}
