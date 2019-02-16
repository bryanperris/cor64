using cor64.BassSharp;
using cor64.BassSharp.Table;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using Xunit.Abstractions;
using static cor64.BassSharp.Bass;

namespace cor64.Tests.Asm
{
    public class AssemblerTests
    {
        private Byte[] Assemble(AssemblyTextSource src)
        {
            var assembler = new N64Assembler();
            assembler.AddAssemblySource(src);
            assembler.AssembleCode(true);
            var o = assembler.Output;
            byte[] b = new byte[o.Length];
            o.Position = 0;
            o.Read(b, 0, b.Length);
            return b;
        }

        private N64Assembler AssembleAndPeek(AssemblyTextSource src)
        {
            var assembler = new N64Assembler();
            assembler.AddAssemblySource(src);
            assembler.AssembleCode(true);
            return assembler;
        }


        [Fact]
        [Logging]
        public void EmptyTest()
        {
            var a = new AssemblyTextSource("main");
            a += "//Empty assembly test";
            var result = Assemble(a);
            Assert.Empty(result);
        }

        [Fact]
        [Logging]
        public void EmptyArchTest()
        {
            var a = new AssemblyTextSource("main");
            a += "//Empty assembly test";
            a += "arch n64.cpu";
            var result = Assemble(a);
            Assert.Empty(result);
        }

        [Fact]
        [Logging]
        public void EmptyIncludeTest()
        {
            var a = new AssemblyTextSource("main");
            a += "//Empty assembly test";
            a += "include \"test\"";
            var result = Assemble(a);
            Assert.Empty(result);
        }

        [Fact]
        [Logging]
        public void OutputTest()
        {
            var a = new AssemblyTextSource("main");
            a += "output \"Sample.N64\", create";
            Assemble(a);
        }

        [Fact]
        [Logging]
        public void FillTest()
        {
            var a = new AssemblyTextSource("main");
            a += "fill 0xDEAD";
            var result = Assemble(a);
            Assert.Equal(0xDEAD, result.Length);
        }

        [Fact]
        [Logging]
        public void IncludeTest()
        {
            var a = new AssemblyTextSource("main");
            a += "include \"file\"";
            Assemble(a);
        }

        [Fact]
        [Logging]
        public void SimpleProgram()
        {
            var a = new AssemblyTextSource("main");
            a += "arch n64.cpu";
            a += "endian msb";
            a += "origin $00000000";
            a += "base $80000000 // Entry Point Of Code";
            a += "nop";
            var result = Assemble(a);
            Assert.Equal(4, result.Length);
            Assert.Equal(new byte[] { 0, 0, 0, 0 }, result);
        }

        [Fact]
        [Logging]
        public void EndianTest()
        {
            var a = new AssemblyTextSource("main");
            a += "endian msb";
            var result = AssembleAndPeek(a);
            Assert.Equal(Endian.MSB, result.Endianess);

            a = new AssemblyTextSource("main");
            a += "endian lsb";
            result = AssembleAndPeek(a);
            Assert.NotEqual(Endian.MSB, result.Endianess);
        }

        [Fact]
        [Logging]
        public void OffsetTest()
        {
            var a = new AssemblyTextSource("main");
            a += "base 5";
            a += "origin $DEAD";
            var result = AssembleAndPeek(a);

            Assert.Equal(0xDEAD, result.Origin);
            Assert.Equal(5, result.Base);
            Assert.Equal(0xDEAD + 5, result.Pc);
        }

        [Fact]
        [Logging]
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

            var result = AssembleAndPeek(a);
            Assert.Equal(1, result.MacroCount);

            var result2 = Assemble(a);
            Assert.Equal(4, result2.Length);
            Assert.Equal(new byte[] { 0xD, 0xE, 0xA, 0xD }, result2);
        }

        [Fact]
        [Logging]
        public void DefineTest()
        {
            var a = new AssemblyTextSource("main");
            a += "endian msb";
            a += "define foo($DEAF)";
            a += "dw {foo}";
            var result = Assemble(a);
            Assert.Equal(2, result.Length);
            Assert.Equal(new byte[] { 0xDE, 0xAF }, result);
        }

        [Fact]
        [Logging]
        public void ConstantTest()
        {
            var a = new AssemblyTextSource("main");
            a += "endian msb";
            a += "constant foo($DEAF)";
            a += "dw foo";
            var result = Assemble(a);
            Assert.Equal(2, result.Length);
            Assert.Equal(new byte[] { 0xDE, 0xAF }, result);
        }

        [Fact]
        [Logging]
        public void ConstantStringTest()
        {
            var a = new AssemblyTextSource("main");
            a += "endian msb";
            a += "define foo(\"test\")";
            a += "db {foo}";
            var result = Assemble(a);
            Assert.Equal(4, result.Length);
            Assert.Equal("test", ASCIIEncoding.ASCII.GetString(result));
        }

