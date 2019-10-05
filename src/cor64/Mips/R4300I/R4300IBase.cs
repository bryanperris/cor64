using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.Mips.R4300I
{
    public abstract class R4300IBase : BaseInterpreter, IMipsOpcodes
    {
        protected R4300IBase(BaseDisassembler disassembler) : base(disassembler)
        {
        }

        public abstract void Abs(DecodedInstruction inst);
        public abstract void Add(DecodedInstruction inst);
        public abstract void Add32(DecodedInstruction inst);
        public abstract void Add64(DecodedInstruction inst);
        public abstract void BitwiseLogic(DecodedInstruction inst);
        public abstract void Branch(DecodedInstruction inst);
        public abstract void Cache(DecodedInstruction inst);
        public abstract void Ceil(DecodedInstruction inst);
        public abstract void Condition(DecodedInstruction inst);
        public abstract void Convert(DecodedInstruction inst);
        public abstract void Divide(DecodedInstruction inst);
        public abstract void Divide32(DecodedInstruction inst);
        public abstract void Divide64(DecodedInstruction inst);
        public abstract void ExceptionReturn(DecodedInstruction inst);
        public abstract void FloatLoad(DecodedInstruction inst);
        public abstract void FloatStore(DecodedInstruction inst);
        public abstract void Floor(DecodedInstruction inst);
        public abstract void Jump(DecodedInstruction inst);
        public abstract void Load(DecodedInstruction inst);
        public abstract void Mov(DecodedInstruction inst);
        public abstract void Multiply(DecodedInstruction inst);
        public abstract void Multiply32(DecodedInstruction inst);
        public abstract void Multiply64(DecodedInstruction inst);
        public abstract void Neg(DecodedInstruction inst);
        public abstract void Round(DecodedInstruction inst);
        public abstract void SetOnLessThan(DecodedInstruction inst);
        public abstract void Shift32(DecodedInstruction inst);
        public abstract void Shift64(DecodedInstruction inst);
        public abstract void SqrRoot(DecodedInstruction inst);
        public abstract void Store(DecodedInstruction inst);
        public abstract void Subtract(DecodedInstruction inst);
        public abstract void Subtract32(DecodedInstruction inst);
        public abstract void Subtract64(DecodedInstruction inst);
        public abstract void Sync(DecodedInstruction inst);
        public abstract void TransferReg(DecodedInstruction inst);
        public abstract void Truncate(DecodedInstruction inst);
    }
}
