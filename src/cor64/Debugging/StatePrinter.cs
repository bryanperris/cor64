using System;
using System;
using System.IO;
using cor64.Mips;

namespace cor64.Debugging {
    public static class StatePrinter {
        public static void Print_R4300I_General(N64System targetMachine, Stream printStream) {
            StreamWriter writer = new(printStream);

            var disasm = targetMachine.DeviceCPU.Disassembler.Disassemble(targetMachine.DeviceCPU.CurrentInst);

            writer.WriteLine("Break Instruction: " + disasm);
            writer.WriteLine("\nMIPS R4300I Registers");

            var state = targetMachine.DeviceCPU.State;

            writer.WriteLine("{0}: {1:X16} {2}: {3:X16} {4}: {5:X16} {6}: {7:X16}",
                ABI.GetLabel("o32", ABI.RegType.GPR, 0), state.GetGpr64(0),
                ABI.GetLabel("o32", ABI.RegType.GPR, 1), state.GetGpr64(1),
                ABI.GetLabel("o32", ABI.RegType.GPR, 2), state.GetGpr64(2),
                ABI.GetLabel("o32", ABI.RegType.GPR, 3), state.GetGpr64(3));

            writer.WriteLine("  {0}: {1:X16} {2}: {3:X16} {4}: {5:X16} {6}: {7:X16}",
                ABI.GetLabel("o32", ABI.RegType.GPR, 4), state.GetGpr64(4),
                ABI.GetLabel("o32", ABI.RegType.GPR, 5), state.GetGpr64(5),
                ABI.GetLabel("o32", ABI.RegType.GPR, 6), state.GetGpr64(6),
                ABI.GetLabel("o32", ABI.RegType.GPR, 7), state.GetGpr64(7));

            writer.WriteLine("  {0}: {1:X16} {2}: {3:X16} {4}: {5:X16} {6}: {7:X16}",
                ABI.GetLabel("o32", ABI.RegType.GPR, 8), state.GetGpr64(8),
                ABI.GetLabel("o32", ABI.RegType.GPR, 9), state.GetGpr64(9),
                ABI.GetLabel("o32", ABI.RegType.GPR, 10), state.GetGpr64(10),
                ABI.GetLabel("o32", ABI.RegType.GPR, 11), state.GetGpr64(11));

            writer.WriteLine("  {0}: {1:X16} {2}: {3:X16} {4}: {5:X16} {6}: {7:X16}",
                ABI.GetLabel("o32", ABI.RegType.GPR, 12), state.GetGpr64(12),
                ABI.GetLabel("o32", ABI.RegType.GPR, 13), state.GetGpr64(13),
                ABI.GetLabel("o32", ABI.RegType.GPR, 14), state.GetGpr64(14),
                ABI.GetLabel("o32", ABI.RegType.GPR, 15), state.GetGpr64(15));

            writer.WriteLine("  {0}: {1:X16} {2}: {3:X16} {4}: {5:X16} {6}: {7:X16}",
                ABI.GetLabel("o32", ABI.RegType.GPR, 16), state.GetGpr64(16),
                ABI.GetLabel("o32", ABI.RegType.GPR, 17), state.GetGpr64(17),
                ABI.GetLabel("o32", ABI.RegType.GPR, 18), state.GetGpr64(18),
                ABI.GetLabel("o32", ABI.RegType.GPR, 19), state.GetGpr64(19));

            writer.WriteLine("  {0}: {1:X16} {2}: {3:X16} {4}: {5:X16} {6}: {7:X16}",
                ABI.GetLabel("o32", ABI.RegType.GPR, 20), state.GetGpr64(20),
                ABI.GetLabel("o32", ABI.RegType.GPR, 21), state.GetGpr64(21),
                ABI.GetLabel("o32", ABI.RegType.GPR, 22), state.GetGpr64(22),
                ABI.GetLabel("o32", ABI.RegType.GPR, 23), state.GetGpr64(23));

            writer.WriteLine("  {0}: {1:X16} {2}: {3:X16} {4}: {5:X16} {6}: {7:X16}",
                ABI.GetLabel("o32", ABI.RegType.GPR, 24), state.GetGpr64(24),
                ABI.GetLabel("o32", ABI.RegType.GPR, 25), state.GetGpr64(25),
                ABI.GetLabel("o32", ABI.RegType.GPR, 26), state.GetGpr64(26),
                ABI.GetLabel("o32", ABI.RegType.GPR, 27), state.GetGpr64(27));

            writer.WriteLine("  {0}: {1:X16} {2}: {3:X16} {4}: {5:X16} {6}: {7:X16}",
                ABI.GetLabel("o32", ABI.RegType.GPR, 28), state.GetGpr64(28),
                ABI.GetLabel("o32", ABI.RegType.GPR, 29), state.GetGpr64(29),
                ABI.GetLabel("o32", ABI.RegType.GPR, 30), state.GetGpr64(30),
                ABI.GetLabel("o32", ABI.RegType.GPR, 31), state.GetGpr64(31));

            writer.WriteLine("  {0}: {1:X16} {2}: {3:X16} {4}: {5:X16} {6}: {7:X16}",
                "pc", targetMachine.DeviceCPU.ReadPC(),
                "lo", state.GetLo(),
                "hi", state.GetHi(),
                "ll", state.LLBit ? 1 : 0);

            writer.Flush();
        }

