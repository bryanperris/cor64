using System.ComponentModel;
using System.Linq.Expressions;
using System.Text;
using System.Linq;
using System;
using System.IO;
using cor64.Mips;
using cor64.Mips.Analysis;
using cor64.Mips.R4300I;
using NUnit.Framework;
using static cor64.Mips.Analysis.ProgramTrace;
using cor64;
using System.Collections.Generic;

namespace Tests
{
    [TestFixture]
    public class TraceLogTests : BaseTest
    {
        private static Interpreter BuildTraceableCore(
            Stream rom,
            long injectOffset
        )
        {
            var intr = new Interpreter("strict");
            intr.SetTraceMode(TraceMode.Full);
            // intr.SetInstructionDebugMode(InstructionDebugMode.Full);
            intr.Cop0.UseDirectAddresses();
            intr.SetMMUBypass(true);

            N64MemoryController systemMemory = new N64MemoryController();
            systemMemory.Init();

            TestContext.WriteLine("Write asm into {0:X8}", injectOffset);

            rom.Position = 0;
            for (int i = 0; i < rom.Length; i++)
            {
                systemMemory.U8(i + injectOffset, (byte)rom.ReadByte());
            }

            intr.AttachMemory(systemMemory);

            return intr;
        }

        private static String[] _Asm(params String[] programLines)
        {
            if (programLines.Length == 1)
            {
                var trimmedLines = from line in programLines[0].Split('\n') select line.Trim();
                var filteredLines = from line in trimmedLines where !string.IsNullOrEmpty(line) select line;
                programLines = filteredLines.ToArray();
            }

            return programLines;
        }

        private static AsmBlock _Asm(int address, params String[] programLines)
        {
            var temp = programLines.ToList();
            temp.Insert(0, "base " + address.ToString());
            return new AsmBlock(address, temp.ToArray());
        }

        private static AsmBlock[] _Blocks(params AsmBlock[] blocks)
        {
            return blocks;
        }

        private static String LinesToString(IEnumerable<String> lines)
        {
            StringBuilder sb = new StringBuilder();

            foreach (var line in lines)
                sb.AppendLine(line);

            return sb.ToString();
        }

        private class AsmBlock
        {
            public int Address { get; }
            public string[] Code { get; }

            public AsmBlock(int address, string[] code)
            {
                Address = address;
                Code = code;
            }
        }

        private static void ExpectTraceLog(AsmBlock[] asmBlocks, String[] expectedTrace = null)
        {
            BassSymbolSource symbolSource = null;

            MemoryStream mem = new MemoryStream
            {
                Position = 0
            };

            List<string> verifyLog = new List<string>();

            foreach (var block in from b in asmBlocks orderby b.Address select b)
            {
                var asmStream = Asm.Assemble(ref symbolSource, block.Code);
                asmStream.Position = 0;

                for (var i = mem.Position; i < block.Address; i++)
                {
                    mem.WriteByte(0);
                }

                asmStream.CopyTo(mem);
            }

            var core = BuildTraceableCore(mem, 0);
            var tracer = core.TraceLog;
            tracer.DisableAdvancedTraceReduction = true;

            core.Disassembler.AttachSymbolProvider(symbolSource);

            if (expectedTrace == null)
                tracer.OnInstTrace += (pc, inst) => verifyLog.Add($"{pc:X8} {tracer.Disassembler.Disassemble(pc, inst)}");

            var stepsTaken = 1;

            while (!core.InfiniteLoopWarn)
            {
                core.Step();
                stepsTaken++;
            }

            /* Trace the delay slot */
            core.Step();

            var traceLog = tracer.GenerateTraceLog();

            if (expectedTrace == null)
            {
                expectedTrace = verifyLog.ToArray();

                // Filter out real log comments
                traceLog = (from l in traceLog.ToList() where !l.StartsWith("/*") select l).ToList();
                traceLog = (from l in traceLog.ToList() where !l.EndsWith("~") select l).ToList();
            }

            String expectedStr = LinesToString(expectedTrace);
            String actualStr = LinesToString(traceLog);

            Console.WriteLine("Expected: \n" + expectedStr);
            Console.WriteLine("Actual: \n" + actualStr);
            Console.WriteLine("Core Steps Taken: {0}", stepsTaken);

            Assert.AreEqual(expectedTrace.Length, traceLog.Count);

            for (int i = 0; i < expectedTrace.Length; i++)
            {
                var expectedLine = expectedTrace[i];
                var traceLine = traceLog[i].Replace("\r", "");

                if (traceLine.IndexOf('/') > 0)
                {
                    traceLine = traceLine.Substring(0, traceLine.IndexOf('/') + 1);
                    traceLine = traceLine.Trim();
                }

                Assert.AreEqual(expectedLine, traceLine);
            }
        }

