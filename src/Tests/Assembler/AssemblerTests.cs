using cor64;
using cor64.BassSharp;
using cor64.BassSharp.Table;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Text;
using static cor64.BassSharp.Bass;

namespace Tests.Assembler
{
    [TestFixture]
    public class AssemblerTests : BaseTest
    {
        [Test]        
        public void EmptyTest()
        {
            var a = new AssemblyTextSource("main");
            a += "//Empty assembly test";
            var result = Asm.Assemble(a);
            Assert.IsEmpty(result);
        }

        [Test]        
        public void EmptyArchTest()
        {
            var a = new AssemblyTextSource("main");
            a += "//Empty assembly test";
            a += "arch n64.cpu";
            var result = Asm.Assemble(a);
            Assert.IsEmpty(result);
        }

        [Test]        
        public void EmptyIncludeTest()
        {
            var a = new AssemblyTextSource("main");
            a += "//Empty assembly test";
            a += "include \"test\"";
            var result = Asm.Assemble(a);
            Assert.IsEmpty(result);
        }

        [Test]        
        public void OutputTest()
        {
            var a = new AssemblyTextSource("main");
            a += "output \"Sample.N64\", create";
            Asm.Assemble(a);
        }

        [Test]        
        public void FillTest()
        {
            var a = new AssemblyTextSource("main");
            a += "fill 0xDEAD";
            var result = Asm.Assemble(a);
            Assert.AreEqual(0xDEAD, result.Length);
        }

        [Test]        
        public void IncludeTest()
        {
            var a = new AssemblyTextSource("main");
            a += "include \"file\"";
            Asm.Assemble(a);
        }

        [Test]        
        public void SimpleProgram()
        {
            var a = new AssemblyTextSource("main");
            a += "arch n64.cpu";
            a += "endian msb";
            a += "origin $00000000";
            a += "base $80000000 // Entry Point Of Code";
            a += "nop";
            var result = Asm.Assemble(a);
            Assert.AreEqual(4, result.Length);
            Assert.AreEqual(new byte[] { 0, 0, 0, 0 }, result);
        }

        [Test]
        public void ImmInstruction() {
            var a = new AssemblyTextSource("main");
            a += "arch n64.cpu";
            a += "endian msb";
            a += "origin $00000000";
            a += "base $80000000 // Entry Point Of Code";
            a += "lui 8, $DEAD";
            
            Assert.DoesNotThrow(() => {
                Asm.Assemble(a);
            }, "could not assemble");
        }

        [Test]        
        public void EndianTest()
        {
            var a = new AssemblyTextSource("main");
            a += "endian msb";
            var result = Asm.AssembleAndPeek(a);
            Assert.AreEqual(Endian.MSB, result.Endianess);

            a = new AssemblyTextSource("main");
            a += "endian lsb";
            result = Asm.AssembleAndPeek(a);
            Assert.AreNotEqual(Endian.MSB, result.Endianess);
        }

        [Test]        
        public void OffsetTest()
        {
            var a = new AssemblyTextSource("main");
            a += "base 5";
            a += "origin $DEAD";
            var result = Asm.AssembleAndPeek(a);

            Assert.AreEqual(0xDEAD, result.Origin);
            Assert.AreEqual(5, result.Base);
            Assert.AreEqual(0xDEAD + 5, result.Pc);
        }

        [Test]        
        public void MacroTest()
        {
            var a = new AssemblyTextSource("main");
            a += "macro foo(val) {";
            a += "db {val}";
            a += "db 0xE";
            a += "db 0xA";
            a += "db {val}";
            a += "}";
            a += "foo(0xD)";

            var result = Asm.AssembleAndPeek(a);
            Assert.AreEqual(1, result.MacroCount);

            var result2 = Asm.Assemble(a);
            Assert.AreEqual(4, result2.Length);
            Assert.AreEqual(new byte[] { 0xD, 0xE, 0xA, 0xD }, result2);
        }

        [Test]        
        public void DefineTest()
        {
            var a = new AssemblyTextSource("main");
            a += "endian msb";
            a += "define foo($DEAF)";
            a += "dw {foo}";
            var result = Asm.Assemble(a);
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(new byte[] { 0xDE, 0xAF }, result);
        }

