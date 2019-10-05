using cor64.Mips.Analysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;

using IL = System.Reflection.Emit.OpCodes;

namespace cor64.Mips.R4300I.JitIL
{
    public class MipsILEmitter32 : MipsILEmitter
    {
        public override void EmitLoadLocals()
        {
            m_RegAllocationMap.LoadLocals32();
        }

        public override void EmitStoreLocals()
        {
            m_RegAllocationMap.StoreLocals32();
        }

        public override void Add32(DecodedInstruction inst)
        {
            LogIL("Opcode Add32");

            int destReg = 0;

            void EmitOperands()
            {
                EmitGpr_L(inst.Source);

                if (inst.IsImmediate())
                {
                    EmitImm32(inst);
                    destReg = inst.Target;
                }
                else
                {
                    EmitGpr_L(inst.Target);
                    destReg = inst.Destination;
                }
            }

            if (inst.IsUnsigned())
            {
                EmitOperands();
                MathAdd(false);
                EmitGpr_S(destReg);
            }
            else
            {
                TryCatchBlock(() =>
                {
                    EmitOperands();
                    MathAdd(true);
                    UnsignedConvert(CLRValueType.UINT_32);
                    EmitGpr_S(destReg);
                },

                () => CoreEmit_MipsException(ExceptionType.Overflow),

                typeof(OverflowException));
            }
        }

        public override void Add64(DecodedInstruction inst)
        {
            LogIL("Opcode Add64");

            CoreEmit_MipsException(ExceptionType.Reserved);
        }

        public override void Subtract32(DecodedInstruction inst)
        {
            LogIL("Opcode Subtract32");

            if (inst.IsUnsigned())
            {
                EmitGpr_L(inst.Source);
                EmitGpr_L(inst.Target);
                MathSub(false);
                EmitGpr_S(inst.Destination);
            }
            else
            {
                TryCatchBlock(() =>
                {
                    EmitGpr_L(inst.Source);
                    SignedConvert(CLRValueType.INT_32);
                    EmitGpr_L(inst.Target);
                    SignedConvert(CLRValueType.INT_32);
                    MathSub(true);
                    UnsignedConvert(CLRValueType.UINT_32);
                    EmitGpr_S(inst.Destination);
                },

                () =>
                {
                   CoreEmit_MipsException(ExceptionType.Overflow);
                },

                typeof(OverflowException));
            }
        }

        public override void Subtract64(DecodedInstruction inst)
        {
            LogIL("Opcode Subtract64");

            CoreEmit_MipsException(ExceptionType.Reserved);
        }

        public override void Shift32(DecodedInstruction inst)
        {
            LogIL("Opcode Condition");

            void PushShiftAmount()
            {
                /* Push the shift amount */
                if (inst.IsVariableShift())
                {
                    EmitGpr_L(inst.Source);
                    Constant32(0x3F, true);
                    Emit(IL.And);
                }
                else
                {
                    Constant32(inst.ShiftAmount);
                }
            }

            EmitGpr_L(inst.Target);

            /* The shift operation */
            if (inst.Op.ArithmeticType == ArithmeticOp.LSHIFT)
            {
                PushShiftAmount();
                Emit(IL.Shl);
                EmitGpr_S(inst.Destination);
            }
            else
            {
                if (!inst.IsUnsigned())
                    SignedConvert(CLRValueType.INT_32);

                PushShiftAmount();

                if (!inst.IsUnsigned())
                {
                    Emit(IL.Shr);
                }
                else
                {
                    Emit(IL.Shr_Un);
                }

                EmitGpr_S(inst.Destination);
            }
        }

        public override void Shift64(DecodedInstruction inst)
        {
            LogIL("Opcode Shift64");

            CoreEmit_MipsException(ExceptionType.Reserved);
        }

