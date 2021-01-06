using cor64.Mips.Rsp;
using NUnit.Framework;
using static Tests.RspTestHelper;

namespace Tests
{
    [TestFixture]
    public class RspTests : BaseTest
    {
        private static void RspMultiplyAccTest(
            RspVector a,
            RspVector b,
            RspVector c,
            RspVector d,
            RspVector e,
            RspVector f,
            RspVector g,
            RspVector h,
            RspVector i
        ) {
            RspTest(a, b).Expect(c).InjectAccumulator(d,e,f).ExpectAccumulator(g,h,i).Run();
        }

        private static void RspMultiplyTest(
            RspVector a,
            RspVector b,
            RspVector c,
            RspVector d,
            RspVector e,
            RspVector f
        ) {
            RspTest(a, b).Expect(c).ExpectAccumulator(d,e,f).Run();
        }

        [Test]
        public void AccumulatorTests() {
            Accumulator accumulator;

            accumulator = new Accumulator(
                "AABBAABBAABBAABBAABBAABBAABBAABB",
                "AABBAABBAABBAABBAABBAABBAABBAABB",
                "AABBAABBAABBAABBAABBAABBAABBAABB"
            );

            Assert.AreEqual("AABB", accumulator.Hi(0).ToString("X4"));

            accumulator.Hi(0, 0xEEFF);

            Assert.AreEqual("EEFF", accumulator.Hi(0).ToString("X4"));
        }

        [Test]
        public void RspRegTests() {
            var carry = new RspCarryFlag();
            carry.SetNotEqualBit(0, false);
            carry.SetCarryBit(0, 1);

            Assert.AreEqual("0001", carry.RegValue.ToString("X4"));
        }

        [Test]
        public void ClampTestsUnsigned() {
            Accumulator accumulator = null;
            RspVector clampedVector = null;

            accumulator = new Accumulator(
                "00000000000000000000000000000000",
                "0000124648F6A40DDEC1719A28DB0484",
                "04CCE174223CC724D02C3D540E9C4404"
            );

            clampedVector = new RspVector();

            accumulator.ProductUnsignedClamp(clampedVector);

            Assert.AreEqual("0000124648F6FFFFFFFF719A28DB0484", clampedVector.ToString());


            accumulator = new Accumulator(
                "0000FFFFFFFFFFFFFFFFFFFFFFFFFFFF",
                "0000F6DDDB85ADF9909FC733EB92FDBE",
                "7D9C0F486EE49C7097EC6158F8B45E00"
            );

            clampedVector = new RspVector();

            accumulator.ProductUnsignedClamp(clampedVector);

            Assert.AreEqual("00000000000000000000000000000000", clampedVector.ToString());
        }

        [Test]
        public void ClampTestsMi() {
            Accumulator accumulator = null;
            RspVector clampedVector = null;

            accumulator = new Accumulator(
                "00000491123D290237B11C670A370121",
                "010F78861220CDDC232000FE00FE2321",
                "0133B85D888F71C9740B8F55C3A71101"
            );

            clampedVector = new RspVector();

            accumulator.ProductSignedClampMi(clampedVector);

            Assert.AreEqual("010F7FFF7FFF7FFF7FFF7FFF7FFF7FFF", clampedVector.ToString());

            // accumulator = new Accumulator(
            //     "00000000000000000000000000000000",
            //     "00000000000000000000000000000000",
            //     "0023446788ABCCEF1133557799BBDDFF"
            // );

            // clampedVector = new RspVector();

            // TTODO: ADD signed clamp

            // Assert.AreEqual("002344677FFF7FFF8000800099BBDDFF", clampedVector.ToString());
        }

        [Test]
        public void ClampTestsLo() {
            Accumulator accumulator = null;
            RspVector clampedVector = null;

            accumulator = new Accumulator(
                "00000000000000000000000000000000",
                "00000000000000000000000000000000",
                "00000491123D290348E271DCA3F0DF1D"
            );

            clampedVector = new RspVector();

            accumulator.ProductSignedClampLo(clampedVector);

            Assert.AreEqual("00000491123D290348E271DCA3F0DF1D", clampedVector.ToString());
        }

