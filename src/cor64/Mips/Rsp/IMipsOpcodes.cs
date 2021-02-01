using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.Mips.Rsp
{
    internal interface IMipsOpcodes
    {
        void BitwiseLogic(DecodedInstruction inst);

        void Add(DecodedInstruction inst);

        void Subtract(DecodedInstruction inst);

        void Shift(DecodedInstruction inst);

        void SetOnLessThan(DecodedInstruction inst);

        void TransferReg(DecodedInstruction inst);

        void Branch(DecodedInstruction inst);

        void Jump(DecodedInstruction inst);

        void Store(DecodedInstruction inst);

        void Load(DecodedInstruction inst);

        void Break(DecodedInstruction inst);

        void VectorUnitReserved(DecodedInstruction inst);

        void VectorLoad(DecodedInstruction inst);

        void VectorStore(DecodedInstruction inst);

        void VectorAdd(DecodedInstruction inst);

        void VectorSubtract(DecodedInstruction inst);

        void VectorMultiply(DecodedInstruction inst);
        
        void VectorAccumulatorRead(DecodedInstruction inst);

        void VectorBitwise(DecodedInstruction inst);

        void VectorReciprocal(DecodedInstruction inst);

        void VectorCompare(DecodedInstruction inst);

        void VectorClip(DecodedInstruction inst);
        
        void VectorMove(DecodedInstruction inst);
    }
}
