using cor64.Mips;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cor64.IO;
using System.IO;
using cor64.Mips.R4300I;
using System.Text.RegularExpressions;
using static cor64.Mips.BaseDisassembler;
using cor64;
using NUnit.Framework;

namespace Tests
{
    [TestFixture]
    public class DisassemblerTests : BaseTest
    {
        private static readonly HashSet<String> s_CpuExclude = new HashSet<string>()
        {
            "nop",
            "ssnop",
           @"abs\ (.*),(.*)",
           @"abs\ (.*)",
           @"b\ (.*)",
           @"bal\ (.*)",
           @"beqz\ (.*),(.*)",
           @"bnez\ (.*),(.*)",
           @"bge\ (.*),(.*),(.*)",
           @"bgt\ (.*),(.*),(.*)",
           @"ble\ (.*),(.*),(.*)",
           @"blt\ (.*),(.*),(.*)",
           @"bgeu\ (.*),(.*),(.*)",
           @"bgtu\ (.*),(.*),(.*)",
           @"bgtu\ (.*),(.*),(.*)",
           @"bleu\ (.*),(.*),(.*)",
           @"bltu\ (.*),(.*),(.*)",
           @"jal\ (.*),(.*)",
           @"la\ (.*),(.*)",
           @"lli\ (.*),(.*)",
           @"li\ (.*),(.*)",
           @"neg\ (.*),(.*)",
           @"neg\ (.*)",
           @"negu\ (.*),(.*)",
           @"negu\ (.*)",
           @"not\ (.*),(.*)",
           @"not\ (.*)",
           @"seq\ (.*),(.*),(.*)",
           @"seq\ (.*),(.*)",
           @"sge\ (.*),(.*),(.*)",
           @"sge\ (.*),(.*)",
           @"sgeu\ (.*),(.*),(.*)",
           @"sgeu\ (.*),(.*)",
           @"sgt\ (.*),(.*),(.*)",
           @"sgt\ (.*),(.*)",
           @"sgtu\ (.*),(.*),(.*)",
           @"sgtu\ (.*),(.*)",
           @"sle\ (.*),(.*),(.*)",
           @"sle\ (.*),(.*)",
           @"sleu\ (.*),(.*),(.*)",
           @"sleu\ (.*),(.*)",
           @"sne\ (.*),(.*),(.*)",
           @"sne\ (.*),(.*)",
           @"subi\ (.*),(.*),(.*)",
           @"subi\ (.*),(.*)",
           @"subiu\ (.*),(.*),(.*)",
           @"subiu\ (.*),(.*)",
           @"lwc0\ (.*),(.*)\((.*)\)",
           @"lwc2\ (.*),(.*)\((.*)\)",
           @"lwc3\ (.*),(.*)\((.*)\)",
           @"swc0\ (.*),(.*)\((.*)\)",
           @"swc2\ (.*),(.*)\((.*)\)",
           @"swc3\ (.*),(.*)\((.*)\)",
           @"bc0f\ (.*)",
           @"bc0t\ (.*)",
           @"bc2f\ (.*)",
           @"bc2t\ (.*)",
           @"bc3f\ (.*)",
           @"bc3t\ (.*)",
           @"cfc0\ (.*),(.*)",
           @"ctc0\ (.*),(.*)",
           @"mfc2\ (.*),(.*)",
           @"mtc2\ (.*),(.*)",
           @"cfc2\ (.*),(.*)",
           @"ctc2\ (.*),(.*)",
           @"mfc3\ (.*),(.*)",
           @"mtc3\ (.*),(.*)",
           @"cfc3\ (.*),(.*)",
           @"ctc3\ (.*),(.*)",
           @"cop0\ (.*)",
           @"cop1\ (.*)",
           @"cop2\ (.*)",
           @"cop3\ (.*)",
           @"rfe",
           @"l\.s\ (.*),(.*)\((.*)\)",
           @"s\.s\ (.*),(.*)\((.*)\)",
           @"div\ (.*),(.*),(.*)",
           @"divu\ (.*),(.*),(.*)",
           @"mul\ (.*),(.*),(.*)",
           @"mul\ (.*),(.*)",
           @"mulu\ (.*),(.*),(.*)",
           @"mulu\ (.*),(.*)",
           @"mulo\ (.*),(.*),(.*)",
           @"mulo\ (.*),(.*)",
           @"mulou\ (.*),(.*),(.*)",
           @"mulou\ (.*),(.*)",
           @"rem\ (.*),(.*),(.*)",
           @"rem\ (.*),(.*)",
           @"remu\ (.*),(.*),(.*)",
           @"remu\ (.*),(.*)",
           @"bgel\ (.*),(.*),(.*)",
           @"bgtl\ (.*),(.*),(.*)",
           @"blel\ (.*),(.*),(.*)",
           @"bltl\ (.*),(.*),(.*)",
           @"bgeul\ (.*),(.*),(.*)",
           @"bgtul\ (.*),(.*),(.*)",
           @"bleul\ (.*),(.*),(.*)",
           @"bltul\ (.*),(.*),(.*)",
           @"beqzl\ (.*),(.*)",
           @"bnezl\ (.*),(.*)",
           @"ldc2\ (.*),(.*)\((.*)\)",
           @"ldc3\ (.*),(.*)\((.*)\)",
           @"sdc2\ (.*),(.*)\((.*)\)",
           @"sdc3\ (.*),(.*)\((.*)\)",
           @"bc0fl\ (.*)",
           @"bc2fl\ (.*)",
           @"bc3fl\ (.*)",
           @"bc0tl\ (.*)",
           @"bc2tl\ (.*)",
           @"bc3tl\ (.*)",
           @"move\ (.*),(.*)",
           @"dli\ (.*),(.*)",
           @"dabs\ (.*),(.*)",
           @"dabs\ (.*)",
           @"ddiv\ (.*),(.*),(.*)",
           @"ddivu\ (.*),(.*),(.*)",
           @"dmul\ (.*),(.*),(.*)",
           @"dmul\ (.*),(.*)",
           @"dmulu\ (.*),(.*),(.*)",
           @"dmulu\ (.*),(.*)",
           @"dmulo\ (.*),(.*),(.*)",
           @"dmulo\ (.*),(.*)",
           @"dmulou\ (.*),(.*),(.*)",
           @"dmulou\ (.*),(.*)",
           @"drem\ (.*),(.*),(.*)",
           @"drem\ (.*),(.*)",
           @"dremu\ (.*),(.*),(.*)",
           @"dremu\ (.*),(.*)",
           @"drol\ (.*),(.*),(.*)",
           @"drol\ (.*),(.*)",
           @"dror\ (.*),(.*),(.*)",
           @"dror\ (.*),(.*)",
           @"dneg\ (.*),(.*)",
           @"dneg\ (.*)",
           @"dnegu\ (.*),(.*)",
           @"dnegu\ (.*)",
           @"dmfc2\ (.*),(.*)",
           @"dmfc3\ (.*),(.*)",
           @"dmtc2\ (.*),(.*)",
           @"dmtc3\ (.*),(.*)",
           @"l\.d\ (.*),(.*)\((.*)\)",
           @"s\.d\ (.*),(.*)\((.*)\)"
        };

