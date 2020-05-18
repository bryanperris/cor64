using System.Numerics;
using System;
using cor64.IO;
using cor64.Mips.Rsp;
using cor64.RCP;
using NUnit.Framework;

namespace Tests
{
    public class TestableRsp
    {
        private readonly RcpCore m_Rcp = new RcpCore();
        private readonly N64MemoryController m_Memory = new N64MemoryController();
        private readonly InterpreterBaseRsp m_Rsp;
        private bool m_ErrorThrown;
        private RspTestCase m_TestCase;

        public TestableRsp()
        {
            m_Rsp = m_Rcp.DeviceRsp;
        }

        public void Init(RspTestCase testCase)
        {
            m_TestCase = testCase;
            m_Rcp.AttachToMemory(m_Memory);

            if (m_TestCase.SourceA != null) {
                WriteVec(0x0000, m_TestCase.SourceA);
            }

            if (m_TestCase.SourceB != null) {
                WriteVec(0x0010, m_TestCase.SourceB);
            }

            if (m_TestCase.InjectedAcc != null) {
                for (int i = 0; i < 8; i++) {
                    m_Rsp.Acc.Hi(i, m_TestCase.InjectedAcc[0].PackedU16(i));
                    m_Rsp.Acc.Mi(i, m_TestCase.InjectedAcc[1].PackedU16(i));
                    m_Rsp.Acc.Lo(i, m_TestCase.InjectedAcc[2].PackedU16(i));
                }
            }

            if (m_TestCase.InjectedVcc != null) {
                m_Rsp.WriteVCC(m_TestCase.InjectedVcc.Value);
            }
        }

        /// <summary>
        /// Copy the assembled rsp task prorgam into SP instruction memory
        /// </summary>
        /// <param name="ucodeName"></param>
        public void LoadUcode(Byte[] ucode)
        {
            var imem = m_Rcp.RspInterface.CreateIMemorySream();
            imem.Position = 0;
            imem.Write(ucode, 0, ucode.Length);
        }

        public void ExecuteUcode()
        {
            try
            {
                /* Unhalt the signal processor */
                m_Rsp.ManualStart(0);

                while (!m_Rsp.IsHalted)
                {
                    m_Rsp.Step();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("Stack Trace: " + e.StackTrace);
                m_ErrorThrown = true;
            }
        }

        private RspVector ReadVec(uint address) {
            var memVec = new RspVector();

            byte[] buffer = new byte[16];

            var source = m_Rsp.DMem.BaseStream;
            source.Position = address;
            source.Read(buffer, 0, buffer.Length);

            memVec.SetBytesSwapped(buffer);

            return memVec;
        }

        private void WriteVec(uint address, RspVector vector) {
            byte[] buffer = vector.GetRawBytesSwapped();
            
            var source = m_Rsp.DMem.BaseStream;
            source.Position = address;
            source.Write(buffer, 0, buffer.Length);
        }

        public void TestExpectations()
        {
            Assert.False(m_ErrorThrown);

            if (m_TestCase.ExpectedResult != null) {
                Assert.AreEqual(m_TestCase.ExpectedResult.ToString(), ReadVec(0).ToString());
            }

            if (m_TestCase.ExpectedAcc != null) {
                Assert.AreEqual(m_TestCase.ExpectedAcc[0].ToString(), ReadVec(16).ToString());
                Assert.AreEqual(m_TestCase.ExpectedAcc[1].ToString(), ReadVec(16 * 2).ToString());
                Assert.AreEqual(m_TestCase.ExpectedAcc[2].ToString(), ReadVec(16 * 3).ToString());
            }

            if (m_TestCase.ExpectedCarry != null) {
                Assert.AreEqual(((ushort)m_TestCase.ExpectedCarry).ToString("X4"), m_Rsp.ReadVCO().ToString("X4"));
            }
            else {
                Assert.AreEqual("0000", m_Rsp.ReadVCO().ToString("X4"));
            }

            if (m_TestCase.ExpectedCompare != null) {
                Assert.AreEqual(((ushort)m_TestCase.ExpectedCompare).ToString("X4"), m_Rsp.ReadVCC().ToString("X4"));
            }
            else {
                Assert.AreEqual("0000", m_Rsp.ReadVCC().ToString("X4"));
            }

            if (m_TestCase.ExpectedExtension != null) {
                Assert.AreEqual(((ushort)m_TestCase.ExpectedExtension).ToString("X2"), m_Rsp.ReadVCE().ToString("X2"));
            }
            else {
                Assert.AreEqual("00", m_Rsp.ReadVCE().ToString("X2"));
            }
        }
    }
}