using NUnit.Framework;
using cor64.Rdp;

namespace Tests {
    [TestFixture]
    public class TriangleToolTests : BaseTest {
        [Test]
        public void LeftMajorShadeTest() {
            /* Y Sorted Triangle */
            var result = TriangleTools.MajorTriangleCalc(
                new System.Tuple<double, double>(300.0d, 200.0d),
                new System.Tuple<double, double>(250.0d, 200.0d),
                new System.Tuple<double, double>(275.0d, 150.0d),
                TriangleTools.TriangleType.Shade);

            Assert.AreEqual("Shade_Triangle 0, 0, 0, 800,800,600, 250,0, 0,0, 275,0, 0,32768, 275,0, -1,32768",
            result);
        }

        [Test]
        public void RightMajorShadeTest() {
            /* Y Sorted Triangle */
            var result = TriangleTools.MajorTriangleCalc(
                new System.Tuple<double, double>(250.0d, 200.0d),
                new System.Tuple<double, double>(300.0d, 200.0d),
                new System.Tuple<double, double>(275.0d, 150.0d),
                TriangleTools.TriangleType.Shade,
                true);

            Assert.AreEqual("Shade_Triangle 1, 0, 0, 800,800,600, 300,0, 0,0, 275,0, -1,32768, 275,0, 0,32768",
            result);
        }
    }
}