        [Test]        
        public void ConstantTest()
        {
            var a = new AssemblyTextSource("main");
            a += "endian msb";
            a += "constant foo($DEAF)";
            a += "dw foo";
            var result = Asm.Assemble(a);
            Assert.AreEqual(2, result.Length);
            Assert.AreEqual(new byte[] { 0xDE, 0xAF }, result);
        }

        [Test]        
        public void ConstantStringTest()
        {
            var a = new AssemblyTextSource("main");
            a += "endian msb";
            a += "define foo(\"test\")";
            a += "db {foo}";
            var result = Asm.Assemble(a);
            Assert.AreEqual(4, result.Length);
            Assert.AreEqual("test", ASCIIEncoding.ASCII.GetString(result));
        }

        [Test]        
        public void IfConditionalTest()
        {
            var a = new AssemblyTextSource("main");
            a += "endian msb";
            a += "define foo(1)";
            a += "if ({foo}) {";
            a += "db \"test\"";
            a += "}";
            var result = Asm.Assemble(a);
            Assert.AreEqual(4, result.Length);
            Assert.AreEqual("test", ASCIIEncoding.ASCII.GetString(result));

            a = new AssemblyTextSource("main");
            a += "endian msb";
            a += "define foo(0)";
            a += "if ({foo}) {";
            a += "db \"test\"";
            a += "}";
            result = Asm.Assemble(a);
            Assert.IsEmpty(result);
        }

        [Test]        
        public void IfElseConditionalTest()
        {
            var a = new AssemblyTextSource("main");
            a += "endian msb";
            a += "define foo(0)";
            a += "if ({foo}) {";
            a += "db \"test\"";
            a += "} else {";
            a += "db \"poof\"";
            a += "}";
            var result = Asm.Assemble(a);
            Assert.AreEqual(4, result.Length);
            Assert.AreEqual("poof", ASCIIEncoding.ASCII.GetString(result));
        }

        [Test]        
        public void IfElseConditionalTest2()
        {
            var a = new AssemblyTextSource("main");
            a += "endian msb";
            a += "define foo(0)";
            a += "define bar(1)";
            a += "if ({foo}) {";
            a += "db \"test\"";
            a += "} else if ({bar}) {";
            a += "db \"bump\"";
            a += "} else {";
            a += "db \"poof\"";
            a += "}";
            var result = Asm.Assemble(a);
            Assert.AreEqual(4, result.Length);
            Assert.AreEqual("bump", ASCIIEncoding.ASCII.GetString(result));
        }

