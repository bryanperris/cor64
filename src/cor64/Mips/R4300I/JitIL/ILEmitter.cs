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
using cor64.Mips;
using System.Security;
using System.Runtime.InteropServices;
using NLog;
using System.Diagnostics;

namespace cor64.Mips.R4300I.JitIL
{
    /// <summary>
    /// Generates a runtime method that operates on intrinsic data
    /// </summary>
    public class ILEmitter
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private ILGenerator m_Generator;
        protected Label m_LabelInstEnd;
        private StringBuilder m_BlockInstLog = new StringBuilder();

        public void SetCodeGenerator(ILGenerator generator)
        {
            m_Generator = generator;
            m_BlockInstLog.Clear();
        }

        public void DefineLocalVariable(Type t)
        {
            m_Generator.DeclareLocal(t);
        }

        public virtual void InstructionBegin(DecodedInstruction inst)
        {
            m_LabelInstEnd = DefineLabel();
        }

        public void InstructionEnd()
        {
            MarkLabel(m_LabelInstEnd);
        }

        public void EmitNop()
        {
            Emit(IL.Nop);
        }

        public void GotoEnd()
        {
            Emit(IL.Br, m_LabelInstEnd);
        }

        public void BridgeRef()
        {
            Emit(IL.Ldarg_0);
        }

        public void BlockRef()
        {
            Emit(IL.Ldarg_1);
        }

        public void ReturnOnFalse()
        {
            Emit(IL.Brfalse, m_LabelInstEnd);
        }

        public void ArrayElement(int index, CLRValueType elementValueType, CLRValueType outType)
        {
            Constant32(index);

            switch (elementValueType)
            {
                case CLRValueType.UINT_8:
                case CLRValueType.INT_8: Emit(IL.Ldelem_I1); break;
                case CLRValueType.UINT_16:
                case CLRValueType.INT_16: Emit(IL.Ldelem_I2); break;
                case CLRValueType.UINT_32:
                case CLRValueType.INT_32: Emit(IL.Ldelem_I4); break;
                case CLRValueType.UINT_64:
                case CLRValueType.INT_64: Emit(IL.Ldelem_I8); break;
                default: break;
            }

            Convert(elementValueType, outType);
        }

        public void Convert(CLRValueType i, CLRValueType o)
        {
            if (i == o) return;
            bool in_unsigned;
            bool out_unsigned;

            switch (i)
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
            Emit(IL.Ldc_I8, value);
        }

