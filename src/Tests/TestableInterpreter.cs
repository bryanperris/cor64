using cor64.Mips;
using cor64.Mips.R4300I;
using cor64.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Remoting;
using NUnit.Framework;
using cor64;
using NLog;

namespace Tests
{
    internal sealed class TestableInterpreter : Interpreter, ITestableCore
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private TestCase m_TestCase;
        private bool m_UseWords;
		private N64MemoryStream m_N64MemoryStream;
        private static readonly N64MemoryController s_N64Memory;

        static TestableInterpreter() {
            s_N64Memory = new N64MemoryController();
            s_N64Memory.Init();
        }

        public TestableInterpreter() : base()
        {
        }

        public static void LogActivity(string name) {
            Log.Info("{0}", name);
        }

        public void Init(TestCase tester)
        {
            LogActivity("Init");

            m_TestCase = tester;
            s_N64Memory.RDRAM.Clear();

            m_N64MemoryStream = new N64MemoryStream(s_N64Memory);

            SetInstructionDebugMode(InstructionDebugMode.Full);

            // Copy the assembled test program into n64 memory
            var romStream = tester.GetProgram();
            romStream.Position = 0;

            #if HOST_LITTLE_ENDIAN
            romStream = new Swap32Stream(romStream);
            #endif

            m_N64MemoryStream.Position = 0;
            byte[] buffer = new byte[romStream.Length];
            romStream.Read(buffer, 0, buffer.Length);
            m_N64MemoryStream.WriteDirect(buffer, 0, buffer.Length);

            SetMMUBypass(true);
            //AttachIStream(new StreamEx.Wrapper(cor64.DataEndianess.PreByteSwapStream(tester.GetProgram(), cor64.Cartridge.RomEndianess.Big)));
            AttachMemory(s_N64Memory);

            Cop0.REGS.Status.SetFRMode(tester.UseFPUHalfMode);

            if (m_TestCase.IsXfer)
            {
                InjectXferSource(tester);
                return;
            }

            if (m_TestCase.IsLoadStore && m_TestCase.InjectDMem)
            {
                // The injected data is always big endian
                StringBuilder injectedHex = new StringBuilder();
                injectedHex.Append("Injected Data Memory: ");
                for (int i = 0; i < m_TestCase.ExpectedBytes.Length; i++) {
                    long address = N64Endianess.Address8(m_TestCase.ExepectedBytesOffset + i);

                    s_N64Memory.DirectWriteByte(
                        address,
                        m_TestCase.ExpectedBytes[i]
                    );

                    injectedHex.Append(s_N64Memory.DirectReadByte(m_TestCase.ExepectedBytesOffset + i).ToString("X2"));
                }
               Console.WriteLine(injectedHex.ToString());
            }

            if (!m_TestCase.IsFpuTest)
            {

                if (tester.SourceA.Key >= 0)
                    WriteGPR64(tester.SourceA.Key, (ulong)tester.SourceA.Value);

                if (!tester.IsImmediate && tester.SourceB.Key >= 0)
                {
                    WriteGPR64(tester.SourceB.Key, (ulong)tester.SourceB.Value);
                }
            }
            else
            {
                if (tester.SourceA.Key >= 0)
                {
                    SetFPRValue(tester.SourceA.Key, tester.SourceA.Value);
                }

                if (tester.SourceB.Key >= 0)
                {
                    SetFPRValue(tester.SourceB.Key, tester.SourceB.Value);
                }
            }
        }

        private void SetFPRValue(int index, dynamic value)
        {
            LogActivity(nameof(SetFPRValue));

            var type = value.GetType();

            if (typeof(uint) == type)
            {
                WriteFPR_W(index, value);
            }
            else if (typeof(ulong) == type)
            {
                WriteFPR_DW(index, value);
            }
            else if (typeof(float) == type)
            {
                WriteFPR_S(index, value);
            }
            else if (typeof(double) == type)
            {
                WriteFPR_D(index, value);
            }
            else {
                throw new ArgumentException("invalid type to set fpr value");
            }
        }

