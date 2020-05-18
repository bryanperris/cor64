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
using cor64.Mips.Rsp;

namespace Tests
{
    internal static class RspTestHelper
    {
        private static String m_UcodeName = "";
        private static int s_ExpectedUCodeSize = 0;
        private static bool s_GenerateUCode = false;
        private static Asm.RspUCodeTestType s_RspTestType = Asm.RspUCodeTestType.VecSourceTargetDest;

        public static void SetTargetUCode(this String name, int expectedSize=0) {
            m_UcodeName = name;
            s_ExpectedUCodeSize = expectedSize;
            s_GenerateUCode = false;
        }

        public static void GenerateAndSetTargetUCode(this String opcode, Asm.RspUCodeTestType testType = Asm.RspUCodeTestType.VecSourceTargetDest) {
            m_UcodeName = opcode;
            s_ExpectedUCodeSize = 17 * 4;
            s_GenerateUCode = true;
            s_RspTestType = testType;
        }

        public static RspTestCase RspTest() {
            return new RspTestCase(m_UcodeName);
        }

        public static RspTestCase RspTest(RspVector a, RspVector b) {
            return new RspTestCase(m_UcodeName)
            {
                SourceA = a,
                SourceB = b
            };
        }

        public static void Run(this RspTestCase testCase) {
            var rsp = new TestableRsp();

            /* Assmemble and check ucode */
            var ucodeData = s_GenerateUCode ? Asm.GenTestRspUCode(s_RspTestType, m_UcodeName) : Asm.AssembleRspUCode(m_UcodeName);

            if (s_ExpectedUCodeSize > 0) {
                Assert.AreEqual(s_ExpectedUCodeSize, ucodeData.Length);
            }

            rsp.Init(testCase);
            rsp.LoadUcode(ucodeData);
            rsp.ExecuteUcode();
            rsp.TestExpectations();
        }

        public static RspTestCase Expect(this RspTestCase testCase, RspVector expectedValue) {
            testCase.ExpectedResult = expectedValue;
            return testCase;
        }
        
        public static RspTestCase ExpectAccumulator(this RspTestCase testCase, RspVector hi, RspVector mid, RspVector lo) {
            testCase.ExpectedAcc = new RspVector[] { hi, mid, lo };
            return testCase;
        }

        public static RspTestCase ExpectCarry(this RspTestCase testCase, ushort expectedValue) {
            testCase.ExpectedCarry = expectedValue;
            return testCase;
        }

        public static RspTestCase ExpectCompare(this RspTestCase testCase, ushort expectedValue) {
            testCase.ExpectedCompare = expectedValue;
            return testCase;
        }

        public static RspTestCase ExpectExtension(this RspTestCase testCase, byte expectedValue) {
            testCase.ExpectedExtension = expectedValue;
            return testCase;
        }

        public static RspTestCase InjectAccumulator(this RspTestCase testCase, RspVector hi, RspVector mid, RspVector lo) {
            testCase.InjectedAcc = new RspVector[] { hi, mid, lo };
            return testCase;
        }

        public static RspTestCase InjectVcc(this RspTestCase testCase, ushort vcc) {
            testCase.InjectedVcc = vcc;
            return testCase;
        }
    }
}