        private static readonly HashSet<string> s_RspExclude = new HashSet<string>()
        {
           @"abs\ (.*),(.*)",
           @"abs\ (.*)",
           @"b\ (.*)",
           @"bal\ (.*)",
           @"beqz\ (.*),(.*)",
           @"bnez\ (.*),(.*)",
           @"bge\ (.*),(.*),(.*)",
           @"bgt\ (.*),(.*),(.*)",
           @"ble\ (.*),(.*),(.*)",
           @"blt\ (.*),(.*),(.*)",
           @"bgeu\ (.*),(.*),(.*)",
           @"bgtu\ (.*),(.*),(.*)",
           @"bgtu\ (.*),(.*),(.*)",
           @"bleu\ (.*),(.*),(.*)",
           @"bltu\ (.*),(.*),(.*)",
           @"jal\ (.*),(.*)",
           @"la\ (.*),(.*)",
           @"lli\ (.*),(.*)",
           @"li\ (.*),(.*)",
           @"neg\ (.*),(.*)",
           @"neg\ (.*)",
           @"negu\ (.*),(.*)",
           @"negu\ (.*)",
           @"not\ (.*),(.*)",
           @"not\ (.*)",
           @"seq\ (.*),(.*),(.*)",
           @"seq\ (.*),(.*)",
           @"sge\ (.*),(.*),(.*)",
           @"sge\ (.*),(.*)",
           @"sgeu\ (.*),(.*),(.*)",
           @"sgeu\ (.*),(.*)",
           @"sgt\ (.*),(.*),(.*)",
           @"sgt\ (.*),(.*)",
           @"sgtu\ (.*),(.*),(.*)",
           @"sgtu\ (.*),(.*)",
           @"sle\ (.*),(.*),(.*)",
           @"sle\ (.*),(.*)",
           @"sleu\ (.*),(.*),(.*)",
           @"sleu\ (.*),(.*)",
           @"sne\ (.*),(.*),(.*)",
           @"sne\ (.*),(.*)",
           @"subi\ (.*),(.*),(.*)",
           @"subi\ (.*),(.*)",
           @"subiu\ (.*),(.*),(.*)",
           @"subiu\ (.*),(.*)",
           @"syscall\ (.*)",
           @"lwl\ (.*),(.*)\((.*)\)",
           @"lwr\ (.*),(.*)\((.*)\)",
           @"lwc0\ (.*),(.*)\((.*)\)",
           @"lwc1\ (.*),(.*)\((.*)\)",
           @"lwc3 0,0(0)",
           @"lwc3\ (.*),(.*)\((.*)\)",
           @"swl\ (.*),(.*)\((.*)\)",
           @"swr\ (.*),(.*)\((.*)\)",
           @"swc0\ (.*),(.*)\((.*)\)",
           @"swc1\ (.*),(.*)\((.*)\)",
           @"swc3\ (.*),(.*)\((.*)\)",
           @"bc0f\ (.*)",
           @"bc0t\ (.*)",
           @"bc1f\ (.*)",
           @"bc1t\ (.*)",
           @"bc2f\ (.*)",
           @"bc2t\ (.*)",
           @"bc3f\ (.*)",
           @"bc3t\ (.*)",
           @"cfc0\ (.*),(.*)",
           @"ctc0\ (.*),(.*)",
           @"mfc1\ (.*),(.*)",
           @"mtc1\ (.*),(.*)",
           @"cfc1\ (.*),(.*)",
           @"ctc1\ (.*),(.*)",
           @"mfc3\ (.*),(.*)",
           @"mtc3\ (.*),(.*)",
           @"cfc3\ (.*),(.*)",
           @"ctc3\ (.*),(.*)",
           @"cop0\ (.*)",
           @"cop1\ (.*)",
           @"cop2\ (.*)",
           @"cop3\ (.*)",
           @"move\ (.*),(.*)",
           @"lwc2\ (.*),(.*)\((.*)\)",
           @"swc2\ (.*),(.*)\((.*)\)"
        };

