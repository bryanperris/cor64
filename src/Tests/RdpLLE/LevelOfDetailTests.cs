using System;
using cor64.Rdp.LLE;
using NUnit.Framework;

namespace Tests.RdpLLE {
    [TestFixture]
    public class LevelOfDetailTests : BaseTest {

        [Test]
        public void LodComputeTests() {

            static void C4X17To15(int s, int t, int ns, int nt, int pre, string elevel) {
                var system = RdpHelper.CreateTestableSystem();
                var rdp = system.GetRdp();

                rdp.SetTestData();

                int level = 0;

                LevelOfDetail.Convert4X17To15(s, ns, t, nt, pre, ref level);

                Assert.AreEqual(elevel, level.ToString("X8"));
            }

            static void Clamp(int s, int t, string es, string et) {
                var system = RdpHelper.CreateTestableSystem();
                var rdp = system.GetRdp();

                rdp.SetTestData();

                LevelOfDetail.Clamp(ref s, ref t);

                Assert.AreEqual(es, s.ToString("X8"));
                Assert.AreEqual(et, t.ToString("X8"));
            }

            static void Signals(bool sharp, bool detail, bool clamped, int level, string et, bool em, bool ed, string ef) {
                var system = RdpHelper.CreateTestableSystem();
                var rdp = system.GetRdp();

                rdp.SetTestData();

                rdp.GraphicsState.OtherModes.EnableSharpenTextures = sharp;
                rdp.GraphicsState.OtherModes.EnableDetailTextures = detail;

                int lodTile = 0;
                bool magnify = false;
                bool distant = false;
                int lodFraction = 0;

                LevelOfDetail.Signals(rdp.GraphicsState, clamped, level, ref lodTile, ref magnify, ref distant, ref lodFraction);

                Assert.AreEqual(et, lodTile.ToString("X8"));
                Assert.AreEqual(em, magnify);
                Assert.AreEqual(ed, distant);
                Assert.AreEqual(ef, lodFraction.ToString("X8"));
            }

            C4X17To15(
                1,
                1,
                2,
                2,
                0,
                "00000001"
            );

            C4X17To15(
                1,
                1,
                2,
                2,
                5,
                "00000005"
            );

            C4X17To15(
                13,
                15,
                20,
                21,
                5,
                "00000007"
            );

            C4X17To15(
                113,
                115,
                20,
                28,
                10,
                "0000005C"
            );

            Clamp(0x40000, 0x20000, "00007FFF", "00008000");

            Signals(false, false, false, 2, "00000000", true, false, "00000000");
            Signals(false, false, true, 2, "00000000", false, true, "000000FF");
            Signals(false, false, false, 0x200, "00000004", false, false, "00000000");
            Signals(false, false, false, 0x2000, "00000000", false, true, "000000FF");
            Signals(true, true, false, 0x002240, "00000004", false, true, "00000020");
        }

        [Test]
        public void LodPassTests() {

            static void Pass(bool sharp, bool detail, int sss, int sst, TextureCoordinate tcoord, TextureCoordinate dinc, string esss, string esst, string et1, string et2, string elf) {
                var system = RdpHelper.CreateTestableSystem();
                var rdp = system.GetRdp();

                rdp.SetTestData();

                int t1 = 0;
                int t2 = 0;

                int lf = 0;

                rdp.GraphicsState.OtherModes.EnableDetailTextures = detail;
                rdp.GraphicsState.OtherModes.EnableSharpenTextures = sharp;

                LevelOfDetail.Pass(rdp, ref sss, ref sst, tcoord, dinc, 0, ref t1, ref t2, ref lf);

                Assert.AreEqual(esss, sss.ToString("X8"));
                Assert.AreEqual(esst, sst.ToString("X8"));
                Assert.AreEqual(et1, t1.ToString("X8"));
                Assert.AreEqual(et2, t2.ToString("X8"));
                Assert.AreEqual(elf, lf.ToString("X8"));
            }

            static void PassWithNext(bool sharp, bool detail, int sss, int sst, int sss2, int sst2, TextureCoordinate dinc, string esss, string esst, string esss2, string esst2, string et1, string et2, string elf) {
                var system = RdpHelper.CreateTestableSystem();
                var rdp = system.GetRdp();

                rdp.SetTestData();

                int t1 = 0;
                int t2 = 0;

                int lf = 0;

                rdp.GraphicsState.OtherModes.EnableDetailTextures = detail;
                rdp.GraphicsState.OtherModes.EnableSharpenTextures = sharp;

                LevelOfDetail.PassWithNext(rdp, ref sss, ref sst, ref sss2, ref sst2, dinc, 0, ref t1, ref t2, ref lf, 0);

                Assert.AreEqual(esss, sss.ToString("X8"));
                Assert.AreEqual(esst, sst.ToString("X8"));
                Assert.AreEqual(esss2, sss2.ToString("X8"));
                Assert.AreEqual(esst2, sst2.ToString("X8"));
                Assert.AreEqual(et1, t1.ToString("X8"));
                Assert.AreEqual(et2, t2.ToString("X8"));
                Assert.AreEqual(elf, lf.ToString("X8"));
            }

            Pass(false, false, 0xFFF << 5, 0xEEE << 5, new TextureCoordinate(2, 3, 4), new TextureCoordinate(1, 2, 3),"0000FFE0", "0000DDC0", "00000002", "00000002", "000000FF");
            Pass(true, true, 0xFFF << 5, 0xEEE << 5, new TextureCoordinate(2, 3, 4), new TextureCoordinate(1, 2, 3),"0000FFE0", "0000DDC0", "00000003", "00000003", "00000020");

            PassWithNext(false, false, 0xFFF << 5, 0xEEE << 5, 0xFFF << 4, 0xEEE << 4, new TextureCoordinate(1, 2, 3),"0000FFE0", "0000DDC0", "00007FFF", "00007FFF", "00000002", "00000002", "000000FF");
            PassWithNext(true, true, 0xFFF << 5, 0xEEE << 5, 0xFFF << 4, 0xEEE << 4, new TextureCoordinate(1, 2, 3),"0000FFE0", "0000DDC0", "00007FFF", "00007FFF", "00000003", "00000003", "000000FF");
        }
    }
}