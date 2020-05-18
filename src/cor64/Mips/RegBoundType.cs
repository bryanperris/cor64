using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.Mips
{
    public enum RegBoundType : ushort
    {
        None =   0,
        Gpr =    1,
        Lo =     0b100,
        Hi =     0b1000,
        Cp0 =    0b10000,
        Cp1 =    0b100000,
        Cp1Ctl = 0b1000000,
        Cp2    = 0b10000000,
        Cp2Ctl = 0b100000000
    }
}
