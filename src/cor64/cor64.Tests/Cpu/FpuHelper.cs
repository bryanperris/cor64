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

            var builder = new TestCase(opcode);
            builder.Values(operandA, operandB);
            return builder;
        }

        public static TestCase FPUTest(this String opcode)
        {
            s_LastOpcode = opcode;

            return new TestCase(opcode, true);
        }
    }
}
