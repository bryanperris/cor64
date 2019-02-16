using cor64.IO;
using cor64.Mips;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using NLog;

namespace cor64.Tests.Cpu
{
    internal static class Helper
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

        static Helper()
        {
            s_InterpreterList = new List<RuntimeTypeHandle>()
            {
                typeof(TestableSimpleInterpreter).TypeHandle,
                typeof(TestableILRecompiler).TypeHandle
            };

            if (TestablePJ6.Ping())
            {
                s_InterpreterList.Add(typeof(TestablePJ6).TypeHandle);
            }
        }

        private static Stream _Assemble(params String[] asm)
        {
            N64Assembler assembler = new N64Assembler();

            /* Assembly Source */
            var source = new AssemblyTextSource("main");
            source += "arch n64.cpu";
            source += "endian msb";

            StringBuilder line = new StringBuilder();

            for (int i = 0; i < asm.Length; i++)
            {
                line.Append(asm[i]);
                if (i + 1 < asm.Length) line.Append(" ");
            }

            source += line.ToString();

            /* Assemble into bytes */
            assembler.AddAssemblySource(source);
            assembler.AssembleCode(true);

            /* Get the output */
            var streamOut = assembler.Output;
            streamOut.Position = 0;

            return streamOut;
        }

        public static TestCase CPUTest(ulong operandA, ulong operandB, ParamType type = ParamType.Normal)
        {
            return s_LastOpcode.CPUTest(operandA, operandB, type);
        }

        public static TestCase CPUTest()
        {
            return s_LastOpcode.CPUTest();
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
            Run(testCase, false);
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
            Assert.False(!testCase.Test32 && !testCase.Test64);

            Stream rom = null;

            String layout = "{0} {1}{2}{3}";

            String asm = "";

            if (testCase.IsJump)
            {
                asm = testCase.Opcode;
                asm += " ";

                if (testCase.JumpOffset != null)
                {
                    asm += testCase.JumpOffset.Value;
                }
                else
                {
                    asm += testCase.SourceA.Key;
                }
            }
            else if (testCase.IsBranch)
            {
                /* All branch tests are simple, they always target address (word-aligned) */

                /* The assembler auto divides the offset operand by 4 */

                asm = String.Format(
                    layout,
                    testCase.Opcode,
                    testCase.SourceA.Key + ",",
                    testCase.SourceB.Key >= 0 ? (testCase.SourceB.Key + ",") : "",
                    "4");
            }
            else if (testCase.IsLoadStore)
            {
                layout = "{0} {1},{3}({2})";

                asm = String.Format(
                    layout,
                    testCase.Opcode,
                    testCase.SourceA.Key,
                    testCase.SourceB.Key,
                    (ushort)testCase.SourceC.Value
                    );
            }
            else if (!testCase.IsXfer)
            {
                /* Used by most instructions */

                StringBuilder asmBuilder = new StringBuilder();
                bool chainStart = false;

                asmBuilder.Append(testCase.Opcode);
                asmBuilder.Append(" ");

                if (testCase.Result.Key >= 0 && !testCase.NoDestinationReg)
                {
                    asmBuilder.Append(testCase.Result.Key.ToString());
                    chainStart = true;
                }

                if (testCase.SourceA.Key >= 0)
                {
                    if (chainStart)
                    {
                        asmBuilder.Append(",");
                    }

                    if (testCase.IsImmediate && testCase.SourceB.Key < 0)
                    {
                        asmBuilder.Append((ushort)testCase.SourceA.Value);
                    }
                    else
                    {
                        asmBuilder.Append(testCase.SourceA.Key);
                    }

                    chainStart = true;
                }

                if (testCase.SourceB.Key >= 0)
                {
                    if (chainStart)
                    {
                        asmBuilder.Append(",");
                    }

                    if (testCase.IsImmediate || testCase.IsShift)
                    {
                        asmBuilder.Append((ushort)testCase.SourceB.Value);
                    }
                    else
                    {
                        asmBuilder.Append(testCase.SourceB.Key);
                    }
                }

                asm = asmBuilder.ToString();
            }
            else if (testCase.IsFpuTest)
            {
                layout = "{0} {1},{2}";

                asm = String.Format(
                        layout,
                        testCase.Opcode,
                        testCase.SourceA.Key,
                        testCase.SourceB.Key
                    );
            }
            else
            {
                String operandA = "";
                String sep = "";
                String operandB = "";

                switch (testCase.XferSource)
                {
                    case RegBoundType.Gpr: operandA = testCase.SourceA.Key.ToString(); break;
                    case RegBoundType.Cp0: operandA = testCase.SourceCp0.Key.ToString(); break;
                    default: break;
                }

                switch (testCase.XferTarget)
                {
                    case RegBoundType.Gpr: operandB = testCase.Result.Key.ToString(); break;
                    case RegBoundType.Cp0: operandB = testCase.SourceCp0.Key.ToString(); break;
                    default: break;
                }

                if (operandA.Length > 0 && operandB.Length > 0)
                {
                    sep = ",";
                }

                asm = testCase.Opcode + " " + operandA + sep + operandB;
            }

            Log.Info("Test Asm: " + asm);

            /* Register and Immediate */
            rom = _Assemble(asm);

            testCase.SetProgram(rom);

            for (int i = 0; i < s_InterpreterList.Count; i++)
            {
                Type type = Type.GetTypeFromHandle(s_InterpreterList[i]);

                /* Test in 64-bit Kernel Mode */
                if (testCase.Test64)
                {
                    rom.Position = 0;
                    ITestableCore testableCore = (ITestableCore)Activator.CreateInstance(type);
                    testableCore.Init(testCase);
                    testableCore.SetProcessorMode(Mips.ProcessorMode.Kernel | Mips.ProcessorMode.Runtime64);

                    if (toggleBreakpoint && Debugger.IsAttached)
                        Debugger.Break();

                    testableCore.StepOnce();
                    testableCore.TestExpectations();
                }

                /* Test in 32-bit kernel mode */
                if (testCase.Test32)
                {
                    rom.Position = 0;
                    ITestableCore testableCore = (ITestableCore)Activator.CreateInstance(type);
                    testableCore.Init(testCase);
                    testableCore.SetProcessorMode(Mips.ProcessorMode.Kernel | Mips.ProcessorMode.Runtime32);

                    if (toggleBreakpoint && Debugger.IsAttached)
                        Debugger.Break();

                    testableCore.StepOnce();
                    testableCore.TestExpectations();
                }
            }
        }
    }
}