        private static void ExpectTraceLog(String[] programLines, String[] expectedTrace = null)
        {
            ExpectTraceLog(new AsmBlock[] { new AsmBlock(0, programLines) }, expectedTrace);
        }

        [Test]
        public void SingleBlockTrace()
        {
            ExpectTraceLog(
                _Asm(
                    "nop",
                    "nop",
                    "nop",
                    "j $000C",
                    "nop"
                ),

                _Asm(
                    "00000000 nop",
                    "00000004 nop",
                    "00000008 nop",
                    "0000000C j $0003",
                    "00000010 nop"
                )
            );

            ExpectTraceLog(
                _Asm(
                    "nop",
                    "nop",
                    "nop",
                    "j $0000000C",
                    "nop"
                )
            );
        }

        [Test]
        public void SimpleGoto()
        {
            ExpectTraceLog(
                _Asm(
                    "nop",
                    "beq 0,0, func",
                    "nop",
                    "nop",
                    "func:",
                    "nop",
                    "end:",
                    "j end",
                    "nop"
                ),

                _Asm(
                    "00000000 nop",
                    "00000004 beq 0,0,$0002", // (@func - @delayslot) / 4
                    "00000008 nop",
                    "00000010 <func> nop",
                    "00000014 <end> j $0005",
                    "00000018 nop"
                )
            );
        }

        [Test]
        public void MultipleBlockTrace()
        {
            ExpectTraceLog(
                _Asm(
                    "block_a:",
                    "nop",
                    "nop",
                    "nop",
                    "j block_c",
                    "nop",

                    "block_b:",
                    "nop",
                    "nop",
                    "nop",
                    "end:",
                    "j end",
                    "nop",

                    "block_c:",
                    "nop",
                    "nop",
                    "nop",
                    "j block_b",
                    "nop"
                ),

                _Asm(
                    "00000000 <block_a> nop",
                    "00000004 nop",
                    "00000008 nop",
                    "0000000C j $000A", // @block_c / 4
                    "00000010 nop",
                    "00000028 <block_c> nop",
                    "0000002C nop",
                    "00000030 nop",
                    "00000034 j $0005", // @block_b / 4
                    "00000038 nop",
                    "00000014 <block_b> nop",
                    "00000018 nop",
                    "0000001C nop",
                    "00000020 <end> j $0008", // @end / 4
                    "00000024 nop"
                )
            );

            ExpectTraceLog(
                _Asm(
                    "nop",
                    "nop",
                    "nop",
                    "j blockA",
                    "nop",
                    "blockB:",
                    "nop",
                    "nop",
                    "nop",
                    "loop:",
                    "j loop",
                    "nop",
                    "blockA:",
                    "nop",
                    "nop",
                    "nop",
                    "j blockB",
                    "nop"
                )
            );
        }

        [Test]
        public void SimpleLoopTrace()
        {
            ExpectTraceLog(
                _Asm(
                    "addi 2,0,$0005",
                    "loop:",
                    "bne 1,2, loop",
                    "addi 1,1,$0001",
                    "end:",
                    "j end",
                    "nop"
                ),
                _Asm(
                    "00000000 addi 2,0,$0005",
                    "00000004 <loop> bne 1,2,$FFFF",
                    "00000008 addi 1,1,$0001",
                    "00000004 <loop> bne 1,2,$FFFF",
                    "00000008 addi 1,1,$0001",
                    "( Repeats for 4 time(s) )",
                    "0000000C <end> j $0003",
                    "00000010 nop"
                )
            );
        }

