using System;
using cor64.Rdp.LLE;
using NUnit.Framework;

namespace Tests.RdpLLE {
    [TestFixture]
    public class CombinerTests : BaseTest {

        [Test]
        public void InputTest() {
            var system = RdpHelper.CreateTestableSystem();
            var rdp = system.GetRdp();

            rdp.GraphicsState.EnvironmentColor.SetColor(new Color(0xDE, 0xAD, 0xBA, 0xBE));

            system.AssembleAnInjectDL("Set_Combine_Mode 5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5");

            system.RdpExecute();

            Assert.AreEqual("DEADBABE", rdp.RdpCombiner.S0.SubtractA.GetColor().ToString());
        }

        [Test]
        public void EquationTests() {
            var system = RdpHelper.CreateTestableSystem();
            var rdp = system.GetRdp();

            var result = rdp.RdpCombiner.ColorEquation(0xDE, 0xAD, 0xBA, 0xBE);
            Assert.AreEqual("0000E21A", result.ToString("X8"));

            result = rdp.RdpCombiner.AlphaEquation(0xDE, 0xAD, 0xBA, 0xBE);
            Assert.AreEqual("000000E2", result.ToString("X8"));
        }

        [Test]
        public void SignTests() {
            Assert.AreEqual("FFFFFFFF", Utils.SignF(0xFFFF, 9).ToString("X8"));
            Assert.AreEqual("FFFFFFFF", Utils.Sign(0xFFFF, 5).ToString("X8"));
        }

        [Test]
        public void TableTests() {
            Assert.AreEqual("000000DE", Combiner.LookupExt(0xDE).ToString("X8"));
        }

        [Test]
        public void ChromaKeyTests() {
            var system = RdpHelper.CreateTestableSystem();
            var rdp = system.GetRdp();

            rdp.GraphicsState.KeyWidth.R = 0xDE;
            rdp.GraphicsState.KeyWidth.G = 0xAD;
            rdp.GraphicsState.KeyWidth.B = 0xBA;

            Color c = new Color(0xEF, 0xAA, 0xFF, 00);

            int result = rdp.RdpCombiner.ChromaKeyMin(c);
            Assert.AreEqual("000000FF", result.ToString("X8"));
        }

        [Test]
        public void CombinerPassTests() {
            var system = RdpHelper.CreateTestableSystem();
            var rdp = system.GetRdp();
            var om = rdp.GraphicsState.OtherModes;

            void Pass_0(int aseed, int cvg, String expectedAcAlpha, String expectedColor) {
                rdp.SetTestData();
                rdp.RdpCombiner.UpdateCombineMode();

                uint acalpha = 0;

                rdp.RdpCombiner.Pass_2Cycle_Cycle0(aseed, cvg, ref acalpha);

                Assert.AreEqual(expectedAcAlpha, acalpha.ToString("X8"));
                Assert.AreEqual(expectedColor, rdp.GraphicsState.CombinedColor.GetColor().ToString());
            }

            void Pass_1(int aseed, String expectedCvg, String expectedColor, String expectedPixelColor) {
                rdp.SetTestData();
                rdp.RdpCombiner.UpdateCombineMode();

                uint cvg = 0;

                rdp.RdpCombiner.Pass_2Cycle_Cycle0(aseed, ref cvg);

                Assert.AreEqual(expectedCvg, cvg.ToString("X8"));
                Assert.AreEqual(expectedColor, rdp.GraphicsState.CombinedColor.GetColor().ToString());
                Assert.AreEqual(expectedPixelColor, rdp.GraphicsState.PixelColor.GetColor().ToString());
            }

            Pass_0(0, 0, "00000000", "B4D440F0");

            om.EnableAlphaCompare = true;

            Pass_0(1, 0, "000000F1", "B4D440F0");

            om.AlphaCoverageSelect = true;

            Pass_0(1, 2, "00000040", "B4D440F0");

            om.CoverageTimesAlpha = true;

            Pass_0(1, 2, "0000003C", "B4D440F0");

            om.EnableAlphaCompare = false;
            om.AlphaCoverageSelect = false;
            om.CoverageTimesAlpha = false;

            Pass_1(1, "00000000", "D4AF40FF", "D4AF40FF");

            om.EnableKey = true;

            Pass_1(1, "00000000", "D4AF40FF", "D4AF4000");
        }
    }
}