        public void Constant64(ulong value, bool isUnsigned)
        {
            Emit(IL.Ldc_I8, (long)value);

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
                case CLRValueType.INT_8: Emit(IL.Conv_U1); break;
                case CLRValueType.UINT_16:
                case CLRValueType.INT_16: Emit(IL.Conv_U2); break;
                case CLRValueType.UINT_32:
                case CLRValueType.INT_32: Emit(IL.Conv_U4); break;
                case CLRValueType.UINT_64:
                case CLRValueType.INT_64: Emit(IL.Conv_U8); break;
                default: break;
            }
        }

        public void SignedConvert(CLRValueType t)
        {
            switch (t)
            {
                case CLRValueType.UINT_8:
                case CLRValueType.INT_8: Emit(IL.Conv_I1); break;
                case CLRValueType.UINT_16:
                case CLRValueType.INT_16: Emit(IL.Conv_I2); break;
                case CLRValueType.UINT_32:
                case CLRValueType.INT_32: Emit(IL.Conv_I4); break;
                case CLRValueType.UINT_64:
                case CLRValueType.INT_64: Emit(IL.Conv_I8); break;
                default: break;
            }
        }

        public void Pop()
        {
            Emit(IL.Pop);
        }

        public void Nop()
        {
            Emit(IL.Nop);
        }

        public void Constant32(uint value, bool isUnsigned)
        {
            switch (value)
            {
                case 0: Emit(IL.Ldc_I4_0); break;
                case 1: Emit(IL.Ldc_I4_1); break;
                case 2: Emit(IL.Ldc_I4_2); break;
                case 3: Emit(IL.Ldc_I4_3); break;
                case 4: Emit(IL.Ldc_I4_4); break;
                case 5: Emit(IL.Ldc_I4_5); break;
                case 6: Emit(IL.Ldc_I4_6); break;
                case 7: Emit(IL.Ldc_I4_7); break;
                case 8: Emit(IL.Ldc_I4_8); break;
                default: Emit(IL.Ldc_I4, (int)value); break;
            }

            if (isUnsigned)
            {
                UnsignedConvert(CLRValueType.UINT_32);
            }
        }

        public void MathAdd(bool checkOverflow)
        {
            if (checkOverflow)
            {
                Emit(IL.Add_Ovf);
            }
            else
            {
                Emit(IL.Add);
            }
        }

        public void MathSub(bool checkOverflow)
        {
            if (checkOverflow)
            {
                Emit(IL.Sub_Ovf);
            }
            else
            {
                Emit(IL.Sub);
            }
        }

        public void TryCatchBlock(Action t, Action c, Type catchType)
        {
            Label outsideBlock = DefineLabel();

            BeginExceptionBlock();

            t.Invoke();

            Emit(IL.Leave_S, outsideBlock);

            BeginCatchBlock(catchType);
            Emit(IL.Pop);

            c.Invoke();

            Emit(IL.Leave_S, m_LabelInstEnd);
            EndExceptionBlock();

            MarkLabel(outsideBlock);
        }

        public void EmitLocalStore(int index)
        {
            switch (index)
            {
                case 0: Emit(OpCodes.Stloc_0); break;
                case 1: Emit(OpCodes.Stloc_1); break;
                case 2: Emit(OpCodes.Stloc_2); break;
                case 3: Emit(OpCodes.Stloc_3); break;
                default: Emit(OpCodes.Stloc_S, index); break;
            }
        }

        public void EmitLocalLoad(int index)
        {
            switch (index)
            {
                case 0: Emit(OpCodes.Ldloc_0); break;
                case 1: Emit(OpCodes.Ldloc_1); break;
                case 2: Emit(OpCodes.Ldloc_2); break;
                case 3: Emit(OpCodes.Ldloc_3); break;
                default: Emit(OpCodes.Ldloc_S, index); break;
            }
        }

        public void Constant32(int value)
        {
            switch (value)
            {
                case 0: Emit(OpCodes.Ldc_I4_0); break;
                case 1: Emit(OpCodes.Ldc_I4_1); break;
                case 2: Emit(OpCodes.Ldc_I4_2); break;
                case 3: Emit(OpCodes.Ldc_I4_3); break;
                case 4: Emit(OpCodes.Ldc_I4_4); break;
                case 5: Emit(OpCodes.Ldc_I4_5); break;
                case 6: Emit(OpCodes.Ldc_I4_6); break;
                case 7: Emit(OpCodes.Ldc_I4_7); break;
                case 8: Emit(OpCodes.Ldc_I4_8); break;
                default: Emit(OpCodes.Ldc_I4, value); break;
            }
        }

        public void EmitLocalPop()
        {
            Emit(IL.Pop);
        }

        [Conditional("DEBUG")]
        protected void LogIL(String line)
        {
            m_BlockInstLog.AppendLine(line);
            //Log.Debug(line);
        }

        private void LogIL(OpCode il) {
            LogIL(il.Name);
        }

        public void Emit(OpCode opcode)
        {
            m_Generator.Emit(opcode);
            LogIL(opcode);
        }

        public void Emit(OpCode opcode, byte arg)
        {
            m_Generator.Emit(opcode, arg);
            LogIL(opcode + " " + arg.ToString("X2"));
        }

        [SecuritySafeCritical]
        public void Emit(OpCode opcode, double arg)
        {
            m_Generator.Emit(opcode, arg);
            LogIL(opcode + " " + arg.ToString("G"));
        }

        public void Emit(OpCode opcode, short arg)
        {
            m_Generator.Emit(opcode, arg);
            LogIL(opcode + " " + arg.ToString("X4"));
        }

        public void Emit(OpCode opcode, int arg)
        {
            m_Generator.Emit(opcode, arg);
            LogIL(opcode + " " + arg.ToString("X8"));
        }

        public void Emit(OpCode opcode, long arg)
        {
            m_Generator.Emit(opcode, arg);
            LogIL(opcode + " " + arg.ToString("X16"));
        }

        [ComVisible(true)]
        [SecuritySafeCritical]
        public void Emit(OpCode opcode, ConstructorInfo con)
        {
            m_Generator.Emit(opcode, con);
            LogIL(opcode);
        }

        public void Emit(OpCode opcode, Label label)
        {
            m_Generator.Emit(opcode, label);
            LogIL(opcode);
        }

        public void Emit(OpCode opcode, Label[] labels)
        {
            m_Generator.Emit(opcode, labels);
            LogIL(opcode);
        }

        public void Emit(OpCode opcode, LocalBuilder local)
        {
            m_Generator.Emit(opcode, local);
            LogIL(opcode);
        }

        public void Emit(OpCode opcode, SignatureHelper signature)
        {
            m_Generator.Emit(opcode, signature);
            LogIL(opcode);
        }

        public void Emit(OpCode opcode, FieldInfo field)
        {
            m_Generator.Emit(opcode, field);
            LogIL(opcode);
        }

        [SecuritySafeCritical]
        public void Emit(OpCode opcode, MethodInfo meth)
        {
            m_Generator.Emit(opcode, meth);
            LogIL(opcode.Name + " " + meth.Name);
        }

        protected void Emit(OpCode opcode, sbyte arg)
        {
            m_Generator.Emit(opcode, arg);
            LogIL(opcode + " " + arg.ToString("X2"));
        }

        [SecuritySafeCritical]
        public void Emit(OpCode opcode, float arg)
        {
            m_Generator.Emit(opcode, arg);
            LogIL(opcode + " " + arg.ToString("G"));
        }

        public void Emit(OpCode opcode, string str)
        {
            m_Generator.Emit(opcode, str);
            LogIL(opcode + " " + str);
        }

        [SecuritySafeCritical]
        public void Emit(OpCode opcode, Type cls)
        {
            m_Generator.Emit(opcode, cls);
            LogIL(opcode);
        }

        public LocalBuilder DeclareLocal(Type localType)
        {
            m_BlockInstLog.AppendLine("Declare local: " + localType.Name);
            return m_Generator.DeclareLocal(localType);
        }

        public LocalBuilder DeclareLocal(Type localType, bool pinned)
        {
            m_BlockInstLog.AppendLine("Declare pinned local: " + localType.Name);
            return m_Generator.DeclareLocal(localType, pinned);
        }

        public Label DefineLabel()
        {
            m_BlockInstLog.AppendLine("Define label");
            return m_Generator.DefineLabel();
        }

        public void MarkLabel(Label loc)
        {
            m_BlockInstLog.AppendLine("mark label " + loc.ToString());
            m_Generator.MarkLabel(loc);
        }

        public void BeginCatchBlock(Type exceptionType)
        {
            m_BlockInstLog.AppendLine("begin catch block: " + exceptionType.Name);
            m_Generator.BeginCatchBlock(exceptionType);
        }

        public virtual Label BeginExceptionBlock()
        {
            m_BlockInstLog.AppendLine("begin exception block");
            return m_Generator.BeginExceptionBlock();
        }

        public void EndExceptionBlock()
        {
            m_BlockInstLog.AppendLine("end exception block");
            m_Generator.EndExceptionBlock();
        }

        public void BeginFinallyBlock()
        {
            m_BlockInstLog.AppendLine("begin finally block");
            m_Generator.BeginFinallyBlock();
        }

        public String GetCompileLog()
        {
            return m_BlockInstLog.ToString();
        }
    }
}
