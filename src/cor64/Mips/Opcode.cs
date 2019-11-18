using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.Mips
{
    public struct Opcode
    {
        public readonly String Op;
        public readonly ExecutionFlags Flags;
        public readonly OperandType OperandFmt;
        public readonly OperationFamily Family;
        public readonly ArithmeticOp ArithmeticType;
        public readonly RegBoundType XferSource;
        public readonly RegBoundType XferTarget;
        public readonly int ID;

        public Opcode(string op, ExecutionFlags flags, OperandType operandFmt, OperationFamily family, ArithmeticOp arithmeticType, RegBoundType xferSource, RegBoundType xferTarget, int iD)
        {
            Op = op;
            Flags = flags;
            OperandFmt = operandFmt;
            Family = family;
            ArithmeticType = arithmeticType;
            XferSource = xferSource;
            XferTarget = xferTarget;
            ID = iD;
        }

        public override bool Equals(object obj)
        {
            return ((Opcode)obj).ID == ID;
        }

        public static bool operator ==(Opcode a, Opcode b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Opcode a, Opcode b)
        {
            return !(a.Equals(b));
        }

        public override int GetHashCode() {
            return this.ID;
        }
    }
}
