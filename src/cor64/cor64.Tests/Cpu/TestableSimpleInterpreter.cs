using cor64.Mips;
using cor64.Mips.R4300I;
using cor64.IO;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace cor64.Tests.Cpu
{
    internal sealed class TestableSimpleInterpreter : SimpleInterpreter, ITestableCore
    {
        private TestCase m_TestCase;
        private bool m_UseWords;
		private MemoryStream m_DataMemory = new MemoryStream();

        public TestableSimpleInterpreter() : base(true)
        {
        }

        public void Init(TestCase tester)
        {
            m_TestCase = tester;

            BypassMMU = true;
            AttachIStream(new StreamEx.Wrapper(tester.GetProgram()));
			AttachDStream(new StreamEx.Wrapper(m_DataMemory));

            /* Inject zeros into data memory */
            for (int i = 0; i < 16; i++)
            {
                m_DataMemory.WriteByte(0);
            }

            if (m_TestCase.IsXfer)
            {
                InjectXferSource(tester);
                return;
            }

            if (m_TestCase.IsLoadStore && m_TestCase.InjectDMem)
            {
                Stream s = m_DataMemory;

                //switch (m_TestCase.ExpectedBytes.Length)
                //{
                //    default: break;
                //    case 2: s = new Swap16Stream(s); break;
                //    case 4: s = new Swap32Stream(s); break;
                //    case 8: s = new Swap64Stream(s); break;
                //}

                s.Position = m_TestCase.ExepectedBytesOffset;
                s.Write(m_TestCase.ExpectedBytes, 0, m_TestCase.ExpectedBytes.Length);
            }

            if (!m_TestCase.IsFpuTest)
            {

                if (tester.SourceA.Key >= 0)
                    State.GPR_64[tester.SourceA.Key] = (ulong)tester.SourceA.Value;

                if (!tester.IsImmediate && tester.SourceB.Key >= 0)
                {
                    State.GPR_64[tester.SourceB.Key] = (ulong)tester.SourceB.Value;
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
            var type = value.GetType();

            if (typeof(uint) == type)
            {
                State.FPR_32[index] = (uint)value;
            }
            else if (typeof(ulong) == type)
            {
                State.FPR_64[index] = (ulong)value;
            }
            else if (typeof(float) == type)
            {
                State.FPR_FLOAT[index] = (float)value;
            }
            else if (typeof(double) == type)
            {
                State.FPR_DOUBLE[index] = (double)value;
            }
            else {
                throw new ArgumentException("invalid type to set fpr value");
            }
        }

        private void InjectXferSource(TestCase test)
        {
            switch (test.XferSource)
            {
                case RegBoundType.Gpr:
                    {
                        State.GPR_64[test.SourceA.Key] = test.SourceA.Value; break;
                    }
                case RegBoundType.Hi:
                    {
                        State.Hi = test.SourceA.Value; break;
                    }
                case RegBoundType.Lo:
                    {
                        State.Lo = test.SourceA.Value; break;
                    }
                case RegBoundType.Cp0:
                    {
                        Cp0Regs.Write(test.SourceCp0.Key, test.SourceCp0.Value); break;
                    }
                default: throw new NotImplementedException();
            }
        }

        public void SetProcessorMode(ProcessorMode mode)
        {
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
            Step();
        }

        private static String L(ulong value)
        {
            return value.ToString("X16");
        }

        private static String I(uint value)
        {
            return value.ToString("X8");
        }

        private static bool AssertStreamBytes(Stream source, int offset, byte[] testBuffer)
        {
            var s = source;

            //switch(testBuffer.Length)
            //{
            //    default: break;
            //    case 2: s = new Swap16Stream(s); break;
            //    case 4: s = new Swap32Stream(s); break;
            //    case 8: s = new Swap64Stream(s); break;
            //}

            s.Position = offset;
            byte[] readBuffer = new byte[testBuffer.Length];
            s.Read(readBuffer, 0, readBuffer.Length);
            return testBuffer.SequenceEqual(readBuffer);
        }

        public void TestExpectations()
        {
            if ((m_TestCase.ExpectationFlags & TestCase.Expectations.Exceptions) == TestCase.Expectations.Exceptions)
            {
                Assert.Equal(m_TestCase.ExpectedExceptions, Exceptions);
                return;
            }
            else
            {
                Assert.Equal(ExceptionType.Interrupt, Exceptions);
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
                        Assert.Equal(L((ulong)m_TestCase.ExpectedLo), L(State.Lo));
                    }
                    else
                    {
                        Assert.Equal(I((uint)m_TestCase.ExpectedLo), I((uint)State.Lo));
                    }
                }
                else
                {
                    Assert.Equal(L(0UL), L(State.Lo));
                }
            }

            if (m_TestCase.XferSource != RegBoundType.Hi)
            {
                if ((m_TestCase.ExpectationFlags & TestCase.Expectations.ResultHi) == TestCase.Expectations.ResultHi)
                {
                    if (!m_UseWords)
                    {
                        Assert.Equal(L((ulong)m_TestCase.ExpectedHi), L(State.Hi));
                    }
                    else
                    {
                        Assert.Equal(I((uint)m_TestCase.ExpectedHi), I((uint)State.Hi));
                    }
                }
                else
                {
                    Assert.Equal(L(0UL), L(State.Hi));
                }
            }

            if (m_TestCase.IsBranch)
            {
                if ((m_TestCase.ExpectationFlags & TestCase.Expectations.BranchTaken) == TestCase.Expectations.BranchTaken)
                {
                    Assert.Equal(8UL, BranchTarget);
                    //Assert.Equal(4L, m_Pc);
                }
                else
                {
                    Assert.Equal(0UL, BranchTarget);
                }

                if ((m_TestCase.ExpectationFlags & TestCase.Expectations.DelaySlot) == TestCase.Expectations.DelaySlot)
                {
                    Assert.True(BranchDelaySlot);
                }
                else
                {
                    Assert.False(BranchDelaySlot);
                }

                if ((m_TestCase.ExpectationFlags & TestCase.Expectations.Link) == TestCase.Expectations.Link)
                {
                    /* Return address check */
                    Assert.Equal(L(8UL), L(State.GPR_64[31]));
                }
                else
                {
                    Assert.Equal(L(0UL), L(State.GPR_64[31]));
                }
            }

            if (m_TestCase.IsJump)
            {
                Assert.Equal(L(m_TestCase.ExpectedJump), L(BranchTarget));

                if ((m_TestCase.ExpectationFlags & TestCase.Expectations.Link) == TestCase.Expectations.Link)
                {
                    /* Return address check */
                    Assert.Equal(L(8UL), L(State.GPR_64[31]));
                }
                else
                {
                    Assert.Equal(L(0UL), L(State.GPR_64[31]));
                }
            }

            if (m_TestCase.IsLoadStore)
            {
                if ((m_TestCase.ExpectationFlags & TestCase.Expectations.DMemStore) == TestCase.Expectations.DMemStore)
                {
                    Assert.True(AssertStreamBytes(m_DataMemory, m_TestCase.ExepectedBytesOffset, m_TestCase.ExpectedBytes));
                }
            }
        }

        private void TestGPRExpectations()
        {
            if (!m_UseWords)
                Assert.Equal(L((ulong)m_TestCase.Result.Value), L(State.GPR_64[m_TestCase.Result.Key]));
            else
            {
                /* Test values as 32-bit words */
                Assert.Equal(I((uint)m_TestCase.Result.Value), I((uint)State.GPR_64[m_TestCase.Result.Key]));
            }
        }

        private void TestFPRExpectations()
        {
            switch (m_TestCase.ExpectedFpuType) {
                case FpuValueType.Reserved: throw new ArgumentException("cannot test with reserved fpu types");
                case FpuValueType.Word: Assert.IsType(m_TestCase.Result.Value.GetType(), typeof(uint)); break;
                case FpuValueType.Doubleword: Assert.IsType(m_TestCase.Result.Value.GetType(), typeof(ulong)); break;
                case FpuValueType.FSingle: Assert.IsType(m_TestCase.Result.Value.GetType(), typeof(float)); break;
                case FpuValueType.FDouble: Assert.IsType(m_TestCase.Result.Value.GetType(), typeof(double)); break;
            }
        }
    }
}