        public override void Divide32(DecodedInstruction inst)
        {
            LogIL("Opcode Divide32");

            var lblDivZero = DefineLabel();
            var lblEnd = DefineLabel();
            var lblVal = DefineLabel();
            var lblVal2 = DefineLabel();

            TryCatchBlock(() =>
            {
                if (inst.IsUnsigned())
                {
                    EmitGpr_L(inst.Target);
                    Constant32(0);
                    Emit(IL.Beq, lblDivZero);

                    EmitGpr_L(inst.Source);
                    EmitGpr_L(inst.Target);
                    Emit(IL.Div_Un);
                    EmitLo_S();

                    EmitGpr_L(inst.Source);
                    EmitGpr_L(inst.Target);
                    Emit(IL.Rem_Un);
                    EmitHi_S();
                    Emit(IL.Br, lblEnd);

                    MarkLabel(lblDivZero);

                    EmitGpr_L(inst.Source);
                    EmitHi_S();
                    Constant32(0xFFFFFFFFU, true);
                    EmitLo_S();

                    MarkLabel(lblEnd);
                }
                else
                {
                    EmitGpr_L(inst.Target);
                    Constant32(0);
                    Emit(IL.Beq, lblDivZero);

                    EmitGpr_L(inst.Source);
                    SignedConvert(CLRValueType.INT_32);
                    EmitGpr_L(inst.Target);
                    SignedConvert(CLRValueType.INT_32);
                    Emit(IL.Div);
                    UnsignedConvert(CLRValueType.UINT_32);
                    EmitLo_S();

                    EmitGpr_L(inst.Source);
                    SignedConvert(CLRValueType.INT_32);
                    EmitGpr_L(inst.Target);
                    SignedConvert(CLRValueType.INT_32);
                    Emit(IL.Rem);
                    UnsignedConvert(CLRValueType.UINT_32);
                    EmitHi_S();
                    Emit(IL.Br, lblEnd);

                    MarkLabel(lblDivZero);

                    EmitGpr_L(inst.Source);
                    EmitHi_S();

                    EmitGpr_L(inst.Source);
                    SignedConvert(CLRValueType.INT_32);
                    Constant32(0);

                    Emit(IL.Blt, lblVal);

                    Constant32(0xFFFFFFFFU, true);
                    Emit(IL.Br, lblVal2);

                    MarkLabel(lblVal);
                    Constant32(1U, true);

                    MarkLabel(lblVal2);
                    EmitLo_S();

                    MarkLabel(lblEnd);
                    Nop();
                }
            }, () =>

            {
                Constant32(0);
                EmitHi_S();
                EmitGpr_L(inst.Source);
                EmitLo_S();
            },

            typeof(ArithmeticException));
        }

        public override void Divide64(DecodedInstruction inst)
        {
            LogIL("Opcode Divide64");

            CoreEmit_MipsException(ExceptionType.Reserved);
        }

        public override void Multiply32(DecodedInstruction inst)
        {
            LogIL("Opcode Multiply32");

            if (inst.IsUnsigned())
            {
                EmitGpr_L(inst.Source);
                UnsignedConvert(CLRValueType.UINT_64);
                EmitGpr_L(inst.Target);
                UnsignedConvert(CLRValueType.UINT_64);
            }
            else
            {
                EmitGpr_L(inst.Source);
                SignedConvert(CLRValueType.INT_32);
                SignedConvert(CLRValueType.INT_64);
                UnsignedConvert(CLRValueType.UINT_64);
                EmitGpr_L(inst.Target);
                SignedConvert(CLRValueType.INT_32);
                SignedConvert(CLRValueType.INT_64);
                UnsignedConvert(CLRValueType.UINT_64);
            }

            Emit(IL.Mul);
            UnsignedConvert(CLRValueType.UINT_64);
            EmitDuplicate();

            /* Lo */
            UnsignedConvert(CLRValueType.UINT_32);
            EmitLo_S();

            /* Hi */
            Constant32(32, false);
            Emit(IL.Shr_Un);
            UnsignedConvert(CLRValueType.UINT_32);
            EmitHi_S();
        }

        public override void Multiply64(DecodedInstruction inst)
        {
            CoreEmit_MipsException(ExceptionType.Reserved);
        }

        public override void FloatLoad(DecodedInstruction inst)
        {
            throw new NotImplementedException();
        }

        public override void FloatStore(DecodedInstruction inst)
        {
            throw new NotImplementedException();
        }

        public override void Round(DecodedInstruction inst)
        {
            throw new NotImplementedException();
        }

        public override void Truncate(DecodedInstruction inst)
        {
            throw new NotImplementedException();
        }

        public override void Ceil(DecodedInstruction inst)
        {
            throw new NotImplementedException();
        }

        public override void Floor(DecodedInstruction inst)
        {
            throw new NotImplementedException();
        }

        public override void Convert(DecodedInstruction inst)
        {
            throw new NotImplementedException();
        }
    }
}
