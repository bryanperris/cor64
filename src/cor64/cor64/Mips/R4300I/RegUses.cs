using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.Mips.R4300I
{
    [Flags]
    public enum RegUses : byte
    {
        None = 0,
        GPRSource = 0b1,
        GPRTarget = 0b10,
        GPRDest =   0b100,
        FPRSource = 0b1000,
        FPRTarget = 0b10000,
        FPRDest   = 0b100000
    }
}