        public static void Print_RSP_General(N64System targetMachine, Stream printStream) {
            StreamWriter writer = new(printStream);

            var disasm = targetMachine.DeviceRcp.DeviceRsp.Disassembler.Disassemble(targetMachine.DeviceRcp.DeviceRsp.CurrentInst);

            writer.WriteLine("Break Instruction: " + disasm);
            writer.WriteLine("\nMIPS RSP Registers");

            var state = targetMachine.DeviceRcp.DeviceRsp.State;

            writer.WriteLine("{0}: {1:X16} {2}: {3:X16} {4}: {5:X16} {6}: {7:X16}",
                ABI.GetLabel("o32", ABI.RegType.GPR, 0), state.GetGpr64(0),
                ABI.GetLabel("o32", ABI.RegType.GPR, 1), state.GetGpr64(1),
                ABI.GetLabel("o32", ABI.RegType.GPR, 2), state.GetGpr64(2),
                ABI.GetLabel("o32", ABI.RegType.GPR, 3), state.GetGpr64(3));

            writer.WriteLine("  {0}: {1:X16} {2}: {3:X16} {4}: {5:X16} {6}: {7:X16}",
                ABI.GetLabel("o32", ABI.RegType.GPR, 4), state.GetGpr64(4),
                ABI.GetLabel("o32", ABI.RegType.GPR, 5), state.GetGpr64(5),
                ABI.GetLabel("o32", ABI.RegType.GPR, 6), state.GetGpr64(6),
                ABI.GetLabel("o32", ABI.RegType.GPR, 7), state.GetGpr64(7));

            writer.WriteLine("  {0}: {1:X16} {2}: {3:X16} {4}: {5:X16} {6}: {7:X16}",
                ABI.GetLabel("o32", ABI.RegType.GPR, 8), state.GetGpr64(8),
                ABI.GetLabel("o32", ABI.RegType.GPR, 9), state.GetGpr64(9),
                ABI.GetLabel("o32", ABI.RegType.GPR, 10), state.GetGpr64(10),
                ABI.GetLabel("o32", ABI.RegType.GPR, 11), state.GetGpr64(11));

            writer.WriteLine("  {0}: {1:X16} {2}: {3:X16} {4}: {5:X16} {6}: {7:X16}",
                ABI.GetLabel("o32", ABI.RegType.GPR, 12), state.GetGpr64(12),
                ABI.GetLabel("o32", ABI.RegType.GPR, 13), state.GetGpr64(13),
                ABI.GetLabel("o32", ABI.RegType.GPR, 14), state.GetGpr64(14),
                ABI.GetLabel("o32", ABI.RegType.GPR, 15), state.GetGpr64(15));

            writer.WriteLine("  {0}: {1:X16} {2}: {3:X16} {4}: {5:X16} {6}: {7:X16}",
                ABI.GetLabel("o32", ABI.RegType.GPR, 16), state.GetGpr64(16),
                ABI.GetLabel("o32", ABI.RegType.GPR, 17), state.GetGpr64(17),
                ABI.GetLabel("o32", ABI.RegType.GPR, 18), state.GetGpr64(18),
                ABI.GetLabel("o32", ABI.RegType.GPR, 19), state.GetGpr64(19));

            writer.WriteLine("  {0}: {1:X16} {2}: {3:X16} {4}: {5:X16} {6}: {7:X16}",
                ABI.GetLabel("o32", ABI.RegType.GPR, 20), state.GetGpr64(20),
                ABI.GetLabel("o32", ABI.RegType.GPR, 21), state.GetGpr64(21),
                ABI.GetLabel("o32", ABI.RegType.GPR, 22), state.GetGpr64(22),
                ABI.GetLabel("o32", ABI.RegType.GPR, 23), state.GetGpr64(23));

            writer.WriteLine("  {0}: {1:X16} {2}: {3:X16} {4}: {5:X16} {6}: {7:X16}",
                ABI.GetLabel("o32", ABI.RegType.GPR, 24), state.GetGpr64(24),
                ABI.GetLabel("o32", ABI.RegType.GPR, 25), state.GetGpr64(25),
                ABI.GetLabel("o32", ABI.RegType.GPR, 26), state.GetGpr64(26),
                ABI.GetLabel("o32", ABI.RegType.GPR, 27), state.GetGpr64(27));

            writer.WriteLine("  {0}: {1:X16} {2}: {3:X16} {4}: {5:X16} {6}: {7:X16}",
                ABI.GetLabel("o32", ABI.RegType.GPR, 28), state.GetGpr64(28),
                ABI.GetLabel("o32", ABI.RegType.GPR, 29), state.GetGpr64(29),
                ABI.GetLabel("o32", ABI.RegType.GPR, 30), state.GetGpr64(30),
                ABI.GetLabel("o32", ABI.RegType.GPR, 31), state.GetGpr64(31));

            writer.WriteLine("  {0}: {1:X16}",
                "pc", targetMachine.DeviceRcp.DeviceRsp.ReadPC());

            writer.Flush();
        }
    }
}