       [Test]
        public void RspOpcodes() {
            var arch = "n64.rsp";
            var opcodes = GenerateOpcodeList(arch, s_RspExclude);
            var disassembler = new cor64.Mips.Rsp.Disassembler("o32");
            TestOpcodes(opcodes, disassembler, arch, null);
        }

        private static void TestOpcodes(IList<cor64.BassSharp.Table.Opcode> opcodes, BaseDisassembler disassembler, String arch, Action<cor64.BassSharp.Table.Opcode, DecodedInstruction> validator)
        {
            for (int i = 0; i < opcodes.Count; i++)
            {
                cor64.BassSharp.Table.Opcode opcode = opcodes[i];
                var op = Regex.Unescape(opcode.GetOpName());

                StringBuilder sb = new StringBuilder();

                Console.WriteLine("Opcode to test: " + opcode.ToString());

                for (int j = 0; j < opcode.NumberList.Count; j++)
                {
                    //Console.WriteLine(opcode.PrefixList[j].Text);

                    if (j == 0) {
                        var firstSplit = opcode.PrefixList[0].Text.Split(" ");

                        if (firstSplit.Length > 1) {
                            sb.Append(firstSplit[1]);
                        }
                    }

                    var isParen = Regex.Unescape(opcode.PrefixList[j].Text) == "(";

                    /* Vector element selection */
                    if (Regex.Unescape(opcode.PrefixList[j].Text) == "[e") {
                        sb.Remove(sb.Length - 1, 1);
                        sb.Append("[e");
                    }

                    /* Vector reg select */
                    if (j != 0 && Regex.Unescape(opcode.PrefixList[j].Text).EndsWith("v")) {
                        sb.Append("v");
                    }

                    /* Remove last comma */
                    if (isParen) sb.Remove(sb.Length - 1, 1);

                    sb.Append(isParen ? "(0)" : "0");

                    if (Regex.Unescape(opcode.PrefixList[j].Text) == "[e") {
                        sb.Append("]");
                    }

                    if (!isParen && j + 1 < opcode.NumberList.Count) sb.Append(",");
                }

                var asm = op + " " + sb.ToString();

                Console.WriteLine("Test Asm: " + asm);

                var decoded = TestOpcode(disassembler, arch, asm, op);

                validator?.Invoke(opcode, decoded);
            }
        }

        [Test]
        public void TestOperands()
        {
            var arch = "n64.cpu";
            var opcodes = GenerateOpcodeList(arch, s_CpuExclude);
            var disassembler = new Disassembler("o32");

            TestOpcodes(opcodes, disassembler, arch, (o, d) =>
            {
                var operandFmt = Operands.GetFormat(d.Op.OperandFmt);

                Assert.True(
                    o.NumberList.Count == operandFmt.Size ||
                    o.NumberList.Count == operandFmt.Size - 1);

                if (o.NumberList.Count > 0)
                {
                    var result = Regex.Match(operandFmt.Format, o.Pattern.Split(new[] { ' ' }, 2)[1]).Success;

                    Assert.True(result);
                }
            });
        }

