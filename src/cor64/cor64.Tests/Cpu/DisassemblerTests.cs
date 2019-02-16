using cor64.Mips;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cor64.IO;
using System.IO;
using Xunit;
using cor64.Mips.R4300I;
using System.Text.RegularExpressions;
using static cor64.Mips.BaseDisassembler;

namespace cor64.Tests.Cpu
{
    public class DisassemblerTests
    {
        private static readonly HashSet<String> s_R4300IFilter = new HashSet<string>()
        {
            "lwc0",
            "lwc2",
            "lwc3",
            "swc0",
            "swc2",
            "swc3",
            "bc0f",
            "bc0t",
            "bc2f",
            "bc2t",
            "bc3f",
            "bc3t",
            "cfc0",
            "ctc0",
            "mfc2",
            "mtc2",
            "cfc2",
            "ctc2",
            "mfc3",
            "mtc3",
            "cfc3",
            "ctc3",
            "cop0",
            "cop1",
            "cop2",
            "cop3",
            "rfe",
            "l.s",
            "s.s",
            "beqzl",
            "bnezl",
            "ldc2",
            "ldc3",
            "sdc2",
            "sdc3",
            "bc0fl",
            "bc2fl",
            "bc3fl",
            "bc0tl",
            "bc2tl",
            "bc3tl",
            "dneg",
            "dnegu",
            "dmfc2",
            "dmfc3",
            "dmtc2",
            "dmtc3",
            "l.d",
            "s.d"
        };

        private static DecodedInstruction TestOpcode(String asm, String opcode)
        {
            var assembler = new N64Assembler();

            /* Assembly Source */
            var source = new AssemblyTextSource("main");
            source += "arch n64.cpu";
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
                var disassembler = new Disassembler("o32", Mode.DebugFast);
                disassembler.SetStreamSource(streamOut);
                var decode = disassembler.Disassemble(0, 1)[0];
                var result = decode.IsNull ? "" : disassembler.GetFullDisassembly(decode).Split(' ')[0];

                /* Since FPU format/condition is undetermined, bypass it */
                if (decode.Op.Family == OperationFamily.Fpu)
                {
                    opcode = BypassFpu(opcode);
                    result = BypassFpu(result);

                    if (opcode.StartsWith("c.f"))
                        opcode = opcode.Replace("c.f", "c.c");
                }

                if (result == "nop")
                {
                    result = "sll";
                }

                Assert.Equal(opcode, result);

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

        private static IList<BassSharp.Table.Opcode> GenerateOpcodeList(HashSet<String> blacklist)
        {
            var assembler = new N64Assembler();

            /* Assembly Source */
            var source = new AssemblyTextSource("main");
            source += "arch n64.cpu";
            source += "endian msb";

            /* Assemble into bytes */
            assembler.AddAssemblySource(source);
            assembler.AssembleCode(true);

            var opcodes = assembler.GetOpcodes();

            Func<int, int, int, bool> isPseudo = (v, b, s) =>
            {
                return v >= b && v < (b + s);
            };

            return (from i in Enumerable.Range(0, opcodes.Count)
                    let rawOp = opcodes[i]
                    let opcode = rawOp.GetOpName()
                    where !isPseudo(i, 0, 46)
                    where !isPseudo(i, 238, 14)
                    where !isPseudo(i, 260, 8)
                    where !isPseudo(i, 333, 22)
                    where !blacklist.Contains(opcode)
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
                var disassembler = new Disassembler("o32", Mode.DebugFast);
                disassembler.SetStreamSource(streamOut);
                var decode = disassembler.Disassemble(0, 1)[0];

                Assert.Equal(opcode, disassembler.GetFullDisassembly(decode).Split(' ')[0]);
            }
        }

        [Fact]
        [Logging]
        public void TestOpcodes()
        {
            _TestOpcodes(null);
        }

        private static void _TestOpcodes(Action<BassSharp.Table.Opcode, DecodedInstruction> validator)
        {
            var opcodes = GenerateOpcodeList(s_R4300IFilter);

            for (int i = 0; i < opcodes.Count; i++)
            {
                BassSharp.Table.Opcode opcode = opcodes[i];
                var op = Regex.Unescape(opcode.PrefixList[0].Text.Split(' ')[0]);

                StringBuilder sb = new StringBuilder();

                for (int j = 0; j < opcode.NumberList.Count; j++)
                {
                    var isParen = Regex.Unescape(opcode.PrefixList[j].Text) == "(";

                    /* Remove last comma */
                    if (isParen) sb.Remove(sb.Length - 1, 1);

                    sb.Append(isParen ? "(0)" : "0");

                    if (!isParen && j + 1 < opcode.NumberList.Count) sb.Append(",");
                }

                var asm = op + " " + sb.ToString();

                var decoded = TestOpcode(asm, op);

                validator?.Invoke(opcode, decoded);
            }
        }

        [Fact]
        [Logging]
        public void TestOperands()
        {
            _TestOpcodes((o, d) =>
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

        [Fact]
        [Logging]
        public void TestPsuedoOpcodes()
        {
            TestOpcode("nop", "sll");
            TestOpcode("b 0", "beq");
            TestOpcode("jal 0,0", "lui");
        }
    }
}