        private void InjectXferSource(TestCase test)
        {
            LogActivity(nameof(InjectXferSource));

            switch (test.XferSource)
            {
                case RegBoundType.Gpr:
                    {
                        WriteGPR64(test.SourceA.Key, test.SourceA.Value); break;
                    }
                case RegBoundType.Hi:
                    {
                        WriteHi(test.SourceA.Value); break;
                    }
                case RegBoundType.Lo:
                    {
                        WriteLo(test.SourceA.Value); break;
                    }
                case RegBoundType.Cp0:
                    {
                        Cop0State.Write(test.SourceCp0.Key, test.SourceCp0.Value); break;
                    }
                default: throw new NotImplementedException();
            }
        }

        public void SetProcessorMode(ProcessorMode mode)
        {
            LogActivity(nameof(SetProcessorMode));

            /* Right now this is just a hack */
            if ((mode & ProcessorMode.Runtime32) == ProcessorMode.Runtime32)
            {
                SetOperation64Mode(false);
                m_UseWords = true;
            }
            else
            {
                SetOperation64Mode(true);
            }
        }

        public void StepOnce()
        {
            LogActivity(nameof(StepOnce));
            Step();
        }

        public override void Step()
        {
            base.Step();
        }

        private static String L(ulong value)
        {
            return value.ToString("X16");
        }

        private static String I(uint value)
        {
            return value.ToString("X8");
        }

        private static void AssertMemory(N64MemoryController memory, long address, byte[] testBuffer)
        {
            byte[] readBuffer = new byte[testBuffer.Length];

            for (int i = 0; i < readBuffer.Length; i++) {
                readBuffer[i] = memory.DirectReadByte(N64Endianess.Address8(address + i));
            }

            Assert.AreEqual(
                BytesToHex(testBuffer),
                BytesToHex(readBuffer));
        }

        private static String BytesToHex(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder();

            for (int i = 0; i < bytes.Length; i++)
            {
                sb.Append(bytes[i].ToString("X2"));
            }

            return sb.ToString();
        }

        public void TestExpectations()
        {
            LogActivity(nameof(TestExpectations));
            if ((m_TestCase.ExpectationFlags & TestCase.Expectations.Exceptions) == TestCase.Expectations.Exceptions)
            {
                if (!m_TestCase.IsFpuTest)
                {
                    Assert.AreEqual(m_TestCase.ExpectedExceptions, Exceptions);
                }
                else
                {
                    // TODO: Should test that cop0 will contain an exception for FPU
                    Assert.AreEqual(m_TestCase.ExpectedFPUExceptions, State.FCR.Cause);
                }

                return;
            }
            else
            {
                Assert.AreEqual(ExceptionType.Interrupt, Exceptions);

                if (m_TestCase.IsFpuTest)
                {
                    Assert.AreEqual(FpuExceptionFlags.None, State.FCR.Cause);
                }
            }

            // TODO: Xfer source is GPR, does it affect this part?
            if ((m_TestCase.ExpectationFlags & TestCase.Expectations.Result) == TestCase.Expectations.Result)
            {
                if (!m_TestCase.IsFpuTest)
                {
                    TestGPRExpectations();
                }
                else
                {
                    TestFPRExpectations();
                }
            }

            if (m_TestCase.XferSource != RegBoundType.Lo)
            {
                if ((m_TestCase.ExpectationFlags & TestCase.Expectations.ResultLo) == TestCase.Expectations.ResultLo)
                {
                    if (!m_UseWords)
                    {
                        Assert.AreEqual(L((ulong)m_TestCase.ExpectedLo), L(ReadLo()));
                    }
                    else
                    {
                        Assert.AreEqual(I((uint)m_TestCase.ExpectedLo), I((uint)ReadLo()));
                    }
                }
                else
                {
                    Assert.AreEqual(L(0UL), L(ReadLo()));
                }
            }

            if (m_TestCase.XferSource != RegBoundType.Hi)
            {
                if ((m_TestCase.ExpectationFlags & TestCase.Expectations.ResultHi) == TestCase.Expectations.ResultHi)
                {
                    if (!m_UseWords)
                    {
                        Assert.AreEqual(L((ulong)m_TestCase.ExpectedHi), L(ReadHi()));
                    }
                    else
                    {
                        Assert.AreEqual(I((uint)m_TestCase.ExpectedHi), I((uint)ReadHi()));
                    }
                }
                else
                {
                    Assert.AreEqual(L(0UL), L(ReadHi()));
                }
            }

            if (m_TestCase.IsBranch)
            {
                // TODO: Assert branching using the updated CPU pipeline process

                if ((m_TestCase.ExpectationFlags & TestCase.Expectations.BranchTaken) == TestCase.Expectations.BranchTaken)
                {
                    Assert.AreEqual(4UL, BU_TargetAddress);
                }
                else
                {
                    Assert.AreEqual(0UL, BU_TargetAddress);
                }

                if ((m_TestCase.ExpectationFlags & TestCase.Expectations.DelaySlot) == TestCase.Expectations.DelaySlot)
                {
                    Assert.True(BU_ExecuteBranchDelay);
                }
                else
                {
                    Assert.False(BU_ExecuteBranchDelay);
                }

                if ((m_TestCase.ExpectationFlags & TestCase.Expectations.Link) == TestCase.Expectations.Link)
                {
                    /* Return address check */
                    Assert.AreEqual(L(8UL), L(ReadRA()));
                }
                else
                {
                    Assert.AreEqual(L(0UL), L(ReadRA()));
                }
            }

            if (m_TestCase.IsJump)
            {
                Assert.AreEqual(L(m_TestCase.ExpectedJump), L((ulong)BU_TargetAddress));

                if ((m_TestCase.ExpectationFlags & TestCase.Expectations.Link) == TestCase.Expectations.Link)
                {
                    /* Return address check */
                    Assert.AreEqual(L(8UL), L(ReadRA()));
                }
                else
                {
                    Assert.AreEqual(L(0UL), L(ReadRA()));
                }
            }

            if (m_TestCase.IsLoadStore)
            {
                if ((m_TestCase.ExpectationFlags & TestCase.Expectations.DMemStore) == TestCase.Expectations.DMemStore)
                {
                    AssertMemory(s_N64Memory, m_TestCase.ExepectedBytesOffset, m_TestCase.ExpectedBytes);
                }
            }
        }