        // Based on chatgpt, it should be 3 times

        [Test]
        public void MultiBlockLoopTraceOptimized()
        {
            ExpectTraceLog(
                _Asm(
                    "main:",
                    "addi 9, 0",
                    "addi 10, 0",
                    "addi 11, 5",

                    "outer_loop:",
                    "bge 9, 11, exit",
                    "addi 9, 9, 1",

                    "addi 10, 0, 0",
                    "inner_loop:",
                    "bge 10, 11, outer_loop",
                    "addi 10, 10, 1",
                    "j inner_loop",

                    "exit:",
                    "j exit"
                )
                // _Asm(
                //     "00000000 addi 2,0,$0005",
                //     "00000004 j $0009",
                //     "00000008 nop",
                //     "00000024 <loop> j $0006",
                //     "00000028 nop",
                //     "00000018 <func1> nop",
                //     "0000001C j $0003",
                //     "00000020 nop",
                //     "0000000C <func2> nop",
                //     "00000010 j $000B",
                //     "00000014 nop",
                //     "0000002C <return> bne 1,2,$FFFD",
                //     "00000030 addi 1,1,$0001",
                //     "Trace 00000024-->00000030 repeats 5 times",
                //     "00000034 <end> j $000D",
                //     "00000038 nop"
                // )
            );
        }

        /*
        From chatgpt
        [PC: 00400000] 00000000: addi 2,0,$0005
        [PC: 00400004] 00000004: j loop
        [PC: 00400008] 00000008: nop
        [PC: 0040000C] 0000000C: nop
        [PC: 00400010] 00000010: nop
        [PC: 00400014] 00000014: j func1
        [PC: 00400018] 00000018: nop
        [PC: 0040001C] 0000001C: j func2
        [PC: 00400020] 00000020: nop
        [PC: 00400024] 00000024: nop
        [PC: 00400028] 00000028: j return
        [PC: 0040002C] 0000002C: nop
        [PC: 00400030] 00000030: nop
        [PC: 00400034] 00000034: j end
        [PC: 00400038] 00000038: nop
        [PC: 0040003C] 0000003C: bne 1,2, loop
        [PC: 00400040] 00000040: addi 1,1,$0001
        [PC: 00400044] 00000044: j end
        [PC: 00400048] 00000048: nop
        */

        [Test]
        public void InterruptedBlockTrace()
        {
            ExpectTraceLog(
                _Blocks(
                    _Asm(0x180, "nop", "mtc0 0,$C", "mfc0 5,14", "addiu 5,5,4", "mtc0 5,14", "eret"),
                    _Asm(0x000, "nop", "addi 1,0,$1", "mtc0 1,$C", "break", "nop", "end:", "j end", "nop")
                ),
                _Asm(
                    "00000000 nop",
                    "00000004 addi 1,0,$0001",
                    "00000008 mtc0 1,12",
                    "0000000C break $00000000",
                    "/* Exception Handler Begin */",
                    "00000180 nop",
                    "00000184 mtc0 0,12",
                    "00000188 mfc0 14,5",
                    "0000018C addiu 5,5,$0004",
                    "00000190 mtc0 5,14",
                    "00000194 eret",
                    "/* Exception Handler End */",
                    "00000010 nop",
                    "00000014 j $00000014",
                    "00000018 nop"
                )
            );
        }