        [Test]
        public void VectorElementTests() {
            RspVector vec = new RspVector();
            vec.SetFromString("00112233445566778899AABBCCDDEEFF");

            Assert.AreEqual("001100114455445588998899CCDDCCDD", RspVector.ScalarQuarter(vec, 0).ToString());
            Assert.AreEqual("00110011001100118899889988998899", RspVector.ScalarHalf(vec, 0).ToString());
            Assert.AreEqual("00110011001100110011001100110011", RspVector.ScalarWhole(vec, 0).ToString());
        }

        [Test]
        public void VectorAddTests() {
            "UCodes.VADD.asm".SetTargetUCode(17 * 4);
            
            RspTest().Expect("0000 0000 0000 0000 0000 0000 0000 0000").Run();

            RspTest(
                "0001 0000 0000 0000 0000 0000 0000 0000", 
                "0000 0000 0000 0000 0000 0000 0000 0000")
                .Expect("0001 0000 0000 0000 0000 0000 0000 0000").Run();

            RspTest(
                "$0000, $0000, $0000, $0000, $0000, $0000, $0000, $0000", 
                "$0011, $2233, $4455, $6677, $8899, $AABB, $CCDD, $EEFF")
            .ExpectAccumulator(
                "$0000, $0000, $0000, $0000, $0000, $0000, $0000, $0000",
                "$0000, $0000, $0000, $0000, $0000, $0000, $0000, $0000",
                "$0011, $2233, $4455, $6677, $8899, $AABB, $CCDD, $EEFF"
            )
            .ExpectCarry(0)
            .Expect("$0011, $2233, $4455, $6677, $8899, $AABB, $CCDD, $EEFF").Run();


            "UCodes.VADDC.asm".SetTargetUCode(17 * 4);

            RspTest(
                "0011 2233 4455 6677 8899 AABB CCDD EEFF",
                "0011 2233 4455 6677 8899 AABB CCDD EEFF")
            .ExpectAccumulator(
                "$0000, $0000, $0000, $0000, $0000, $0000, $0000, $0000",
                "$0000, $0000, $0000, $0000, $0000, $0000, $0000, $0000",
                "$0022, $4466, $88AA, $CCEE, $1132, $5576, $99BA, $DDFE"
            )
            .ExpectCarry(0xF0)
            .Expect("$0022, $4466, $88AA, $CCEE, $1132, $5576, $99BA, $DDFE").Run();
        }

        [Test]
        public void VectorSubtractTests() {
            "UCodes.VSUB.asm".SetTargetUCode(17 * 4);

            RspTest(
                "0001 0000 0000 0000 0000 0000 0000 0000", 
                "0000 0000 0000 0000 0000 0000 0000 0000")
                .Expect("0001 0000 0000 0000 0000 0000 0000 0000").Run();

            RspTest(
                "$0000, $0000, $0000, $0000, $0000, $0000, $0000, $0000",
                "$0011, $2233, $4455, $6677, $8899, $AABB, $CCDD, $EEFF")
            .ExpectAccumulator(
                "$0000, $0000, $0000, $0000, $0000, $0000, $0000, $0000",
                "$0000, $0000, $0000, $0000, $0000, $0000, $0000, $0000",
                "$FFEF, $DDCD, $BBAB, $9989, $7767, $5545, $3323, $1101"
            )
            .ExpectCarry(0)
            .Expect("$FFEF, $DDCD, $BBAB, $9989, $7767, $5545, $3323, $1101").Run();


            "UCodes.VSUBC.asm".SetTargetUCode(17 * 4);

            RspTest(
                "00112233445566778899AABBCCDDEEFF",
                "FFEEDDCCBBAA99887766554433221100")
            .ExpectAccumulator(
                "$0000, $0000, $0000, $0000, $0000, $0000, $0000, $0000",
                "$0000, $0000, $0000, $0000, $0000, $0000, $0000, $0000",
                "$0023, $4467, $88AB, $CCEF, $1133, $5577, $99BB, $DDFF"
            )
            .ExpectCarry(0xFF0F)
            .Expect("$0023, $4467, $88AB, $CCEF, $1133, $5577, $99BB, $DDFF").Run();
        }

