using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cor64.Mips;

namespace cor64.Tests.Cpu
{
    public static class FpuHelper
    {
        private static String s_LastOpcode;

        public static TestCase FPUTest(this String opcode, float operandA, float operandB)
        {
            s_LastOpcode = opcode;

            var builder = new TestCase(opcode, true);
            builder.FpuValues(operandA, operandB);
            return builder;
        }

        public static TestCase FPUTest(float operandA, float operandB)
        {
            return s_LastOpcode.FPUTest(operandA, operandB);
        }

        public static TestCase FPUTest(this String opcode, double operandA, double operandB)
        {
            s_LastOpcode = opcode;

            var builder = new TestCase(opcode, true);
            builder.FpuValues(operandA, operandB);
            return builder;
        }

        public static TestCase FPUTest(double operandA, double operandB)
        {
            return s_LastOpcode.FPUTest(operandA, operandB);
        }

        public static TestCase FPUTest(this String opcode, float operandA)
        {
            s_LastOpcode = opcode;

            var builder = new TestCase(opcode, true);
            builder.FpuValues(operandA);
            return builder;
        }

        public static TestCase FPUTest(float operandA)
        {
            return s_LastOpcode.FPUTest(operandA);
        }

        public static TestCase FPUTest(this String opcode, double operandA)
        {
            s_LastOpcode = opcode;

            var builder = new TestCase(opcode, true);
            builder.FpuValues(operandA);
            return builder;
        }

        public static TestCase FPUTest(double operandA)
        {
            return s_LastOpcode.FPUTest(operandA);
        }

        public static TestCase FPUTest(this String opcode)
        {
            s_LastOpcode = opcode;

            return new TestCase(opcode, true);
        }

        public static FpuValueType ResolveFpuType(dynamic value)
        {
            Type t = value.GetType();

            if (t == typeof(uint))
            {
                return FpuValueType.Word;
            }
            else if (t == typeof(ulong))
            {
                return FpuValueType.Doubleword;
            }
            else if (t == typeof(float))
            {
                return FpuValueType.FSingle;
            }
            else if (t == typeof(double))
            {
                return FpuValueType.FDouble;
            }
            else
            {
                throw new ArgumentException("unsupported value type: " + t.ToString());
            }
        }
    }
}
