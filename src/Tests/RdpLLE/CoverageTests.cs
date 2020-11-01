using System.Text;
using System;
using cor64.Rdp.LLE;
using NUnit.Framework;

namespace Tests.RdpLLE
{
    [TestFixture]
    public class CoverageTests : BaseTest
    {

        [Test]
        public void ComputeCoverageTests()
        {

            static void ComputeCvg(bool flip, String expectedCoverage)
            {
                var system = RdpHelper.CreateTestableSystem();
                var rdp = system.GetRdp();
                var gs = rdp.GraphicsState;

                rdp.SetTestData();

                if (flip)
                {
                    rdp.TestSpanDataFlipped();
                }
                else
                {
                    rdp.TestSpanData();
                }

                Coverage.Compute(rdp.GraphicsState, 0, flip);

                StringBuilder sb = new StringBuilder();

                for (int i = 0; i <= 10; i++)
                {
                    sb.Append(gs.ConverageBuffer[i].ToString("X2"));
                }

                Assert.AreEqual(expectedCoverage, sb.ToString());
            }

            ComputeCvg(true, "000000050F5FFFFAF0A000");
            ComputeCvg(false, "000000A0F0FAFF5F0F0500");
        }

        [Test]
        public unsafe void ComputeCoverageSampleATest()
        {
            var system = RdpHelper.CreateTestableSystem();
            var rdp = system.GetRdp();
            var gs = rdp.GraphicsState;

            byte x = 0xE8;
            byte offx = 0;
            byte offy = 0;
            uint curpixel_cvg = 0;
            bool curpixel_cvbit = false;

            /*x=000000E8 lx=000000F8 rx=000000E8 sc=00000020 minorx=000007C0:000007C0:000007C0:000007C0 majorx=00000740:00000740:00000740:00000740
             oz=00000000*/

            int scanline = 0x20;

            gs.ConverageBuffer[x] = 0xFF;

            gs.SpanBuffer[scanline].rx = 0xFA;
            gs.SpanBuffer[scanline].lx = 0xE8;
            
            gs.SpanBuffer[scanline].InvalidYScan[0] = false;
            gs.SpanBuffer[scanline].InvalidYScan[1] = false;
            gs.SpanBuffer[scanline].InvalidYScan[2] = false;
            gs.SpanBuffer[scanline].InvalidYScan[3] = false;

            gs.SpanBuffer[scanline].minorx[0] = 0x7C0;
            gs.SpanBuffer[scanline].minorx[1] = 0x7C0;
            gs.SpanBuffer[scanline].minorx[2] = 0x7C0;
            gs.SpanBuffer[scanline].minorx[3] = 0x7C0;

            gs.SpanBuffer[scanline].majorx[0] = 0x740;
            gs.SpanBuffer[scanline].majorx[1] = 0x740;
            gs.SpanBuffer[scanline].majorx[2] = 0x740;
            gs.SpanBuffer[scanline].majorx[3] = 0x740;

            Coverage.Compute(gs, scanline, true);

            Coverage.LookupMaskDerivatives(gs.ConverageBuffer[x], ref offx, ref offy, ref curpixel_cvg, ref curpixel_cvbit);

            Assert.AreEqual("00000008", curpixel_cvg.ToString("X8"));
            Assert.AreEqual("000000FF", gs.ConverageBuffer[x].ToString("X8"));
            Assert.AreEqual("00000000", offx.ToString("X8"));
            Assert.AreEqual("00000000", offy.ToString("X8"));
        }
    }
}