        [Test]
        public void VectorMultiplyTests() {
            "UCodes.VMULF.asm".SetTargetUCode(17 * 4);

            RspTest(
                "$0000, $0000, $0000, $0000, $0000, $0000, $0000, $0000",
                "$0011, $2233, $4455, $6677, $8899, $AABB, $CCDD, $EEFF")
            .Expect(
                "$0000, $0000, $0000, $0000, $0000, $0000, $0000, $0000")
            .ExpectAccumulator(
                "$0000, $0000, $0000, $0000, $0000, $0000, $0000, $0000",
                "$0000, $0000, $0000, $0000, $0000, $0000, $0000, $0000",
                "$8000, $8000, $8000, $8000, $8000, $8000, $8000, $8000"
            )
            .Run();

            RspTest(
                "$0011, $2233, $4455, $6677, $8899, $AABB, $CCDD, $EEFF",
                "$0011, $2233, $4455, $6677, $8899, $AABB, $CCDD, $EEFF")
            .Expect(
                "$0000, $0923, $247B, $5206, $6F62, $38CE, $146E, $0242")
            .ExpectAccumulator(
                "$0000, $0000, $0000, $0000, $0000, $0000, $0000, $0000",
                "$0000, $0923, $247B, $5206, $6F62, $38CE, $146E, $0242",
                "$8242, $AC52, $0872, $96A2, $56E2, $4932, $6D92, $C402"
            )
            .Run();

            RspTest(
                "$0011, $2233, $4455, $6677, $8899, $AABB, $CCDD, $EEFF",
                "$FFEE, $DDCC, $BBAA, $9988, $7766, $5544, $3322, $1100")
            .Expect(
                "$0000, $F6DD, $DB85, $ADF9, $909F, $C733, $EB92, $FDBE")
            .ExpectAccumulator(
                "$0000, $FFFF, $FFFF, $FFFF, $FFFF, $FFFF, $FFFF, $FFFF",
                "$0000, $F6DD, $DB85, $ADF9, $909F, $C733, $EB92, $FDBE",
                "$7D9C, $0F48, $6EE4, $9C70, $97EC, $6158, $F8B4, $5E00"
            )
            .Run();

            "UCodes.VMULU.asm".SetTargetUCode(17 * 4);

             RspTest(
                "00112233445566778899AABBCCDDEEFF",
                "FFEEDDCCBBAA99887766554433221100")
            .Expect(
                "00000000000000000000000000000000")
            .ExpectAccumulator(
                "0000FFFFFFFFFFFFFFFFFFFFFFFFFFFF",
                "0000F6DDDB85ADF9909FC733EB92FDBE",
                "7D9C0F486EE49C7097EC6158F8B45E00"
            )
            .Run();

            "UCodes.VMACU.asm".SetTargetUCode(17 * 4);

             RspTest(
                "00112233445566778899AABBCCDDEEFF",
                "00112233445566778899AABBCCDDEEFF")
            .Expect(
                "0000124648F5FFFFFFFF719A28DB0484")
            .InjectAccumulator(
                "00000000000000000000000000000000",
                "00000923247B52066F6038CD146D0242",
                "026670BA111EE392E8161EAA874E2202"
            )
            .ExpectAccumulator(
                "00000000000000000000000000000000",
                "0000124648F5A40CDEC2719A28DB0484",
                "04A89D0C9990FA34BEF8E7DC74E06604"
            )
            .Run();

            "vmudl".GenerateAndSetTargetUCode();

            RspMultiplyTest(
                "00112233445566778899AABBCCDDEEFF",
                "00112233445566778899AABBCCDDEEFF",
                "00000491123D290348E271DCA3F0DF1F",
                "00000000000000000000000000000000",
                "00000000000000000000000000000000",
                "00000491123D290348E271DCA3F0DF1F"
            );

            "vmudm".GenerateAndSetTargetUCode();

            RspMultiplyTest(
                "00112233445566778899AABBCCDDEEFF",
                "FFEEDDCCBBAA99887766554433221100",
                "00101DA132173D73C84FE399F5C9FEDE",
                "0000000000000000FFFFFFFFFFFFFFFF",
                "00101DA132173D73C84FE399F5C9FEDE",
                "FECE47A477728E388BF670AC3C5AEF00"
            );

            "vmadl".GenerateAndSetTargetUCode();

            RspMultiplyAccTest(
                "FFEEDDCCBBAA99887766554433221100",
                "FFEEDDCCBBAA99887766554433221100",
                "FFECE25BCDE5C289C046C71FD711F01E",
                "00000000000000000000000000000000",
                "00000000000000000000000000000000",
                "00102232445466768897AAB9CCDBEEFD",
                "00000000000000000000000000000000",
                "00000000000000000000000000000000",
                "FFECE25BCDE5C289C046C71FD711F01E"
            );

            "vmadm".GenerateAndSetTargetUCode();

            RspMultiplyAccTest(
                "FFEEDDCCBBAA99887766554433221100",
                "FFEEDDCCBBAA99887766554433221100",
                "00000491123D2903C04AC722D714F021",
                "0000000000000000FFFFFFFFFFFFFFFF",
                "0011223344556677889AAABCCCDEEF00",
                "FFDBC02889905C1237AD1C640A34011F",
                "0000000000000000FFFFFFFFFFFFFFFF",
                "00000491123D2903C04AC722D714F021",
                "011F9AB856743452345156749AB8011F"
            );

            "vmudn".GenerateAndSetTargetUCode();

            RspMultiplyTest(
                "FFEEDDCCBBAA99887766554433221100",
                "FFEEDDCCBBAA99887766554433221100",
                "0144DA90CCE4D840FCA43A1090840000",
                "FFFFFFFFFFFFFFFF0000000000000000",
                "FFEEE25DCDE7C28B37AF1C660A360121",
                "0144DA90CCE4D840FCA43A1090840000"
            );

            "vmudh".GenerateAndSetTargetUCode();

            RspMultiplyTest(
                "FFEEDDCCBBAA99887766554433221100",
                "FFEEDDCCBBAA99887766554433221100",
                "01447FFF7FFF7FFF7FFF7FFF7FFF7FFF",
                "00000491123D290337AF1C660A360121",
                "0144DA90CCE4D840FCA43A1090840000",
                "00000000000000000000000000000000"
            );

            "vmadn".GenerateAndSetTargetUCode();

            RspMultiplyAccTest(
                "FFEEDDCCBBAA99887766554433221100",
                "FFEEDDCCBBAA99887766554433221100",
                "0133B85D888F71C9740B8F55C3A71101",
                "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF",
                "FFFFFFFFFFFFFFFFFFFFFFFFFFFFFFFF",
                "FFEFDDCDBBAB99897767554533231101",
                "FFFFFFFFFFFFFFFF0000000000000000",
                "FFEEE25DCDE7C28B37AF1C650A350120",
                "0133B85D888F71C9740B8F55C3A71101"
            );

            "vmadh".GenerateAndSetTargetUCode();

            RspMultiplyAccTest(
                "FFEEDDCCBBAA99887766554433221100",
                "FFEEDDCCBBAA99887766554433221100",
                "01217FFF7FFF7FFF7FFF7FFF7FFF7FFF",
                "FFFFFFFFFFFFFFFF0000000000000000",
                "FFDDC02A89925C14AF1671AA3D581221",
                "0133B85D888F71C9740B8F55C3A71101",
                "00000491123D290337B01C660A360121",
                "01219ABA56763454ABBAABBACDDC1221",
                "0133B85D888F71C9740B8F55C3A71101"
            );
        }

