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
            intr.SetInstructionDebugMode(InstructionDebugMode.Full);
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
            return programLines;
        }

        private static String LinesToString(IEnumerable<String> lines) {
            StringBuilder sb = new StringBuilder();

            foreach (var line in lines)
                sb.AppendLine(line);

            return sb.ToString();
        }

        private static void ExpectTraceLog(String[] programLines, String[] expectedTrace = null) {
            BassSymbolSource symbolSource = null;
            var asmStream = Asm.Assemble(ref symbolSource, programLines);
            var core = BuildTraceableCore(asmStream, 0);
            var tracer = core.TraceLog;

            core.Disassembler.AttachSymbolProvider(symbolSource);

            if (expectedTrace == null)
                tracer.SetTestMode();

            while (!core.InfiniteLoopWarn) {
                core.Step();
            }

            /* Trace the delay slot */
            core.Step();

            var traceLog = tracer.GenerateTraceLog();

            if (expectedTrace == null) {
                expectedTrace = tracer.TestLog.ToArray();
            }

            String expectedStr = LinesToString(expectedTrace);
            String actualStr = LinesToString(traceLog);

            Console.WriteLine("Expected: \n" + expectedStr);
            Console.WriteLine("Actual: \n" + actualStr);

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
        public void LoopTrace() {
            ExpectTraceLog(
                _Asm(
                    "addi 2,0,$0005",
                    "loop:",
                    "nop",
                    "bne 1,2, loop",
                    "addi 1,1,$0001",
                    "end:",
                    "j end",
                    "nop"
                ),
                _Asm(
                    "00000000 addi 2,0,$0005",
                    "loop:",
                    "00000004 nop",
                    "00000008 bne 1,2,loop",
                    "0000000C addi 1,1,$0001",
                    "( Repeats for 4 time(s) )",
                    "end:",
                    "00000010 j end end",
                    "00000014 nop"
                )
            );
        }
    }
}