using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.Mips
{
    [Flags]
    public enum ExecutionFlags : uint
    {
        None =       0b00000000,
        Immediate =  0b1,
        Unsigned =   0b10,
        Reserved32 = 0b100,
        Reserved64 = 0b1000,
        VarShift   = 0b10000,
        Shift32    = 0b100000,
        Data8      = 0b1000000,
        Data16     = 0b10000000,
        Data32     = 0b100000000,
        Data64     = 0b1000000000,
        Data128    = 0b10000000000,
        Likely     = 0b100000000000,
        Link       = 0b1000000000000,
        Register   = 0b10000000000000,
        Left       = 0b100000000000000,
        Right      = 0b1000000000000000,
        DataS      = 0b10000000000000000,
        DataD      = 0b100000000000000000,
        CondLT     = 0b1000000000000000000,
        CondGT     = 0b10000000000000000000,
        CondEq     = 0b100000000000000000000,
        CondUn     = 0b1000000000000000000000,
        CondNot    = 0b10000000000000000000000,
        CondOrd    = 0b100000000000000000000000,  // Indicates that unordered values throws an exception
        Carry      = 0b1000000000000000000000000
    }

    public static class ExecutionFlagsHelper
    {
        public static bool TestFlag(this ExecutionFlags flags, ExecutionFlags test)
        {
            return (flags & test) == test;
        }
    }
}
