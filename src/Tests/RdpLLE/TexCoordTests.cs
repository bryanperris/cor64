using System;
using cor64.Rdp.LLE;
using NUnit.Framework;

namespace Tests.RdpLLE {
    [TestFixture]
    public class TexCoordTests : BaseTest {

        [Test]
        public void TexCoordPerspTests() {

            static void Persp(int s, int t, int w, string expectedSss, string expectedSst) {
                var system = RdpHelper.CreateTestableSystem();
                var rdp = system.GetRdp();

                rdp.SetTestData();

                int sss = 0;
                int sst = 0;

                TexCoord.PerspectiveDivide(s, t, w, ref sss, ref sst);

                Assert.AreEqual(expectedSss, sss.ToString("X8"));
                Assert.AreEqual(expectedSst, sst.ToString("X8"));
            }

            Persp(0, 0, 0, "00040000", "00040000");
            Persp(10, 20, 0, "00050000", "00040000");
            Persp(10, 20, -20, "0004000A", "00040014");
            Persp(0x7FFFFFFF, 0x0EEEEEEE, 0xFF, "0001FF7F", "00036E77");
        }

        [Test]
        public void TexCoordTileTests() {
            static void MaskCopy(int s0, int s1, int s2, int s3, int t, string es0, string es1, string es2, string es3, string et) {
                var system = RdpHelper.CreateTestableSystem();
                var rdp = system.GetRdp();

                rdp.SetTestData();

                TexCoord.MaskCopy(rdp.GraphicsState.Tiles[0], ref s0, ref s1, ref s2, ref s3, ref t);

                Assert.AreEqual(es0, s0.ToString("X8"));
                Assert.AreEqual(es1, s1.ToString("X8"));
                Assert.AreEqual(es2, s2.ToString("X8"));
                Assert.AreEqual(es3, s3.ToString("X8"));
                Assert.AreEqual(et, t.ToString("X8"));
            }

            static void ShiftCycle(int s, int t,
                string es, string et, bool emaxs, bool emaxt) {
                var system = RdpHelper.CreateTestableSystem();
                var rdp = system.GetRdp();

                rdp.SetTestData();

                bool maxS = false;
                bool maxT = false;

                TexCoord.ShiftCycle(rdp.GraphicsState.Tiles[0], ref s, ref t, ref maxS, ref maxT);

                Assert.AreEqual(es, s.ToString("X8"));
                Assert.AreEqual(et, t.ToString("X8"));
                Assert.AreEqual(emaxs, maxS);
                Assert.AreEqual(emaxt, maxT);
            }

            static void ClampCycle(int s, int t, bool maxS, bool maxT, string es, string et, string efs, string eft) {
                var system = RdpHelper.CreateTestableSystem();
                var rdp = system.GetRdp();

                rdp.SetTestData();

                int fractionS = 1;
                int fractionT = 1;

                TexCoord.ClampCycle(rdp.GraphicsState.Tiles[0], ref s, ref t, ref fractionS, ref fractionT, maxS, maxT);

                Assert.AreEqual(es, s.ToString("X8"));
                Assert.AreEqual(et, t.ToString("X8"));
                Assert.AreEqual(efs, fractionS.ToString("X8"));
                Assert.AreEqual(eft, fractionT.ToString("X8"));
            }

            static void PipelineCopy(int s0, int s1, int s2, int s3, int t, string es0, string es1, string es2, string es3, string et) {
                var system = RdpHelper.CreateTestableSystem();
                var rdp = system.GetRdp();

                rdp.SetTestData();

                TexCoord.PipelineCopy(rdp.GraphicsState.Tiles[0], ref s0, ref s1, ref s2, ref s3, ref t);

                Assert.AreEqual(es0, s0.ToString("X8"));
                Assert.AreEqual(es1, s1.ToString("X8"));
                Assert.AreEqual(es2, s2.ToString("X8"));
                Assert.AreEqual(es3, s3.ToString("X8"));
                Assert.AreEqual(et, t.ToString("X8"));
            }

            static void MaskCoupled(int s, int t, string es, string et, string esdiff, string etdiff) {
                var system = RdpHelper.CreateTestableSystem();
                var rdp = system.GetRdp();

                rdp.SetTestData();

                int sdiff = 0;
                int tdiff = 0;

                TexCoord.MaskCoupled(rdp.GraphicsState.Tiles[0], ref s, ref sdiff, ref t, ref tdiff);

                Assert.AreEqual(es, s.ToString("X8"));
                Assert.AreEqual(et, t.ToString("X8"));
                Assert.AreEqual(esdiff, sdiff.ToString("X8"));
                Assert.AreEqual(etdiff, tdiff.ToString("X8"));
            }

            MaskCopy(
                5 << 5,
                7 << 4,
                9 << 2,
                11 << 3,
                13 << 5,
                "0000001F",
                "0000000F",
                "0000001B",
                "00000018",
                "0000001F"
            );

            ShiftCycle(
                0xFFF << 3,
                0xFFF << 3,
                "00003FFC",
                "00001FFE",
                true,
                true
            );

            ClampCycle(
                0xFFF << 3,
                0xFFF << 3,
                false,
                false,
                "000003FF",
                "000003FF",
                "00000001",
                "00000001"
            );

            ClampCycle(
                0xFFF << 3,
                0xFFF << 3,
                true,
                true,
                "00000019",
                "00000011",
                "00000000",
                "00000000"
            );

            PipelineCopy(
                0xFFF << 5,
                0xEEE << 5,
                0,
                0,
                0xFFF,
                "00000000",
                "00000000",
                "00000001",
                "00000002",
                "0000001F"
            );

            PipelineCopy(
                0xFFF << 7,
                0xEEE << 5,
                0,
                0,
                0xFFF,
                "00000002",
                "00000001",
                "00000000",
                "00000000",
                "0000001F"
            );

            MaskCoupled(
                0xFFF << 7,
                0xEEE << 5,
                "00000000",
                "00000000",
                "00000001",
                "00000001"
            );

            MaskCoupled(
                1,
                2,
                "00000001",
                "00000002",
                "00000001",
                "00000001"
            );
        }
    }
}