        [Fact]
        [Logging]
        public void IfConditionalTest()
        {
            var a = new AssemblyTextSource("main");
            a += "endian msb";
            a += "define foo(1)";
            a += "if ({foo}) {";
            a += "db \"test\"";
            a += "}";
            var result = Assemble(a);
            Assert.Equal(4, result.Length);
            Assert.Equal("test", ASCIIEncoding.ASCII.GetString(result));

            a = new AssemblyTextSource("main");
            a += "endian msb";
            a += "define foo(0)";
            a += "if ({foo}) {";
            a += "db \"test\"";
            a += "}";
            result = Assemble(a);
            Assert.Empty(result);
        }

        [Fact]
        [Logging]
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
            var result = Assemble(a);
            Assert.Equal(4, result.Length);
            Assert.Equal("poof", ASCIIEncoding.ASCII.GetString(result));
        }

        [Fact]
        [Logging]
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
            var result = Assemble(a);
            Assert.Equal(4, result.Length);
            Assert.Equal("bump", ASCIIEncoding.ASCII.GetString(result));
        }

        [Fact]
        [Logging]
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

            var result = AssembleAndPeek(a);

            int o = 0;
            Assert.Equal("1", result.PrintLines[o++]);
            Assert.Equal("0", result.PrintLines[o++]);
            Assert.Equal("1", result.PrintLines[o++]);
            Assert.Equal("2", result.PrintLines[o++]);
            Assert.Equal("4", result.PrintLines[o++]);
            Assert.Equal("12", result.PrintLines[o++]);
            Assert.Equal("5", result.PrintLines[o++]);
            Assert.Equal("1", result.PrintLines[o++]);
            Assert.Equal((1 >> 1).ToString(), result.PrintLines[o++]);
            Assert.Equal((1 << 1).ToString(), result.PrintLines[o++]);
            Assert.Equal((-1).ToString(), result.PrintLines[o++]);
            Assert.Equal("1", result.PrintLines[o++]);
            Assert.Equal("2", result.PrintLines[o++]);
            Assert.Equal("8", result.PrintLines[o++]);
            Assert.Equal("8", result.PrintLines[o++]);
            Assert.Equal("1", result.PrintLines[o++]);
            Assert.Equal("0", result.PrintLines[o++]);
            Assert.Equal("0", result.PrintLines[o++]);
            Assert.Equal(0xDEAD.ToString(), result.PrintLines[o++]);
            Assert.Equal(0x00AD.ToString(), result.PrintLines[o++]);
            Assert.Equal("1", result.PrintLines[o++]);
            Assert.Equal("0", result.PrintLines[o++]);
            Assert.Equal((~1).ToString(), result.PrintLines[o++]);
            Assert.Equal("1", result.PrintLines[o++]);
            Assert.Equal("1", result.PrintLines[o++]);
            Assert.Equal("1", result.PrintLines[o++]);
            Assert.Equal("0", result.PrintLines[o++]);
            Assert.Equal("1", result.PrintLines[o++]);
        }

        [Fact]
        [Logging]
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

            var result = AssembleAndPeek(a);

            int o = 0;
            Assert.Equal("5", result.PrintLines[o++]);
            Assert.Equal("8", result.PrintLines[o++]);
            Assert.Equal("5", result.PrintLines[o++]);
        }

        [Fact]
        [Logging]
        public void GlobalTest()
        {
            var a = new AssemblyTextSource("main");
            a += "global define a(5)";
            a += "print {a}";
            a += "scope foo {";
            a += "print {a}";
            a += "}";
            a += "print {a}";

            var result = AssembleAndPeek(a);

            int o = 0;
            Assert.Equal("5", result.PrintLines[o++]);
            Assert.Equal("5", result.PrintLines[o++]);
            Assert.Equal("5", result.PrintLines[o++]);
        }

        [Fact]
        [Logging]
        public void BitLengthTest()
        {
            String v = "%1010";
            Assert.Equal(4, BassTable.ArgumentBitLength(ref v));

            v = "$DEAD";
            Assert.Equal(16, BassTable.ArgumentBitLength(ref v));

            v = "0b1010";
            Assert.Equal(4, BassTable.ArgumentBitLength(ref v));

            v = "0xDEAD";
            Assert.Equal(16, BassTable.ArgumentBitLength(ref v));

            v = "<foo";
            Assert.Equal(8, BassTable.ArgumentBitLength(ref v));
            Assert.Equal(" foo", v);

            v = ">foo";
            Assert.Equal(16, BassTable.ArgumentBitLength(ref v));
            Assert.Equal(" foo", v);

            v = "^foo";
            Assert.Equal(24, BassTable.ArgumentBitLength(ref v));
            Assert.Equal(" foo", v);

            v = "?foo";
            Assert.Equal(32, BassTable.ArgumentBitLength(ref v));
            Assert.Equal(" foo", v);

            v = ":foo";
            Assert.Equal(64, BassTable.ArgumentBitLength(ref v));
            Assert.Equal(" foo", v);
        }
    }
}
