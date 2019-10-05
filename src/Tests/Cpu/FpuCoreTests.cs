using cor64.IO;
using cor64.Mips;
using cor64.Mips.R4300I;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static cor64.Mips.FpuExceptionFlags;
using static Tests.Cpu.FpuHelper;

using static cor64.Mips.FpuValueType;
using cor64.CFloat;
using NUnit.Framework;

namespace Tests.Cpu
{
    [TestFixture]
    public class FpuCoreTests : BaseTest
    {
        private static float F(uint value)
        {
            unsafe
            {
                return *(float*)(&value);
            }
        }

        private static double D(ulong value)
        {
            unsafe
            {
                return *(double*)(&value);
            }
        }

        private static readonly CFloatInterpreter s_CFloat = new CFloatInterpreter(true);

        [Test]
        public void AdditionTest()
        {
            "add.s".FPUTest(0.0f, 0.0f).Expect(0.0f).Run();
                    FPUTest(0.0f, 1.0f).Expect(1.0f).Run();
                    FPUTest(123.123f, 111.111f).Expect(234.234f).Run();
                    FPUTest(0.0f, 1234.6789f).Expect(F(0x449A55BA)).Run();
                    FPUTest(1234.6789f, -1234.6789f).Expect(F(0)).Run();
                    FPUTest(1234.6789f, 1234.5f).Expect(F(0x451A52DD)).Run();
                    FPUTest(0.0f, -1234.6789f).Expect(F(0xC49A55BA)).Run();

            "add.d".FPUTest(0.0d, 1.0d).Expect(1.0d).Run();
                    FPUTest(12345678.67891234d, 12345678.5d).Expect(D(0x41778C29D2DCD330)).Run();
                    FPUTest(-12345678.5d, -12345678.67891234d).Expect(D(0xC1778C29D2DCD330)).Run();
        }

        [Test]
        public void SubtractTest()
        {
            "sub.s".FPUTest(0.0f, 0.0f).Expect(0.0f).Run();
                    FPUTest(1.0f, 0.0f).Expect(1.0f).Run();
                    FPUTest(0.0f, 1234.6789f).Expect(F(0xC49A55BA)).Run();
                    FPUTest(1234.1234f, -1234.1234f).Expect(F(0x451A43F3)).Run();
                    FPUTest(0.0f, -1234.6789f).Expect(F(0x449A55BA)).Run();

            "sub.d".FPUTest(0.0d, 0.0d).Expect(0.0d).Run();
                    FPUTest(1.0d, 0.0d).Expect(1.0d).Run();
                    FPUTest(12345678.67891234d, 12345678.5d).Expect(D(0x3FC6E6997C000000)).Run();
                    FPUTest(12345678.12345678d, -12345678.12345678d).Expect(D(0x41778C29C3F35BA2)).Run();
                    FPUTest(-12345678.5d, -12345678.67891234d).Expect(D(0x3FC6E6997C000000)).Run();
        }

        [Test]
        public void MultiplyTest()
        {
            "mul.s".FPUTest(0.0f, 0.0f).Expect(0.0f).Run();
                    FPUTest(1.0f, 0.0f).Expect(0.0).Run();
                    FPUTest(1234.6789f, 1234.5f).Expect(F(0x49BA0F99)).Run();
                    FPUTest(-1234.5f, -1234.6789f).Expect(F(0x49BA0F99)).Run();
                    FPUTest(0.0f, -1234.6789f).Expect(0.0f).Run();

            "mul.d".FPUTest(0.0d, 0.0d).Expect(0.0d).Run();
                    FPUTest(0.0d, 1.0d).Expect(0.0d).Run();
                    FPUTest(12345678.67891234d, 12345678.5d).Expect(D(0x42E153E20D49258F)).Run();
                    FPUTest(12345678.12345678d, -12345678.12345678d).Expect(D(0xC2E153E1F756E7EA)).Run();
                    FPUTest(-12345678.5d, -12345678.67891234d).Expect(D(0x42E153E20D49258F)).Run();
        }

        [Test]
        public void DivideTest()
        {
            "div.s".FPUTest(0.0f, 1.0f).Expect(0.0f).Run();
			        FPUTest(1234.6789f, 1234.5f).Expect(F(0x3F8004C0)).Run();
					FPUTest(1234.1234f, -1234.1234f).Expect(F(0xBF800000)).Run();
					FPUTest(-1234.5f, -1234.6789f).Expect(F(0x3F7FF680)).Run();
					FPUTest(0.0f, -1234.6789f).Expect(F(0x80000000)).Run();
                    FPUTest(1.0f, 0.0f).Except(DivideByZero).CoreOverride(typeof(CFloatInterpreter)).Run();
					
			"div.d".FPUTest(0.0d, 1.0d).Expect(0.0d).Run();
			        FPUTest(0.0d, 12345678.67891234d).Expect(0.0d).Run();
					FPUTest(12345678.5d, 12345678.12345678d).Expect(D(0x3FF00000082FF1BA)).Run();
					FPUTest(12345678.12345678d, -12345678.12345678d).Expect(D(0xBFF0000000000000)).Run();
					FPUTest(-12345678.5d, -12345678.67891234d).Expect(D(0x3FEFFFFFF8383F9A)).Run();
        }

        [Test]
        public void SqrtTest()
        {
			"sqrt.s".FPUTest(0.0f).Expect(0.0f).Run();
					 FPUTest(1234.6789f).Expect(F(0x420C8D50)).Run();
			
			"sqrt.d".FPUTest(0.0d).Expect(0.0d).Run();
			         FPUTest(12345678.67891234d).Expect(D(0x40AB734899A3F078)).Run();
        }

        [Test]
        public void AbsTest()
        {
            "abs.s".FPUTest(0.0f).Expect(0.0f).Run();
                    FPUTest(-1.0f).Expect(1.0f).Run();
                    FPUTest(-0.0f).Expect(0.0f).Run();
                    FPUTest(1234.1234f).Expect(1234.1234f).Run();
                    FPUTest(-1234.1234f).Expect(1234.1234f).Run();

            "abs.d".FPUTest(0.0d).Expect(0.0d).Run();
                    FPUTest(-1.0d).Expect(1.0d).Run();
                    FPUTest(-0.0d).Expect(0.0d).Run();
                    FPUTest(12345678.12345678d).Expect(12345678.12345678d).Run();
                    FPUTest(-12345678.12345678d).Expect(12345678.12345678d).Run();
        }

        [Test]
        public void MovTest()
        {
            "mov.s".FPUTest(1234.1234f).Expect(1234.1234f).Run();

            "mov.d".FPUTest(12345678.12345678d).Expect(12345678.12345678d).Run();
        }

        [Test]
        public void NegTest()
        {
			"neg.s".FPUTest(0.0f).Expect(0.0f).Run();
                    FPUTest(-1.0f).Expect(1.0f).Run();
			        FPUTest(1234.6789f).Expect(F(0xC49A55BA)).Run();
        }
    }
}
