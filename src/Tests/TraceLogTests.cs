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

namespace Tests {
    [TestFixture]
    public class TraceLogTests : BaseTest {
        private static Interpreter BuildTraceableCore(
            Stream rom,
            long injectOffset
        ) {
            var intr = new Interpreter("strict");
            intr.SetTraceMode(TraceMode.Full);
            // intr.SetInstructionDebugMode(InstructionDebugMode.Full);
            intr.Cop0.UseDirectAddresses();
            intr.BypassMMU = true;

            var testMem = new MemoryStream(1024 * 1024 * 1);

            rom.Position = 0;
            testMem.Position = injectOffset;

            byte[] romBytes = new byte[rom.Length];

            rom.Read(romBytes, 0, romBytes.Length);
            testMem.Write(romBytes, 0, romBytes.Length);

            testMem.Position = injectOffset;

            intr.AttachIStream(testMem);

            return intr;
        }

        private static String[] _Asm(params String[] programLines) {
            if (programLines.Length == 1) {
                var trimmedLines = from line in programLines[0].Split('\n') select line.Trim();
                var filteredLines = from line in trimmedLines where !string.IsNullOrEmpty(line) select line;
                programLines = filteredLines.ToArray();
            }

            return programLines;
        }

        private static AsmBlock _Asm(int address, params String[] programLines) {
            var temp = programLines.ToList();
            temp.Insert(0, "base " + address.ToString());
            return new AsmBlock(address, temp.ToArray());
        }
        
        private static AsmBlock[] _Blocks(params AsmBlock[] blocks) {
            return blocks;
        }

        private static String LinesToString(IEnumerable<String> lines) {
            StringBuilder sb = new StringBuilder();

            foreach (var line in lines)
                sb.AppendLine(line);

            return sb.ToString();
        }

        private class AsmBlock {
            public int Address { get; }
            public string[] Code { get; }

            public AsmBlock(int address, string[] code) {
                Address = address;
                Code = code;
            }
        }