        [Test]
        public void VectorDivideTests() {
            "vrcp".GenerateAndSetTargetUCode(Asm.RspUCodeTestType.VectTargetDest);

            RspTest(
                "00000000000000000000000000000000",
                "00112233445566778899AABBCCDDEEFF"
            )
            .ExpectAccumulator(
                "00000000000000000000000000000000",
                "00000000000000000000000000000000",
                "00112233445566778899AABBCCDDEEFF"
            )
            .Expect("84000000000000000000000000000000").Run();

            RspTest(
                "00112233445566778899AABBCCDDEEFF",
                "FFEEDDCCBBAA99887766554433221100"
            )
            .ExpectAccumulator(
                "00000000000000000000000000000000",
                "00000000000000000000000000000000",
                "FFEEDDCCBBAA99887766554433221100"
            )
            .Expect("8FFF2233445566778899AABBCCDDEEFF").Run();


            "vrsq".GenerateAndSetTargetUCode(Asm.RspUCodeTestType.VectTargetDest);

            RspTest(
                "00000000000000000000000000000000",
                "00112233445566778899AABBCCDDEEFF"
            )
            .ExpectAccumulator(
                "00000000000000000000000000000000",
                "00000000000000000000000000000000",
                "00112233445566778899AABBCCDDEEFF"
            )
            .Expect("60000000000000000000000000000000").Run();

            RspTest(
                "00112233445566778899AABBCCDDEEFF",
                "FFEEDDCCBBAA99887766554433221100"
            )
            .ExpectAccumulator(
                "00000000000000000000000000000000",
                "00000000000000000000000000000000",
                "FFEEDDCCBBAA99887766554433221100"
            )
            .Expect("8FFF2233445566778899AABBCCDDEEFF").Run();

            "vrcpl".GenerateAndSetTargetUCode(Asm.RspUCodeTestType.VectTargetDest);

            RspTest(
                "00000000000000000000000000000000",
                "00112233445566778899AABBCCDDEEFF"
            )
            .ExpectAccumulator(
                "00000000000000000000000000000000",
                "00000000000000000000000000000000",
                "00112233445566778899AABBCCDDEEFF"
            )
            .Expect("84000000000000000000000000000000").Run();

            RspTest(
                "00112233445566778899AABBCCDDEEFF",
                "FFEEDDCCBBAA99887766554433221100"
            )
            .ExpectAccumulator(
                "00000000000000000000000000000000",
                "00000000000000000000000000000000",
                "FFEEDDCCBBAA99887766554433221100"
            )
            .Expect("8FFF2233445566778899AABBCCDDEEFF").Run();


            "vrsql".GenerateAndSetTargetUCode(Asm.RspUCodeTestType.VectTargetDest);

            RspTest(
                "00000000000000000000000000000000",
                "00112233445566778899AABBCCDDEEFF"
            )
            .ExpectAccumulator(
                "00000000000000000000000000000000",
                "00000000000000000000000000000000",
                "00112233445566778899AABBCCDDEEFF"
            )
            .Expect("60000000000000000000000000000000").Run();

            RspTest(
                "00112233445566778899AABBCCDDEEFF",
                "FFEEDDCCBBAA99887766554433221100"
            )
            .ExpectAccumulator(
                "00000000000000000000000000000000",
                "00000000000000000000000000000000",
                "FFEEDDCCBBAA99887766554433221100"
            )
            .Expect("8FFF2233445566778899AABBCCDDEEFF").Run();

            "vrcph".GenerateAndSetTargetUCode(Asm.RspUCodeTestType.VectTargetDest);

            RspTest(
                "00000000000000000000000000000000",
                "00112233445566778899AABBCCDDEEFF"
            )
            .ExpectAccumulator(
                "00000000000000000000000000000000",
                "00000000000000000000000000000000",
                "00112233445566778899AABBCCDDEEFF"
            )
            .Expect("00000000000000000000000000000000").Run();

            RspTest(
                "00112233445566778899AABBCCDDEEFF",
                "FFEEDDCCBBAA99887766554433221100"
            )
            .ExpectAccumulator(
                "00000000000000000000000000000000",
                "00000000000000000000000000000000",
                "FFEEDDCCBBAA99887766554433221100"
            )
            .Expect("00002233445566778899AABBCCDDEEFF").Run();

            RspTest(
                "FFEEDDCCBBAA99887766554433221100",
                "FFEEDDCCBBAA99887766554433221100"
            )
            .ExpectAccumulator(
                "00000000000000000000000000000000",
                "00000000000000000000000000000000",
                "FFEEDDCCBBAA99887766554433221100"
            )
            .Expect("0000DDCCBBAA99887766554433221100").Run();

            "vrsqh".GenerateAndSetTargetUCode(Asm.RspUCodeTestType.VectTargetDest);

            RspTest(
                "00000000000000000000000000000000",
                "00112233445566778899AABBCCDDEEFF"
            )
            .ExpectAccumulator(
                "00000000000000000000000000000000",
                "00000000000000000000000000000000",
                "00112233445566778899AABBCCDDEEFF"
            )
            .Expect("00000000000000000000000000000000").Run();

            RspTest(
                "00112233445566778899AABBCCDDEEFF",
                "FFEEDDCCBBAA99887766554433221100"
            )
            .ExpectAccumulator(
                "00000000000000000000000000000000",
                "00000000000000000000000000000000",
                "FFEEDDCCBBAA99887766554433221100"
            )
            .Expect("00002233445566778899AABBCCDDEEFF").Run();

            RspTest(
                "FFEEDDCCBBAA99887766554433221100",
                "FFEEDDCCBBAA99887766554433221100"
            )
            .ExpectAccumulator(
                "00000000000000000000000000000000",
                "00000000000000000000000000000000",
                "FFEEDDCCBBAA99887766554433221100"
            )
            .Expect("0000DDCCBBAA99887766554433221100").Run();
        }

