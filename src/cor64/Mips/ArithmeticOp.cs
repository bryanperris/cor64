using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.Mips
{
    public enum ArithmeticOp
    {
        UNK,
        AND,
        NAND,
        OR,
        XOR,
        NXOR,
        NOR,
        LSHIFT,
        RSHIFT,
        EQUAL,
        NOT_EQUAL,
        GREATER_THAN_OR_EQUAL,
        LESS_THAN_OR_EQUAL,
        GREATER_THAN,
        LESS_THAN,
        TRUE,
        FALSE,
        SIGNALING,
        ABSOLUTE,
        MOVE
    }
}
