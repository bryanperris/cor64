using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using System.Reflection.Emit;
using IL = System.Reflection.Emit.OpCodes;
using System.Runtime.CompilerServices;
using System.Linq.Expressions;

namespace cor64.MSIL
{
    /// <summary>
    /// Generates a runtime method that operates on intrinsic data
    /// </summary>
    public class ILCodeEmitter
    {
        private ILGenerator m_CG;
        private Label m_ReturnLabel;
        private ILBasicBlock m_CurrentBlock;

        public ILCodeEmitter()
        {

        }

        public void Begin(ILBasicBlock block)
        {
            m_CG = block.GetCodeGenerator();
            m_CurrentBlock = block;
        }

        public void End()
        {
            /* Emit the return code */
            m_CG.Emit(IL.Ret);
        }

        public ILGenerator RawEmitter => m_CG;

        public void DefineLocalVariable(Type t)
        {
            m_CG.DeclareLocal(t);
        }

        public void OpBegin()
        {
            m_ReturnLabel = m_CG.DefineLabel();
        }

        public void OpEnd()
        {
            m_CG.MarkLabel(m_ReturnLabel);
        }

        public void GotoEnd()
        {
            m_CG.Emit(IL.Br, m_ReturnLabel);
        }

        public void BridgeMethodCall(String name, Action argPusher = null)
        {
            m_CG.Emit(IL.Ldarg_0);
            argPusher?.Invoke();
            m_CG.Emit(IL.Call, m_CurrentBlock.Bridge.GetType().GetMethod(name));
        }

        public void BridgeMethodCall(MethodInfo methodInfo, Action argPusher = null)
        {
            m_CG.Emit(IL.Ldarg_0);
            argPusher?.Invoke();
            m_CG.Emit(IL.Call, methodInfo);
        }

        public void BlockMethodcall(String name, Action argPusher = null)
        {
            m_CG.Emit(IL.Ldarg_1);
            argPusher?.Invoke();
            m_CG.Emit(IL.Call, m_CurrentBlock.GetType().GetMethod(name));
        }

        public void BridgeRef()
        {
            m_CG.Emit(IL.Ldarg_0);
        }

        public void BlockRef()
        {
            m_CG.Emit(IL.Ldarg_1);
        }

        public void ReturnOnFalse()
        {
            m_CG.Emit(IL.Brfalse, m_ReturnLabel);
        }

        public void ArrayElement(int index, CLRValueType elementValueType, CLRValueType outType)
        {
            Constant32(index);

            switch (elementValueType)
            {
                case CLRValueType.UINT_8:
                case CLRValueType.INT_8: m_CG.Emit(IL.Ldelem_I1); break;
                case CLRValueType.UINT_16:
                case CLRValueType.INT_16: m_CG.Emit(IL.Ldelem_I2); break;
                case CLRValueType.UINT_32:
                case CLRValueType.INT_32: m_CG.Emit(IL.Ldelem_I4); break;
                case CLRValueType.UINT_64:
                case CLRValueType.INT_64: m_CG.Emit(IL.Ldelem_I8); break;
                default: break;
            }

            Convert(elementValueType, outType);
        }

        public void Convert(CLRValueType i, CLRValueType o)
        {
            if (i == o) return;
            bool in_unsigned;
            bool out_unsigned;

            switch(i)
            {
                default: in_unsigned = false; break;
                case CLRValueType.UINT_8:
                case CLRValueType.UINT_16:
                case CLRValueType.UINT_32:
                case CLRValueType.UINT_64: in_unsigned = true; break;
            }

            switch (o)
            {
                default: out_unsigned = false; break;
                case CLRValueType.UINT_8:
                case CLRValueType.UINT_16:
                case CLRValueType.UINT_32:
                case CLRValueType.UINT_64: out_unsigned = true; break;
            }

            if (in_unsigned != out_unsigned && !in_unsigned)
            {
                /* Force possible unsigned value truncation */
                UnsignedConvert(i);

                if (!out_unsigned)
                    UnsignedConvert(o);
            }

            if (out_unsigned)
            {
                UnsignedConvert(o);
            }
            else
            {
                SignedConvert(o);
            }

        }

        public void Constant64(long value)
        {
            m_CG.Emit(IL.Ldc_I8, value);
        }

        public void Constant64(ulong value, bool isUnsigned)
        {
            m_CG.Emit(IL.Ldc_I8, (long)value);

            if (isUnsigned)
            {
                UnsignedConvert(CLRValueType.UINT_64);
            }
        }

