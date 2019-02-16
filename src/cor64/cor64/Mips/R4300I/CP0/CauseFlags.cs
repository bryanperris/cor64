using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.Mips.R4300I.CP0
{
    [Flags]
    public enum CauseFlags : uint
    {
        None = 0,
        Interrupt0 = 0b10000000,
        Interrupt1 = 0b100000000,
        Interrupt2 = 0b1000000000,
        Interrupt3 = 0b10000000000,
        Interrupt4 = 0b100000000000,
        Interrupt5 = 0b1000000000000,
        Interrupt6 = 0b10000000000000,
        Interrupt7 = 0b100000000000000,
        IsBranchDelaySlot = 0b1000000000000000000000000000000
    }
}