        [Test]
        public void VectorCompareTests() {
            "veq".GenerateAndSetTargetUCode();

            RspTest(
                "00000000000000000000000000000000",
                "00112233445566778899AABBCCDDEEFF"
            )
            .ExpectAccumulator(
                "00000000000000000000000000000000",
                "00000000000000000000000000000000",
                "00112233445566778899AABBCCDDEEFF"
            )
            .Expect("00112233445566778899AABBCCDDEEFF").Run();

            "vne".GenerateAndSetTargetUCode();

            RspTest(
                "00000000000000000000000000000000",
                "00112233445566778899AABBCCDDEEFF"
            )
            .ExpectAccumulator(
                "00000000000000000000000000000000",
                "00000000000000000000000000000000",
                "00000000000000000000000000000000"
            )
            .ExpectCompare(0x00FF)
            .Expect("00000000000000000000000000000000").Run();

            "vlt".GenerateAndSetTargetUCode();

            RspTest(
                "00000000000000000000000000000000",
                "00112233445566778899AABBCCDDEEFF"
            )
            .ExpectAccumulator(
                "00000000000000000000000000000000",
                "00000000000000000000000000000000",
                "00000000000000008899AABBCCDDEEFF"
            )
            .ExpectCompare(0x000F)
            .Expect("00000000000000008899AABBCCDDEEFF").Run();

            "vge".GenerateAndSetTargetUCode();

            RspTest(
                "00000000000000000000000000000000",
                "00112233445566778899AABBCCDDEEFF"
            )
            .ExpectAccumulator(
                "00000000000000000000000000000000",
                "00000000000000000000000000000000",
                "00112233445566770000000000000000"
            )
            .ExpectCompare(0x00F0)
            .Expect("00112233445566770000000000000000").Run();

            "vge".GenerateAndSetTargetUCode();

            RspTest(
                "00112233445566778899AABBCCDDEEFF",
                "FFEEDDCCBBAA99887766554433221100"
            )
            .ExpectAccumulator(
                "00000000000000000000000000000000",
                "00000000000000000000000000000000",
                "00112233445566777766554433221100"
            )
            .ExpectCompare(0x000F)
            .Expect("00112233445566777766554433221100").Run();
        }