        private void TestGPRExpectations()
        {
            LogActivity(nameof(TestGPRExpectations));
            if (!m_UseWords)
                Assert.AreEqual(L((ulong)m_TestCase.Result.Value), L(ReadGPR64(m_TestCase.Result.Key)));
            else
            {
                /* Test values as 32-bit words */
                Assert.AreEqual(I((uint)m_TestCase.Result.Value), I((uint)ReadGPR64(m_TestCase.Result.Key)));
            }
        }

        private String F(float value)
        {
            var v = value.ToString("G");

            if (v.StartsWith("-"))
                v = v.Substring(1);

            return v;
        }

        private String D(double value)
        {
            var v = value.ToString("G");

            if (v.StartsWith("-"))
                v = v.Substring(1);

            return v;
        }

        private void TestFPRExpectations()
        {
            LogActivity(nameof(TestFPRExpectations));
            var v = m_TestCase.Result.Value;

            switch (m_TestCase.ExpectedFpuType) {
                case FpuValueType.Reserved: throw new ArgumentException("cannot test with reserved fpu types");

                case FpuValueType.Word:
                    {
                        Assert.That(v, Is.TypeOf<uint>());
                        Assert.AreEqual(I(v), I(ReadFPR_W(m_TestCase.Result.Key)));
                        break;
                    }

                case FpuValueType.Doubleword:
                    {
                        Assert.That(v, Is.TypeOf<ulong>());
                        Assert.AreEqual(L(v), L(ReadFPR_DW(m_TestCase.Result.Key)));
                        break;
                    }

                case FpuValueType.FSingle:
                    {
                        Assert.That(v, Is.TypeOf<float>());
                        Assert.AreEqual(F(v), F(ReadFPR_S(m_TestCase.Result.Key)));
                        break;
                    }

                case FpuValueType.FDouble:
                    {
                        Assert.That(v, Is.TypeOf<double>());
                        Assert.AreEqual(D(v), D(ReadFPR_D(m_TestCase.Result.Key)));
                        break;
                    }
            }
        }
    }
}
