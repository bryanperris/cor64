using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.Mips
{
    [Flags]
    public enum ProcessorMode : byte
    {
        Invalid = 0,
        Kernel = 1,
        Supervisor = 0b10,
        User = 0b100,
        Runtime32 = 0b1000,
        Runtime64 = 0b10000
    }
}
