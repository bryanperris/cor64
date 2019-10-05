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
    public class MipsILEmitter64 : MipsILEmitter
    {
        public MipsILEmitter64()
        {
            m_IsRuntime64 = true;
        }

        public override void EmitLoadLocals()
        {
            m_RegAllocationMap.LoadLocals64();
        }

        public override void EmitStoreLocals()
        {
            m_RegAllocationMap.StoreLocals64();
        }

        public override void Add32(DecodedInstruction inst)
        {
            LogIL("Opcode Add32");

            int destReg = 0;

            void EmitOperands()
            {
                EmitGpr32_L(inst.Source);

                if (inst.IsImmediate())
                {
                    EmitImm32(inst);
                    destReg = inst.Target;
                }
                else
                {
                    EmitGpr32_L(inst.Target);
                    destReg = inst.Destination;
                }
            }

            if (inst.IsUnsigned())
            {
                EmitOperands();
                MathAdd(false);
                EmitSignExtend64();
                EmitGpr_S(destReg);
            }
            else
            {
                TryCatchBlock(() =>
                {
                    EmitOperands();
                    MathAdd(true);
                    UnsignedConvert(CLRValueType.UINT_32);
                    EmitSignExtend64();
                    EmitGpr_S(destReg);
                },

                () => CoreEmit_MipsException(ExceptionType.Overflow),

                typeof(OverflowException));
            }
        }

        public override void Add64(DecodedInstruction inst)
        {
            LogIL("Opcode Add64");

            int destReg = 0;

            void EmitOperands()
            {
                EmitGpr_L(inst.Source);

                if (inst.IsImmediate())
                {
                    EmitImm64(inst);
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
                    UnsignedConvert(CLRValueType.UINT_64);
                    EmitGpr_S(destReg);
                },

                () => CoreEmit_MipsException(ExceptionType.Overflow),

                typeof(OverflowException));
            }
        }

        public override void Subtract32(DecodedInstruction inst)
        {
            LogIL("Opcode Subtract32");

            if (inst.IsUnsigned())
            {
                EmitGpr32_L(inst.Source);
                EmitGpr32_L(inst.Target);
                MathSub(false);
                EmitSignExtend64();
                EmitGpr_S(inst.Destination);
            }
            else
            {
                TryCatchBlock(() =>
                {
                    EmitGpr32_L(inst.Source);
                    SignedConvert(CLRValueType.INT_32);
                    EmitGpr32_L(inst.Target);
                    SignedConvert(CLRValueType.INT_32);
                    MathSub(true);
                    UnsignedConvert(CLRValueType.UINT_32);
                    EmitSignExtend64();
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
                    SignedConvert(CLRValueType.INT_64);
                    EmitGpr_L(inst.Target);
                    SignedConvert(CLRValueType.INT_64);
                    MathSub(true);
                    UnsignedConvert(CLRValueType.UINT_64);
                    EmitGpr_S(inst.Destination);
                },

                () =>
                {
                    CoreEmit_MipsException(ExceptionType.Overflow);
                },

                typeof(OverflowException));
            }
        }

        public override void Shift32(DecodedInstruction inst)
        {
            LogIL("Opcode Shift32");

            void PushShiftAmount()
            {
                /* Push the shift amount */
                if (inst.IsVariableShift())
                {
                    EmitGpr32_L(inst.Source);
                    Constant32(0x3F, true);
                    Emit(IL.And);
                }
                else
                {
                    Constant32(inst.ShiftAmount);
                }
            }

            EmitGpr32_L(inst.Target);

            /* The shift operation */
            if (inst.Op.ArithmeticType == ArithmeticOp.LSHIFT)
            {
                PushShiftAmount();
                Emit(IL.Shl);
                EmitSignExtend64();
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

                EmitSignExtend64();
                EmitGpr_S(inst.Destination);
            }
        }

        public override void Shift64(DecodedInstruction inst)
        {
            LogIL("Opcode Shift64");

            void PushShiftAmount()
            {
                /* Push the shift amount */
                if (inst.IsVariableShift())
                {
                    EmitGpr_L(inst.Source);
                    Constant64(0x3F, true);
                    Emit(IL.And);
                    UnsignedConvert(CLRValueType.UINT_32);
                }
                else
                {
                    if (inst.IsShift32())
                        Constant32(inst.ShiftAmount + 32);
                    else
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
                    SignedConvert(CLRValueType.INT_64);

                PushShiftAmount();

                if (!inst.IsUnsigned())
                {
                    Emit(IL.Shr);
                }
                else
                {
                    Emit(IL.Shr_Un);
                }

                UnsignedConvert(CLRValueType.UINT_64);
                EmitGpr_S(inst.Destination);
            }
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
                    EmitGpr32_L(inst.Target);
                    Constant32(0);
                    Emit(IL.Beq, lblDivZero);

                    EmitGpr32_L(inst.Source);
                    EmitGpr32_L(inst.Target);
                    Emit(IL.Div_Un);
                    EmitSignExtend64();
                    EmitLo_S();

                    EmitGpr32_L(inst.Source);
                    EmitGpr32_L(inst.Target);
                    Emit(IL.Rem_Un);
                    EmitSignExtend64();
                    EmitHi_S();
                    Emit(IL.Br, lblEnd);

                    MarkLabel(lblDivZero);

                    EmitGpr32_L(inst.Source);
                    EmitSignExtend64();
                    EmitHi_S();
                    Constant64(0xFFFFFFFFFFFFFFFFUL, true);
                    EmitLo_S();

                    MarkLabel(lblEnd);
                }
                else
                {
                    EmitGpr32_L(inst.Target);
                    Constant32(0);
                    Emit(IL.Beq, lblDivZero);

                    EmitGpr32_L(inst.Source);
                    SignedConvert(CLRValueType.INT_32);
                    EmitGpr32_L(inst.Target);
                    SignedConvert(CLRValueType.INT_32);
                    Emit(IL.Div);
                    EmitSignExtend64();
                    EmitLo_S();

                    EmitGpr32_L(inst.Source);
                    SignedConvert(CLRValueType.INT_32);
                    EmitGpr32_L(inst.Target);
                    SignedConvert(CLRValueType.INT_32);
                    Emit(IL.Rem);
                    EmitSignExtend64();
                    EmitHi_S();
                    Emit(IL.Br, lblEnd);

                    MarkLabel(lblDivZero);

                    EmitGpr32_L(inst.Source);
                    EmitSignExtend64();
                    EmitHi_S();

                    EmitGpr32_L(inst.Source);
                    SignedConvert(CLRValueType.INT_32);
                    Constant32(0);

                    Emit(IL.Blt, lblVal);

                    Constant64(0xFFFFFFFFFFFFFFFFUL, true);
                    Emit(IL.Br, lblVal2);

                    MarkLabel(lblVal);
                    Constant64(1UL, true);

                    MarkLabel(lblVal2);
                    EmitLo_S();

                    MarkLabel(lblEnd);
                    Nop();
                }
            }, () =>

            {
                Constant64(0);
                EmitHi_S();
                EmitGpr32_L(inst.Source);
                EmitSignExtend64();
                EmitLo_S();
            },

            typeof(ArithmeticException));
        }

        public override void Divide64(DecodedInstruction inst)
        {
            LogIL("Opcode Divide64");

            var lblDivZero = DefineLabel();
            var lblEnd = DefineLabel();
            var lblVal = DefineLabel();
            var lblVal2 = DefineLabel();

            TryCatchBlock(() =>
            {
                if (inst.IsUnsigned())
                {
                    EmitGpr_L(inst.Target);
                    Constant64(0);
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
                    Constant64(0xFFFFFFFFFFFFFFFFUL, true);
                    EmitLo_S();

                    MarkLabel(lblEnd);
                }
                else
                {
                    EmitGpr_L(inst.Target);
                    Constant64(0);
                    Emit(IL.Beq, lblDivZero);

                    EmitGpr_L(inst.Source);
                    SignedConvert(CLRValueType.INT_64);
                    EmitGpr_L(inst.Target);
                    SignedConvert(CLRValueType.INT_64);
                    Emit(IL.Div);
                    UnsignedConvert(CLRValueType.UINT_64);
                    EmitLo_S();

                    EmitGpr_L(inst.Source);
                    SignedConvert(CLRValueType.INT_64);
                    EmitGpr_L(inst.Target);
                    SignedConvert(CLRValueType.INT_64);
                    Emit(IL.Rem);
                    UnsignedConvert(CLRValueType.UINT_64);
                    EmitHi_S();
                    Emit(IL.Br, lblEnd);

                    MarkLabel(lblDivZero);

                    EmitGpr_L(inst.Source);
                    EmitHi_S();

                    EmitGpr_L(inst.Source);
                    SignedConvert(CLRValueType.INT_64);
                    Constant64(0);

                    Emit(IL.Blt, lblVal);

                    Constant64(0xFFFFFFFFFFFFFFFFUL, true);
                    Emit(IL.Br, lblVal2);

                    MarkLabel(lblVal);
                    Constant64(1UL, true);

                    MarkLabel(lblVal2);
                    EmitLo_S();

                    MarkLabel(lblEnd);
                }
            }, () =>

            {
                Constant64(0);
                EmitHi_S();
                EmitGpr_L(inst.Source);
                EmitLo_S();
                return;
            },

            typeof(ArithmeticException));
        }

        public override void Multiply32(DecodedInstruction inst)
        {
            LogIL("Opcode Multiply32");

            if (inst.IsUnsigned())
            {
                EmitGpr32_L(inst.Source);
                UnsignedConvert(CLRValueType.UINT_64);
                EmitGpr32_L(inst.Target);
                UnsignedConvert(CLRValueType.UINT_64);
            }
            else
            {
                EmitGpr32_L(inst.Source);
                SignedConvert(CLRValueType.INT_32);
                SignedConvert(CLRValueType.INT_64);
                UnsignedConvert(CLRValueType.UINT_64);
                EmitGpr32_L(inst.Target);
                SignedConvert(CLRValueType.INT_32);
                SignedConvert(CLRValueType.INT_64);
                UnsignedConvert(CLRValueType.UINT_64);
            }

            Emit(IL.Mul);
            UnsignedConvert(CLRValueType.UINT_64);
            EmitDuplicate();

            /* Lo */
            UnsignedConvert(CLRValueType.UINT_32);
            EmitSignExtend64();
            EmitLo_S();

            /* Hi */
            Constant32(32, false);
            Emit(IL.Shr_Un);
            UnsignedConvert(CLRValueType.UINT_32);
            EmitSignExtend64();
            EmitHi_S();
        }

        public override void Multiply64(DecodedInstruction inst)
        {
            LogIL("Opcode Multiply64");

            EmitGpr_L(inst.Source);
            EmitGpr_L(inst.Target);

            if (inst.IsUnsigned())
            {
                Emit(IL.Call, typeof(CoreUtils).GetMethod(nameof(CoreUtils.Multiply64_Unsigned)));
            }
            else
            {
                Emit(IL.Call, typeof(CoreUtils).GetMethod(nameof(CoreUtils.Multiply64_Signed)));
            }

            EmitDuplicate();
            Emit(IL.Ldfld, typeof(UInt128).GetField(nameof(UInt128.hi)));
            EmitHi_S();
            Emit(IL.Ldfld, typeof(UInt128).GetField(nameof(UInt128.lo)));
            EmitLo_S();
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