        [Test]
        public void TestPsuedoOpcodes()
        {
            var arch = "n64.cpu";
            var disassembler = new Disassembler("o32");
            TestOpcode(disassembler, arch, "nop", "sll");
            TestOpcode(disassembler, arch, "b 0", "beq");
            TestOpcode(disassembler, arch, "jal 0,0", "lui");
        }

        private static DecodedInstruction TestOpcode(BaseDisassembler disassembler, String arch, String asm, String opcode)
        {
            var assembler = new N64Assembler();

            /* Assembly Source */
            var source = new AssemblyTextSource("main");
            source += "arch " + arch;
            source += "endian msb";
            source += asm;

            /* Assemble into bytes */
            assembler.AddAssemblySource(source);
            assembler.AssembleCode(true);

            /* Get the output */
            var streamOut = assembler.Output;
            streamOut.Position = 0;

            /* Put the output stream into a swapped buffer */
            using (streamOut)
            {
                /* Now we start disassembling */
                disassembler.SetStreamSource(streamOut);
                var decode = disassembler.Disassemble(0, 1)[0];
                var result = decode.IsNull ? "" : disassembler.GetFullDisassembly(decode).Split(' ')[0];

                /* Since FPU format/condition is undetermined, bypass it */
                if (decode.Op.Family == OperationFamily.Fpu)
                {
                    opcode = BypassFpu(opcode);
                    result = BypassFpu(result);

                    //if (opcode.StartsWith("c.f"))
                    //    opcode = opcode.Replace("c.f", "c.c");
                }

                if (result == "nop")
                    result = "sll";

                if (opcode.EndsWith("nop") && result == "sll")
                    result = opcode;

                Assert.AreEqual(opcode, result);

                return decode;
            }
        }

        private static string BypassFpu(string value)
        {
            if (value.Contains("."))
            {
                var matches = Regex.Matches(value, @"(\.[a-z]+)");
                int move = 0;

                foreach (Match m in matches)
                {
                    if (m.Success)
                    {
                        value = value.Remove(m.Index - move, m.Length);

                        if (m.Length == 2)
                        {
                            value = value.Insert(m.Index - move, ".f");
                            move -= 2;
                        }
                        else
                        {
                            value = value.Insert(m.Index - move, ".c");
                            move -= 2;
                        }

                        move += m.Length;
                    }
                }
            }

            return value;
        }

        private static IList<cor64.BassSharp.Table.Opcode> GenerateOpcodeList(string arch, HashSet<String> blacklist)
        {
            var assembler = new N64Assembler();

            /* Assembly Source */
            var source = new AssemblyTextSource("main");
            source += "arch " + arch;
            source += "endian msb";

            /* Assemble into bytes */
            assembler.AddAssemblySource(source);
            assembler.AssembleCode(true);

            var opcodes = assembler.GetOpcodes();

            return (from i in Enumerable.Range(0, opcodes.Count)
                    let rawOp = opcodes[i]
                    where !blacklist.Contains(rawOp.Pattern)
                    select rawOp).ToList();
        }

        private static void TestInjectedOpcode(String archData, String asm, String opcode)
        {
            var assembler = new N64Assembler();

            /* Inject arch data */
            String directives = "//DIRECTIVES\n";
            directives += "db; EMIT_BYTES: 0\n";
            directives += "dh; EMIT_BYTES: 1\n";
            directives += "dl; EMIT_BYTES: 2\n";
            directives += "dw; EMIT_BYTES: 3\n";
            directives += "dd; EMIT_BYTES: 4\n";
            directives += "//INSTRUCTIONS\n";
            assembler.AssembleArchTable(directives + archData + "\n");

            /* Assembly Source */
            var source = new AssemblyTextSource("main");
            source += "endian msb";
            source += asm;

            /* Assemble into bytes */
            assembler.AddAssemblySource(source);
            assembler.AssembleCode(true);

            /* Get the output */
            var streamOut = assembler.Output;
            streamOut.Position = 0;

            /* Put the output stream into a swapped buffer */
            using (streamOut)
            {
                /* Now we start disassembling */
                var disassembler = new Disassembler("o32");
                disassembler.SetStreamSource(streamOut);
                var decode = disassembler.Disassemble(0, 1)[0];

                Assert.AreEqual(opcode, disassembler.GetFullDisassembly(decode).Split(' ')[0]);
            }
        }

        [Test]
        public void CpuOpcodes()
        {
            var arch = "n64.cpu";
            var opcodes = GenerateOpcodeList(arch, s_CpuExclude);
            var disassembler = new Disassembler("o32");
            TestOpcodes(opcodes, disassembler, arch, null);
        }
    }
}
