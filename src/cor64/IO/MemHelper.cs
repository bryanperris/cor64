using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace cor64.IO
{
    public static class MemHelper
    {
        public static long VirtualToPhysical(long virtualAddress) {
            /* Kernel 32-bit memory addressing */
            byte index = (byte)((uint)virtualAddress >> 28);

            return index switch
            {
                0x0 or 0x1 or 0x2 or 0x3 or 0x4 or 0x5 or 0x6 or 0x7 => throw new ArgumentException(String.Format("This util doesn't support TLB mapped: {0:X8}", virtualAddress)),
                0x8 or 0x9 => virtualAddress - 0x80000000,
                0xA => virtualAddress & 0x1FFFFFFF,
                0xB => virtualAddress & 0x1FFFFFFF,
                0xC or 0xD or 0xE or 0xF => throw new ArgumentException(String.Format("This util doesn't support TLB mapped: {0:X8}", virtualAddress)),
                _ => throw new ArgumentException("Invalid virtual address provided"),
            };
        }

        public static bool TryVirtualToPhysical(long virtualAddress, out long physicalAddress) {
            try {
                physicalAddress = VirtualToPhysical(virtualAddress);
                return true;
            }
            catch {
                physicalAddress = 0;
                return false;
            }
        }
    }
}