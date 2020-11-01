using System;
using cor64.Rdp.LLE;
using NUnit.Framework;

namespace Tests.RdpLLE {
    [TestFixture]
    public class BlenderTests : BaseTest {

        [Test]
        public void BlendEquationTests() {

            static void Equation(string expectedR, string expectedG, string expectedB, Action<MadCatRdp> setup = null) {
                var system = RdpHelper.CreateTestableSystem();
                var rdp = system.GetRdp();

                rdp.SetTestData();

                setup?.Invoke(rdp);

                int r = 0;
                int g = 0;
                int b = 0;

                rdp.RdpBlender.Equation_Cycle0(ref r, ref g, ref b);

                Assert.AreEqual(expectedR, r.ToString("X8"));
                Assert.AreEqual(expectedG, g.ToString("X8"));
                Assert.AreEqual(expectedB, b.ToString("X8"));
            }

            static void Equation2(string expectedR, string expectedG, string expectedB, Action<MadCatRdp> setup = null) {
                var system = RdpHelper.CreateTestableSystem();
                var rdp = system.GetRdp();

                rdp.SetTestData();

                setup?.Invoke(rdp);

                int r = 0;
                int g = 0;
                int b = 0;

                rdp.RdpBlender.Equation_Cycle0_2(ref r, ref g, ref b);

                Assert.AreEqual(expectedR, r.ToString("X8"));
                Assert.AreEqual(expectedG, g.ToString("X8"));
                Assert.AreEqual(expectedB, b.ToString("X8"));
            }

            Equation("0000009B", "000000F2", "0000004A");

            Equation("00000026", "0000003C", "00000012", (rdp) => rdp.GraphicsState.OtherModes.EnableForceBlend = true);

            Equation2("00000026", "0000003C", "00000012");
        }

        [Test]
        public void BlendPassTests() {

            static void FramebufferPass(int dither, bool enBlend, int prewrap, uint cvg, bool cvbit, bool expectedResult, string expectedR, string expectedG, string expectedB, Action<MadCatRdp> setup = null) {
                var system = RdpHelper.CreateTestableSystem();
                var rdp = system.GetRdp();

                rdp.SetTestData();

                setup?.Invoke(rdp);

                int r = 0;
                int g = 0;
                int b = 0;

                var result = rdp.RdpBlender.FramebufferPass(0, ref r, ref g, ref b, dither, enBlend, prewrap, cvg, cvbit);

                Assert.AreEqual(expectedResult, result);
                Assert.AreEqual(expectedR, r.ToString("X8"));
                Assert.AreEqual(expectedG, g.ToString("X8"));
                Assert.AreEqual(expectedB, b.ToString("X8"));
            }

            FramebufferPass(1, false, 0, 0xAA, false, false, "00000000", "00000000", "00000000");
            FramebufferPass(1, true, 0, 0xAA, true, true, "000000A0", "000000F8", "00000050");

            static void Pass(bool coverage, bool cvbit, bool expectedResult, string expectedR, string expectedG, string expectedB, string expectedA) {
                var system = RdpHelper.CreateTestableSystem();
                var rdp = system.GetRdp();
                var gs = rdp.GraphicsState;

                rdp.SetTestData();

                var result = rdp.RdpBlender.Pass_2Cycle_Cycle0(coverage, cvbit);

                Assert.AreEqual(expectedResult, result);
                Assert.AreEqual(expectedR, rdp.RdpBlender.BlendedColor.R.ToString("X8"));
                Assert.AreEqual(expectedG, rdp.RdpBlender.BlendedColor.G.ToString("X8"));
                Assert.AreEqual(expectedB, rdp.RdpBlender.BlendedColor.B.ToString("X8"));
                Assert.AreEqual(expectedA, gs.InvPixelColor.A.ToString("X8"));
            }

            Pass(false, false, false, "00000000", "00000000", "00000000", "00000000");
            Pass(false, true, true, "00000026", "0000003C", "00000012", "000000DF");
        }
    }
}