        [Test]
        public void InterruptedTwiceBlockTrace()
        {
            ExpectTraceLog(
                _Blocks(
                    _Asm(0x180, "nop", "mtc0 0,$C", "mfc0 5,14", "addiu 5,5,4", "mtc0 5,14", "eret"),
                    _Asm(0x000, "nop", "addi 1,0,$1", "mtc0 1,$C", "break", "nop", "addi 1,0,$1", "mtc0 1,$C", "break", "nop", "end:", "j end", "nop")
                ),
                _Asm(
                    "00000000 nop",
                    "00000004 addi 1,0,$0001",
                    "00000008 mtc0 1,12",
                    "0000000C break $00000000",
                    "/* Exception Handler Begin */",
                    "00000180 nop",
                    "00000184 mtc0 0,12",
                    "00000188 mfc0 14,5",
                    "0000018C addiu 5,5,$0004",
                    "00000190 mtc0 5,14",
                    "00000194 eret",
                    "/* Exception Handler End */",
                    "00000010 nop",
                    "00000014 addi 1,0,$0001",
                    "00000018 mtc0 1,12",
                    "0000001C break $00000000",
                    "/* Exception Handler Begin */",
                    "00000180 nop",
                    "00000184 mtc0 0,12",
                    "00000188 mfc0 14,5",
                    "0000018C addiu 5,5,$0004",
                    "00000190 mtc0 5,14",
                    "00000194 eret",
                    "/* Exception Handler End */",
                    "00000020 nop",
                    "00000024 j $00000024",
                    "00000028 nop"
                )
            );
        }

        [Test]
        public void InterruptedBlockLoopTrace()
        {
            /*
                00000000 addi 2,0,$0002
                00000004 addi 1,0,$1
                00000008 mtc0 1,$C
                0000000C loop: beql 3,1, func
                00000010 break
                00000014 cont: nop
                00000018 bne 3,2, loop
                0000001C addi 3,3,$0001
                00000020 end: j end
                00000024 nop
                00000028 func: nop
                0000002C j cont
            */

            ExpectTraceLog(
                _Blocks(
                    _Asm(0x180, "nop", "mtc0 0,$C", "addi 3,3,$0001", "eret"),
                    _Asm(0x000,
                        "addi 2,0,$0003",
                        "addi 1,0,$1",
                        "mtc0 1,$C",
                        "loop:",
                        "beql 3,1, func",
                        "break", // Throw the break exception in the delay slot only when the branch condition is true
                        "cont:",
                        "nop",
                        "bne 3,2, loop",
                        "addi 3,3,$0001",
                        "end:",
                        "j end",
                        "nop",
                        "func:",
                        "nop",
                        "j cont"
                    )
                ),
                _Asm(
                  @"
                    00000000 addi 2,0,$0003
                    00000004 addi 1,0,$0001
                    00000008 mtc0 1,12
                    0000000C beql 3,1,$000C
                    00000014 nop
                    00000018 bne 3,2,$0008
                    0000001C addi 3,3,$0001
                    0000000C beql 3,1,$000C
                    00000010 break $00000000
                    /* Exception Handler Begin */
                    00000180 nop
                    00000184 mtc0 0,12
                    00000188 addi 3,3,$0001
                    0000018C eret
                    /* Exception Handler End */
                    0000000C beql 3,1,$000C
                    00000014 nop
                    00000018 bne 3,2,$0008
                    0000001C addi 3,3,$0001
                    0000000C beql 3,1,$000C
                    00000014 nop
                    00000018 bne 3,2,$0008
                    0000001C addi 3,3,$0001
                    00000020 j $00000020
                    00000024 nop
                    "
                )
            );
        }


        [Test]
        public void ThreadSwitchTrace()
        {
            ExpectTraceLog(
                _Blocks(
                    _Asm(0x180,
                        "nop",
                        "mtc0 0,$C",
                        "addi 2,0,$0020",
                        "mtc0 2,14",
                        "eret"
                    ),

                    _Asm(0x000,
                        "addi 1,0,$1",
                        "mtc0 1,$C",
                        "break"
                    // "end:",
                    // "j end",
                    // "nop"
                    ),

                    _Asm(0x020,
                        "nop",
                        "nop",
                        "end:",
                        "j end",
                        "nop"
                    )
                ),
                _Asm(
                    @"
                        00000000 addi 1,0,$0001
                        00000004 mtc0 1,12
                        00000008 break $00000000
                        /* Exception Handler Begin */
                        00000180 nop
                        00000184 mtc0 0,12
                        00000188 addi 2,0,$0020
                        0000018C mtc0 2,14
                        00000190 eret
                        /* Exception Handler End */
                        00000020 nop
                        00000024 nop
                        00000028 j $00000028
                        0000002C nop
                    "
                )
            );
        }
    }
}