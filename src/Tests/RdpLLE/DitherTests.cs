using System;
using cor64.Rdp.LLE;
using NUnit.Framework;

namespace Tests.RdpLLE {
    [TestFixture]
    public class DitherTests : BaseTest {

        [Test]
        public void RgbDitherTests() {
            static void D(int sel, int dither, string expectedR, string expectedG, string expectedB) {
                var system = RdpHelper.CreateTestableSystem();
                var rdp = system.GetRdp();

                rdp.SetTestData();

                int r = 0xFF;
                int g = 0xEE;
                int b = 0xAA;

                Dither.RgbDither(sel, ref r, ref g, ref b, dither);

                Assert.AreEqual(expectedR, r.ToString("X8"));
                Assert.AreEqual(expectedG, g.ToString("X8"));
                Assert.AreEqual(expectedB, b.ToString("X8"));
            }

            D(0, 0, "000000FF", "000000F0", "000000B0");
            D(0, 2, "000000FF", "000000F0", "000000AA");
            D(2, 2, "000000FF", "000000F0", "000000B0");
            D(0, 0x20, "000000FF", "000000EE", "000000AA");
        }
    }
}