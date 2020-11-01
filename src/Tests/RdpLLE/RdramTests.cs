using System.Text;
using System.IO;
using System;
using cor64.Rdp.LLE;
using NUnit.Framework;

namespace Tests.RdpLLE {
    [TestFixture]
    public class RdramTests : BaseTest {

        [Test]
        public void RdramWriteTests() {
            // byte[] buffer = new byte[0x100];
            // MemoryStream mem = new MemoryStream(buffer);
            // Rdram rdram = new Rdram(mem);

            // rdram.WriteIdx16(0, 0xDEAD);

            // byte[] read = new byte[4];
            // mem.Position = 0;
            // mem.Read(read, 0, read.Length);

            // StringBuilder sb = new StringBuilder();

            // for (int i = 0; i < read.Length; i++) {
            //     sb.Append(read[i].ToString("X2"));
            // }

            // Assert.AreEqual("DEAD0000", sb.ToString());
        }
    }
}