        [Test]
        public void VectorClipTests() {
            "vcl".GenerateAndSetTargetUCode();

            RspTest(
                "00000000000000000000000000000000",
                "00112233445566778899AABBCCDDEEFF"
            )
            .ExpectAccumulator(
                "00000000000000000000000000000000",
                "00000000000000000000000000000000",
                "00000000000000000000000000000000"
            )
            .Expect("00000000000000000000000000000000").Run();

            RspTest(
                "00112233445566778899AABBCCDDEEFF",
                "00112233445566778899AABBCCDDEEFF"
            )
            .ExpectAccumulator(
                "00000000000000000000000000000000",
                "00000000000000000000000000000000",
                "00112233445566778899AABBCCDDEEFF"
            )
            .ExpectCompare(0xFF00)
            .Expect("00112233445566778899AABBCCDDEEFF").Run();

            "vch".GenerateAndSetTargetUCode();

            RspTest(
                "00000000000000000000000000000000",
                "00112233445566778899AABBCCDDEEFF"
            )
            .ExpectAccumulator(
                "00000000000000000000000000000000",
                "00000000000000000000000000000000",
                "00000000000000007767554533231101"
            )
            .ExpectCarry(0xFFF0)
            .ExpectCompare(0xF0F0)
            .Expect("00000000000000007767554533231101").Run();

            RspTest(
                "00112233445566778899AABBCCDDEEFF",
                "FFEEDDCCBBAA99887766554433221100"
            )
            .ExpectAccumulator(
                "00000000000000000000000000000000",
                "00000000000000000000000000000000",
                "0012223444566678889AAABCCCDEEF00"
            )
            .ExpectCarry(0x00FF)
            .ExpectCompare(0x0FFF)
            .ExpectExtension(0xFF)
            .Expect("0012223444566678889AAABCCCDEEF00").Run();

            RspTest(
                "00112233445566778899AABBCCDDEEFF",
                "00112233445566778899AABBCCDDEEFF"
            )
            .ExpectAccumulator(
                "00000000000000000000000000000000",
                "00000000000000000000000000000000",
                "00112233445566778899AABBCCDDEEFF"
            )
            .ExpectCompare(0xFFF0)
            .Expect("00112233445566778899AABBCCDDEEFF").Run();

            "vcr".GenerateAndSetTargetUCode();

            RspTest(
                "00000000000000000000000000000000",
                "00112233445566778899AABBCCDDEEFF"
            )
            .ExpectAccumulator(
                "00000000000000000000000000000000",
                "00000000000000000000000000000000",
                "00000000000000007766554433221100"
            )
            .ExpectCompare(0xF0F0)
            .Expect("00000000000000007766554433221100").Run();


            "vmrg".GenerateAndSetTargetUCode();

            RspTest(
                "00000000000000000000000000000000",
                "00112233445566778899AABBCCDDEEFF"
            )
            .InjectVcc(0xFF0F)
            .ExpectAccumulator(
                "00000000000000000000000000000000",
                "00000000000000000000000000000000",
                "00000000000000008899AABBCCDDEEFF"
            )
            .ExpectCompare(0xFF0F)
            .Expect("00000000000000008899AABBCCDDEEFF").Run();
        }
    }
}