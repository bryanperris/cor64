using System.Runtime.InteropServices;
using cor64;
using cor64.Mips.R4300I;
using NUnit.Framework;
using cor64.BareMetal;
using System;
using System.IO;
using System.Numerics;
using cor64.Mips.Rsp;
using cor64.IO;

namespace Tests {

    [TestFixture]
    public class VectorTests : BaseTest {


        [Test]
        public void LayoutTests() {
            RspVector vec = new RspVector();
            byte[] bytes;

            vec.QWord = new cor64.Mips.UInt128 { hi = 0x0000000000000000, lo = 0xDEADBEEFDEAFBABE };

            bytes = vec.GetRawBytesSwapped();

            Assert.AreEqual("00", bytes[00].ToString("X2"));
            Assert.AreEqual("BE", bytes[15].ToString("X2"));

            var strVecA = ((RspVector)"0000 0000 0000 0000 DEAD BEEF DEAF BABE").ToString();
            var strVecB = vec.ToString();

            Assert.AreEqual(32, strVecA.Length);
            Assert.AreEqual(32, strVecB.Length);

            Assert.AreEqual(strVecA, strVecB);


            vec.QWord = new cor64.Mips.UInt128 { hi = 0xDEADBEEFDEAFBABE, lo = 0x0000000000000000 };

            bytes = vec.GetRawBytesSwapped();

            Assert.AreEqual("DE", bytes[00].ToString("X2"));
            Assert.AreEqual("00", bytes[15].ToString("X2"));

            strVecA = ((RspVector)"DEAD BEEF DEAF BABE 0000 0000 0000 0000").ToString();
            strVecB = vec.ToString();

            Assert.AreEqual(32, strVecA.Length);
            Assert.AreEqual(32, strVecB.Length);

            Assert.AreEqual(strVecA, strVecB);

            vec.QWord = new cor64.Mips.UInt128 { hi = 0x0001000000000000, lo = 0x0000000000000000 };

            Assert.AreEqual("0001", vec.PackedU16(0).ToString("X4"));
        }

        [Test]
        public void MemoryTests() {
            var pinnedBuffer = new PinnedBuffer(3 * 16);
            var vec1 = new RspVector(pinnedBuffer, 0);
            var vec2 = new RspVector(pinnedBuffer, 1);
            var vec3 = new RspVector(pinnedBuffer, 2);

            var str1 = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA";
            var str2 = "BBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB";
            var str3 = "CCCCCCCCCCCCCCCCCCCCCCCCCCCCCCCC";

            vec1.SetFromString(str1);
            vec2.SetFromString(str2);
            vec3.SetFromString(str3);

            Assert.AreEqual(vec1.ToString(), str1);
            Assert.AreEqual(vec2.ToString(), str2);
            Assert.AreEqual(vec3.ToString(), str3);
        }

        [Test]
        public void LotsOfVectors() {
            RspVector[] vectors = new RspVector[100];

            for (int i = 0; i < vectors.Length; i++) {
                vectors[i] = new RspVector();
            }
        }
    }
}