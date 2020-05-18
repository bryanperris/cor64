using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.Mips.R4300I
{
    internal interface IMipsOpcodes
    {
        void BitwiseLogic(DecodedInstruction inst);

        void Add32(DecodedInstruction inst);

        void Add64(DecodedInstruction inst);

        void Subtract32(DecodedInstruction inst);

        void Subtract64(DecodedInstruction inst);

        void Shift32(DecodedInstruction inst);

        void Shift64(DecodedInstruction inst);

        void Divide32(DecodedInstruction inst);

        void Divide64(DecodedInstruction inst);

        void Multiply32(DecodedInstruction inst);

        void Multiply64(DecodedInstruction inst);

        void SetOnLessThan(DecodedInstruction inst);

        void TransferReg(DecodedInstruction inst);

        void Branch(DecodedInstruction inst);

        void Jump(DecodedInstruction inst);

        void Store(DecodedInstruction inst);

        void Load(DecodedInstruction inst);

        void Cache(DecodedInstruction inst);

        void Sync(DecodedInstruction inst);

        void FloatLoad(DecodedInstruction inst);

        void FloatStore(DecodedInstruction inst);

        void Add(DecodedInstruction inst);

        void Subtract(DecodedInstruction inst);

        void Multiply(DecodedInstruction inst);

        void Divide(DecodedInstruction inst);

        void SqrRoot(DecodedInstruction inst);

        void Abs(DecodedInstruction inst);

        void Mov(DecodedInstruction inst);

        void Neg(DecodedInstruction inst);

        void Round(DecodedInstruction inst);

        void Truncate(DecodedInstruction inst);

        void Ceil(DecodedInstruction inst);

        void Floor(DecodedInstruction inst);

        void Convert(DecodedInstruction inst);

        void Condition(DecodedInstruction inst);

        void ExceptionReturn(DecodedInstruction inst);
    }
}
