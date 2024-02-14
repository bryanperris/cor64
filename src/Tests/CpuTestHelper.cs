using cor64.IO;
using cor64.Mips;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using NUnit.Framework;
using cor64;

namespace Tests
{
    internal static class CpuTestHelper
    {
        private static readonly List<RuntimeTypeHandle> s_InterpreterList;
        private static String s_LastOpcode;
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public enum ParamType
        {
            Normal,
            Immediate,
            Shift,
            Offset
        }

        static CpuTestHelper()
        {
            s_InterpreterList = new List<RuntimeTypeHandle>()
            {
                typeof(TestableInterpreter).TypeHandle,
                // typeof(TestableILRecompiler).TypeHandle
            };

            if (TestablePJ6.Ping())
            {
                s_InterpreterList.Add(typeof(TestablePJ6).TypeHandle);
            }
        }

        public static TestCase CPUTest(ulong operandA, ulong operandB, ParamType type = ParamType.Normal)
        {
            return s_LastOpcode.CPUTest(operandA, operandB, type);
        }

        public static TestCase CPUTest()
        {
            return s_LastOpcode.CPUTest();
        }

        public static void CPUTestBegin(this String opcode) {
            s_LastOpcode = opcode;
        }

        public static TestCase CPUTest(this String opcode, ulong operandA, ulong operandB, ParamType type = ParamType.Normal)
        {
            s_LastOpcode = opcode;

            var builder = new TestCase(opcode);

            switch (type)
            {
                case ParamType.Immediate: builder.WithImmediate(); break;
                case ParamType.Shift: builder.WithShift(); break;
                default: break;
            }

            builder.Values(operandA, operandB);
            return builder;
        }

        public static TestCase CPUTestTriple(this String opcode, ulong operandA, ulong operandB, uint operandC)
        {
            s_LastOpcode = opcode;

            var builder = new TestCase(opcode);

            builder.WithImmediate();
            builder.Values(operandA, operandB, operandC);
            return builder;
        }

        public static TestCase CPUTestDMem(this String opcode, int address, params byte[] data)
        {
            s_LastOpcode = opcode;

            var builder = new TestCase(opcode);

            builder.TestDMem(address, data);
            return builder;
        }

        public static TestCase CPUTestDMem(int address, params byte[] data)
        {
            return s_LastOpcode.CPUTestDMem(address, data);
        }

        public static TestCase CPUTestTriple(ulong operandA, ulong operandB, uint operandC)
        {
            return s_LastOpcode.CPUTestTriple(operandA, operandB, operandC);
        }

        public static TestCase CPUTestSingle(this String opcode, ulong operandA, ParamType param = ParamType.Normal)
        {
            s_LastOpcode = opcode;

            var builder = new TestCase(opcode);

            if (param == ParamType.Offset)
            {
                builder.JOffset((uint)operandA);
            }
            else if (param == ParamType.Immediate)
            {
                builder.WithImmediate();
                builder.Values(operandA);
            }
            else
            {
                builder.Values(operandA);
            }

            return builder;
        }

        public static TestCase CPUTestSingle(ulong operandA, ParamType param = ParamType.Normal)
        {
            return s_LastOpcode.CPUTestSingle(operandA, param);
        }

        public static TestCase CPUTest(this String opcode)
        {
            s_LastOpcode = opcode;

            return new TestCase(opcode);
        }

        public static TestCase CPUTest(this String opcode, ulong value, RegBoundType injectionSelect, RegBoundType expectedSelect)
        {
            s_LastOpcode = opcode;
            return new TestCase(opcode).Xfer(injectionSelect, expectedSelect, value);
        }

        public static TestCase Only32(this TestCase testCase)
        {
            testCase.Test64 = false;
            testCase.Test32 = true;
            return testCase;
        }

        public static TestCase Only64(this TestCase testCase)
        {
            testCase.Test64 = true;
            testCase.Test32 = false;
            return testCase;
        }

        public static void Run(this TestCase testCase)
        {
            try
            {
                Run(testCase, false);
            }
            catch (DllNotFoundException)
            {
                /* XXX: Ignore cases where an external DLL is required but failed to load */
                Assert.True(true);
            }
        }

        public static ulong Neg(this ulong value)
        {
            return ~(value - 1);
        }

        public static ulong Neg(this int value)
        {
            return Neg((ulong)value);
        }

        public static void Run(this TestCase testCase, bool toggleBreakpoint)
        {
            #if !CPU_CHECK_RESERVED
            if (testCase.Test32 && testCase.ExpectedExceptions == ExceptionType.Reserved) {
                Console.WriteLine("Skipping 32-bit reserved test");
                return;
            }
            #endif

            AsmHelper.AssembleMipsTestCase(testCase, toggleBreakpoint);

            for (int i = 0; i < s_InterpreterList.Count; i++)
            {
                Type type = Type.GetTypeFromHandle(s_InterpreterList[i]);

                if (testCase.CoreTypeOverride != null)
                {
                    if (testCase.CoreTypeOverride != type)
                    {
                        continue;
                    }
                }

                /* Test in 64-bit Kernel Mode */
                if (testCase.Test64)
                {
                    testCase.GetProgram().Position = 0;
                    ITestableCore testableCore = (ITestableCore)Activator.CreateInstance(type);
                    testableCore.Init(testCase);
                    testableCore.SetProcessorMode(cor64.Mips.ProcessorMode.Kernel | cor64.Mips.ProcessorMode.Runtime64);

                    if (toggleBreakpoint && Debugger.IsAttached)
                        Debugger.Break();

                    testableCore.StepOnce();
                    testableCore.TestExpectations();
                }

                /* Test in 32-bit kernel mode */
                /// XXX: Not much reason to be testing 32-bit mode...
                #if ALLOW_32BIT_TESTS
                if (testCase.Test32 && !testCase.IsFpuTest)
                {
                    testCase.GetProgram().Position = 0;
                    ITestableCore testableCore = (ITestableCore)Activator.CreateInstance(type);
                    testableCore.Init(testCase);
                    testableCore.SetProcessorMode(cor64.Mips.ProcessorMode.Kernel | cor64.Mips.ProcessorMode.Runtime32);

                    if (toggleBreakpoint && Debugger.IsAttached)
                        Debugger.Break();

                    testableCore.StepOnce();
                    testableCore.TestExpectations();
                }
                #endif
            }
        }
    }
}