        [Test]        
        public void EvaluateTest()
        {
            var a = new AssemblyTextSource("main");

            a += "macro eval(evaluate a) {";
            a += "print {a}";
            a += "}";

            a += "eval(1)";
            a += "eval(1 == 0)";
            a += "eval(1 == 1)";
            a += "eval(1 + 1)";
            a += "eval(5 - 1)";
            a += "eval(6 * 2)";
            a += "eval(10 / 2)";
            a += "eval(5 % 2)";
            a += "eval(1 >> 1)";
            a += "eval(1 << 1)";
            a += "eval(-1)";
            a += "eval(1 ? 1 : 0)";
            a += "eval(0 ? 5 : 2)";
            a += "eval(1 ? (0 ? 9 : 8) : 7)";
            a += "eval(1 + (2 * 3) + 1)";
            a += "eval(1 || 0)";
            a += "eval(1 && 0)";
            a += "eval(1 ^ 1)";
            a += "eval($DE00 | 0xAD)";
            a += "eval($DEAD & 0x00FF)";
            a += "eval(!(0))";
            a += "eval(!(1))";
            a += "eval(~1)";
            a += "eval(5 != 6)";
            a += "eval(6 >= 5)";
            a += "eval(7 <= 7)";
            a += "eval(1 > 2)";
            a += "eval(2 < 3)";

            var result = Asm.AssembleAndPeek(a);

            int o = 0;
            Assert.AreEqual("1", result.PrintLines[o++]);
            Assert.AreEqual("0", result.PrintLines[o++]);
            Assert.AreEqual("1", result.PrintLines[o++]);
            Assert.AreEqual("2", result.PrintLines[o++]);
            Assert.AreEqual("4", result.PrintLines[o++]);
            Assert.AreEqual("12", result.PrintLines[o++]);
            Assert.AreEqual("5", result.PrintLines[o++]);
            Assert.AreEqual("1", result.PrintLines[o++]);
            Assert.AreEqual((1 >> 1).ToString(), result.PrintLines[o++]);
            Assert.AreEqual((1 << 1).ToString(), result.PrintLines[o++]);
            Assert.AreEqual((-1).ToString(), result.PrintLines[o++]);
            Assert.AreEqual("1", result.PrintLines[o++]);
            Assert.AreEqual("2", result.PrintLines[o++]);
            Assert.AreEqual("8", result.PrintLines[o++]);
            Assert.AreEqual("8", result.PrintLines[o++]);
            Assert.AreEqual("1", result.PrintLines[o++]);
            Assert.AreEqual("0", result.PrintLines[o++]);
            Assert.AreEqual("0", result.PrintLines[o++]);
            Assert.AreEqual(0xDEAD.ToString(), result.PrintLines[o++]);
            Assert.AreEqual(0x00AD.ToString(), result.PrintLines[o++]);
            Assert.AreEqual("1", result.PrintLines[o++]);
            Assert.AreEqual("0", result.PrintLines[o++]);
            Assert.AreEqual((~1).ToString(), result.PrintLines[o++]);
            Assert.AreEqual("1", result.PrintLines[o++]);
            Assert.AreEqual("1", result.PrintLines[o++]);
            Assert.AreEqual("1", result.PrintLines[o++]);
            Assert.AreEqual("0", result.PrintLines[o++]);
            Assert.AreEqual("1", result.PrintLines[o++]);
        }

        [Test]        
        public void ScopeTest()
        {
            var a = new AssemblyTextSource("main");
            a += "define a(5)";
            a += "print {a}";
            a += "scope foo {";
            a += "define a(8)";
            a += "print {a}";
            a += "}";
            a += "print {a}";

            var result = Asm.AssembleAndPeek(a);

            int o = 0;
            Assert.AreEqual("5", result.PrintLines[o++]);
            Assert.AreEqual("8", result.PrintLines[o++]);
            Assert.AreEqual("5", result.PrintLines[o++]);
        }

        [Test]        
        public void GlobalTest()
        {
            var a = new AssemblyTextSource("main");
            a += "global define a(5)";
            a += "print {a}";
            a += "scope foo {";
            a += "print {a}";
            a += "}";
            a += "print {a}";

            var result = Asm.AssembleAndPeek(a);

            int o = 0;
            Assert.AreEqual("5", result.PrintLines[o++]);
            Assert.AreEqual("5", result.PrintLines[o++]);
            Assert.AreEqual("5", result.PrintLines[o++]);
        }

        [Test]        
        public void BitLengthTest()
        {
            String v = "%1010";
            Assert.AreEqual(4, BassTable.ArgumentBitLength(ref v));

            v = "$DEAD";
            Assert.AreEqual(16, BassTable.ArgumentBitLength(ref v));

            v = "0b1010";
            Assert.AreEqual(4, BassTable.ArgumentBitLength(ref v));

            v = "0xDEAD";
            Assert.AreEqual(16, BassTable.ArgumentBitLength(ref v));

            v = "<foo";
            Assert.AreEqual(8, BassTable.ArgumentBitLength(ref v));
            Assert.AreEqual(" foo", v);

            v = ">foo";
            Assert.AreEqual(16, BassTable.ArgumentBitLength(ref v));
            Assert.AreEqual(" foo", v);

            v = "^foo";
            Assert.AreEqual(24, BassTable.ArgumentBitLength(ref v));
            Assert.AreEqual(" foo", v);

            v = "?foo";
            Assert.AreEqual(32, BassTable.ArgumentBitLength(ref v));
            Assert.AreEqual(" foo", v);

            v = ":foo";
            Assert.AreEqual(64, BassTable.ArgumentBitLength(ref v));
            Assert.AreEqual(" foo", v);
        }
    }
}