        private static void ExpectTraceLog(AsmBlock[] asmBlocks, String[] expectedTrace = null) {
            BassSymbolSource symbolSource = null;

            MemoryStream mem = new MemoryStream
            {
                Position = 0
            };

            foreach (var block in from b in asmBlocks orderby b.Address select b) {
                var asmStream = Asm.Assemble(ref symbolSource, block.Code);
                asmStream.Position = 0;

                for (var i = mem.Position; i < block.Address; i++) {
                    mem.WriteByte(0);
                }

                asmStream.CopyTo(mem);
            }

            var core = BuildTraceableCore(mem, 0);
            var tracer = core.TraceLog;

            core.Disassembler.AttachSymbolProvider(symbolSource);

            if (expectedTrace == null)
                tracer.EnableLogVerfication();

            var stepsTaken = 1;

            while (!core.InfiniteLoopWarn) {
                core.Step();
                stepsTaken++;
            }

            /* Trace the delay slot */
            core.Step();

            tracer.StoppedAt = core.ReadPC() + 4;
            var traceLog = tracer.GenerateTraceLog();

            if (expectedTrace == null) {
                expectedTrace = tracer.TestLog.ToArray();

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

            for (int i = 0; i < expectedTrace.Length; i++) {
                var expectedLine = expectedTrace[i];
                var traceLine = traceLog[i].Replace("\r", "");

                if (traceLine.IndexOf('/') > 0) {
                    traceLine = traceLine.Substring(0, traceLine.IndexOf('/') + 1);
                    traceLine = traceLine.Trim();
                }

                Assert.AreEqual(expectedLine, traceLine);
            }
        }

        private static void ExpectTraceLog(String[] programLines, String[] expectedTrace = null) {
            ExpectTraceLog(new AsmBlock[] { new AsmBlock(0, programLines) }, expectedTrace);
        }

        [Test]
        public void SingleBlockTrace() {
            ExpectTraceLog(
                _Asm(
                    "nop",
                    "nop",
                    "nop",
                    "j $0000000C",
                    "nop"
                ),

                _Asm(
                    "00000000 nop",
                    "00000004 nop",
                    "00000008 nop",
                    "0000000C j $0000000C",
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
        public void SimpleGoto() {
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
                    "00000004 beq 0,0,$0004",
                    "00000008 nop",
                    "00000010 <func> nop",
                    "00000014 <end> j end",
                    "00000018 nop"
                )
            );
        }

        [Test]
        public void MultipleBlockTrace() {
            ExpectTraceLog(
                _Asm(
                    "nop",
                    "nop",
                    "nop",
                    "j $00000028",
                    "nop",
                    "nop",
                    "nop",
                    "nop",
                    "j $00000020",
                    "nop",
                    "nop",
                    "nop",
                    "nop",
                    "j $00000014",
                    "nop"
                ),

                _Asm(
                    "00000000 nop",
                    "00000004 nop",
                    "00000008 nop",
                    "0000000C j $00000028",
                    "00000010 nop",
                    "00000028 nop",
                    "0000002C nop",
                    "00000030 nop",
                    "00000034 j $00000014",
                    "00000038 nop",
                    "00000014 nop",
                    "00000018 nop",
                    "0000001C nop",
                    "00000020 j $00000020",
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
        public void MergeBlockTrace() {
            ExpectTraceLog(
                _Asm(
                    "nop",
                    "j blockA",
                    "nop",

                    "blockB:",
                    "nop",
                    "addi 1,0,$0001",
                    "nop",

                    "blockA:",
                    "nop",
                    "nop",
                    "nop",
                    "nop",
                    "j blockC",
                    "nop",

                    "blockD:",
                    "nop",
                    "nop",
                    "nop",
                    "bne 1,0, end",
                    "nop",
                    "j blockB",
                    "nop",

                    "blockC:",
                    "nop",
                    "nop",
                    "nop",
                    "j blockD",
                    "nop",

                    "end:",
                    "nop",
                    "loop:",
                    "j loop",
                    "nop"
                )
            );
        }

        [Test]
        public void SimpleLoopTrace() {
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
                    "00000004 <loop> bne 1,2,loop",
                    "00000008 addi 1,1,$0001",
                    "( Repeats for 5 time(s) )",
                    "0000000C <end> j end",
                    "00000010 nop"
                )
            );
        }

        [Test]
        public void InterruptedBlockTrace() {
            ExpectTraceLog(
                _Blocks(
                    _Asm(0x180, "nop", "mtc0 0,$C", "eret"),
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
                    "00000188 eret",
                    "/* Exception Handler End */",
                    "00000010 nop",
                    "00000014 j $00000014",
                    "00000018 nop"
                )
            );
        }

        [Test]
        public void InterruptedTwiceBlockTrace() {
            ExpectTraceLog(
                _Blocks(
                    _Asm(0x180, "nop", "mtc0 0,$C", "eret"),
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
                    "00000188 eret",
                    "/* Exception Handler End */",
                    "00000010 nop",
                    "00000014 addi 1,0,$0001",
                    "00000018 mtc0 1,12",
                    "0000001C break $00000000",
                    "/* Exception Handler Begin */",
                    "00000180 nop",
                    "00000184 mtc0 0,12",
                    "00000188 eret",
                    "/* Exception Handler End */",
                    "00000020 nop",
                    "00000024 j $00000024",
                    "00000028 nop"
                )
            );
        }

        [Test]
        public void InterruptedBlockLoopTrace() {
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
                    _Asm(0x180, "nop", "mtc0 0,$C", "eret"),
                    _Asm(0x000,
                        "addi 2,0,$0002",
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
                    00000000 addi 2,0,$0002
                    00000004 addi 1,0,$0001
                    00000008 mtc0 1,12
                    0000000C beql 3,1,$000C
                    00000010 break $00000000 [NULLIFIED]
                    00000014 nop
                    00000018 bne 3,2,$0008
                    0000001C addi 3,3,$0001
                    0000000C beql 3,1,$000C
                    00000010 break $00000000
                    /* Exception Handler Begin */
                    00000180 nop
                    00000184 mtc0 0,12
                    00000188 eret
                    /* Exception Handler End */
                    00000028 nop
                    0000002C j $00000014
                    00000030 nop
                    00000014 nop
                    00000018 bne 3,2,$0008
                    0000001C addi 3,3,$0001
                    0000000C beql 3,1,$000C
                    00000010 break $00000000 [NULLIFIED]
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
        public void ThreadSwitchTrace() {
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