        public void UnsignedConvert(CLRValueType t)
        {
            switch (t)
            {
                case CLRValueType.UINT_8:
                case CLRValueType.INT_8: m_CG.Emit(IL.Conv_U1); break;
                case CLRValueType.UINT_16:
                case CLRValueType.INT_16: m_CG.Emit(IL.Conv_U2); break;
                case CLRValueType.UINT_32:
                case CLRValueType.INT_32: m_CG.Emit(IL.Conv_U4); break;
                case CLRValueType.UINT_64:
                case CLRValueType.INT_64: m_CG.Emit(IL.Conv_U8); break;
                default: break;
            }
        }

        public void SignedConvert(CLRValueType t)
        {
            switch (t)
            {
                case CLRValueType.UINT_8:
                case CLRValueType.INT_8: m_CG.Emit(IL.Conv_I1); break;
                case CLRValueType.UINT_16:
                case CLRValueType.INT_16: m_CG.Emit(IL.Conv_I2); break;
                case CLRValueType.UINT_32:
                case CLRValueType.INT_32: m_CG.Emit(IL.Conv_I4); break;
                case CLRValueType.UINT_64:
                case CLRValueType.INT_64: m_CG.Emit(IL.Conv_I8); break;
                default: break;
            }
        }

        public void Pop()
        {
            m_CG.Emit(IL.Pop);
        }

        public void Nop()
        {
            m_CG.Emit(IL.Nop);
        }

        public void Constant32(uint value, bool isUnsigned)
        {
            switch (value)
            {
                case 0: m_CG.Emit(IL.Ldc_I4_0); break;
                case 1: m_CG.Emit(IL.Ldc_I4_1); break;
                case 2: m_CG.Emit(IL.Ldc_I4_2); break;
                case 3: m_CG.Emit(IL.Ldc_I4_3); break;
                case 4: m_CG.Emit(IL.Ldc_I4_4); break;
                case 5: m_CG.Emit(IL.Ldc_I4_5); break;
                case 6: m_CG.Emit(IL.Ldc_I4_6); break;
                case 7: m_CG.Emit(IL.Ldc_I4_7); break;
                case 8: m_CG.Emit(IL.Ldc_I4_8); break;
                default: m_CG.Emit(IL.Ldc_I4, value); break;
            }

            if (isUnsigned)
            {
                UnsignedConvert(CLRValueType.UINT_32);
            }
        }

        public void Constant32(int value)
        {
            switch (value)
            {
                case 0: m_CG.Emit(IL.Ldc_I4_0); break;
                case 1: m_CG.Emit(IL.Ldc_I4_1); break;
                case 2: m_CG.Emit(IL.Ldc_I4_2); break;
                case 3: m_CG.Emit(IL.Ldc_I4_3); break;
                case 4: m_CG.Emit(IL.Ldc_I4_4); break;
                case 5: m_CG.Emit(IL.Ldc_I4_5); break;
                case 6: m_CG.Emit(IL.Ldc_I4_6); break;
                case 7: m_CG.Emit(IL.Ldc_I4_7); break;
                case 8: m_CG.Emit(IL.Ldc_I4_8); break;
                default: m_CG.Emit(IL.Ldc_I4, value); break;
            }
        }

        public void StoreToLocalVariable(int index)
        {
            switch (index)
            {
                case 0: m_CG.Emit(IL.Stloc_0); break;
                case 1: m_CG.Emit(IL.Stloc_1); break;
                case 2: m_CG.Emit(IL.Stloc_2); break;
                case 3: m_CG.Emit(IL.Stloc_3); break;
                default: m_CG.Emit(IL.Stloc_S, index); break;
            }
        }

        public void LoadFromLocalVariable(int index)
        {
            switch (index)
            {
                case 0: m_CG.Emit(IL.Ldloc_0); break;
                case 1: m_CG.Emit(IL.Ldloc_1); break;
                case 2: m_CG.Emit(IL.Ldloc_2); break;
                case 3: m_CG.Emit(IL.Ldloc_3); break;
                default: m_CG.Emit(IL.Ldloc_S, index); break;
            }
        }

        public void MathAdd(bool checkOverflow)
        {
            if (checkOverflow)
            {
                m_CG.Emit(IL.Add_Ovf);
            }
            else
            {
                m_CG.Emit(IL.Add);
            }
        }

        public void MathSub(bool checkOverflow)
        {
            if (checkOverflow)
            {
                m_CG.Emit(IL.Sub_Ovf);
            }
            else
            {
                m_CG.Emit(IL.Sub);
            }
        }

        public void TryCatchBlock(Action t, Action c, Type catchType)
        {
            Label outsideBlock = m_CG.DefineLabel();

            m_CG.BeginExceptionBlock();

            t();

            m_CG.Emit(IL.Leave_S, outsideBlock);

            m_CG.BeginCatchBlock(catchType);
            m_CG.Emit(IL.Pop);
            c();
            m_CG.Emit(IL.Leave_S, m_ReturnLabel);
            m_CG.EndExceptionBlock();

            m_CG.MarkLabel(outsideBlock);
        }
    }
}
