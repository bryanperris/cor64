using cor64.Mips;
using cor64.Mips.R4300I;
using NLog;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace cor64.Debugging
{
    public class Debugger
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public static Debugger Current { get; private set; }
        private N64System m_Target;
        private long m_TheadCount;

        public Debugger(N64System target)
        {
            m_Target = target;
            Current = this;
        }

        public void EnterExecution()
        {
            Interlocked.Increment(ref m_TheadCount);
        }

        public void LeaveExecution()
        {
            Interlocked.Decrement(ref m_TheadCount);
        }

        public void Break()
        {
            StepNext = false;

            /* Signal the break active */
            IsBreakActive = true;

            /* Wait until threads have finished tasks */
            while (Interlocked.Read(ref m_TheadCount) > 0)
            {
                Thread.Sleep(100);
            }
        }

        public void Continue()
        {
            IsBreakActive = false;
        }

        public void Step()
        {
            IsBreakActive = false;
            StepNext = true;
        }

        private void AppendState(IDictionary<string, string> source, IDictionary<string, string> target, String groupName)
        {
            foreach (var key in source.Keys) {
                target.Add(groupName + "_" + key, source[key]);
            }
        }

        public void DumpState(IDictionary<String, String> dictionary)
        {
            AppendState(m_Target.DeviceCPU.SnapSave(), dictionary, "cpu");
        }

        public String DisassembleCpuCode(ulong startAddress, int size)
        {
            StringBuilder sb = new StringBuilder();
            var read = m_Target.DeviceCPU.Disassembler.Disassemble(startAddress, size);

            for (int i = 0; i < read.Length; i++)
            {
                sb.Append(m_Target.DeviceCPU.Disassembler.GetFullDisassembly(read[i]) + ";");
            }

            return sb.ToString();
        }

        public bool IsBreakActive { get; private set; }

        public bool StepNext { get; private set; }

        public void Track_DmaOperation(string type, ulong source, ulong dest, int size)
        {
            Log.Debug("{0} DMA: {1:X8} to {2:X8} size={3:X8}", type, source, dest, size);
        }

        public void PrintCpuState(StringBuilder stringBuilder)
        {
            stringBuilder.AppendLine("Break Instruction: " + m_Target.DeviceCPU.Disassembler.GetFullDisassembly(
                m_Target.DeviceCPU.CurrentInst));

            stringBuilder.AppendLine("\nMIPS R4300I Registers");

            var state = m_Target.DeviceCPU.State;

            stringBuilder.AppendLine(String.Format("{0}: {1:X16} {2}: {3:X16} {4}: {5:X16} {6}: {7:X16}",
                ABI.GetLabel("o32", ABI.RegType.GPR, 0), state.GetGpr64(0),
                ABI.GetLabel("o32", ABI.RegType.GPR, 1), state.GetGpr64(1),
                ABI.GetLabel("o32", ABI.RegType.GPR, 2), state.GetGpr64(2),
                ABI.GetLabel("o32", ABI.RegType.GPR, 3), state.GetGpr64(3)));

            stringBuilder.AppendLine(String.Format("  {0}: {1:X16} {2}: {3:X16} {4}: {5:X16} {6}: {7:X16}",
                ABI.GetLabel("o32", ABI.RegType.GPR, 4), state.GetGpr64(4),
                ABI.GetLabel("o32", ABI.RegType.GPR, 5), state.GetGpr64(5),
                ABI.GetLabel("o32", ABI.RegType.GPR, 6), state.GetGpr64(6),
                ABI.GetLabel("o32", ABI.RegType.GPR, 7), state.GetGpr64(7)));

            stringBuilder.AppendLine(String.Format("  {0}: {1:X16} {2}: {3:X16} {4}: {5:X16} {6}: {7:X16}",
                ABI.GetLabel("o32", ABI.RegType.GPR, 8), state.GetGpr64(8),
                ABI.GetLabel("o32", ABI.RegType.GPR, 9), state.GetGpr64(9),
                ABI.GetLabel("o32", ABI.RegType.GPR, 10), state.GetGpr64(10),
                ABI.GetLabel("o32", ABI.RegType.GPR, 11), state.GetGpr64(11)));

            stringBuilder.AppendLine(String.Format("  {0}: {1:X16} {2}: {3:X16} {4}: {5:X16} {6}: {7:X16}",
                ABI.GetLabel("o32", ABI.RegType.GPR, 12), state.GetGpr64(12),
                ABI.GetLabel("o32", ABI.RegType.GPR, 13), state.GetGpr64(13),
                ABI.GetLabel("o32", ABI.RegType.GPR, 14), state.GetGpr64(14),
                ABI.GetLabel("o32", ABI.RegType.GPR, 15), state.GetGpr64(15)));

            stringBuilder.AppendLine(String.Format("  {0}: {1:X16} {2}: {3:X16} {4}: {5:X16} {6}: {7:X16}",
                ABI.GetLabel("o32", ABI.RegType.GPR, 16), state.GetGpr64(16),
                ABI.GetLabel("o32", ABI.RegType.GPR, 17), state.GetGpr64(17),
                ABI.GetLabel("o32", ABI.RegType.GPR, 18), state.GetGpr64(18),
                ABI.GetLabel("o32", ABI.RegType.GPR, 19), state.GetGpr64(19)));

            stringBuilder.AppendLine(String.Format("  {0}: {1:X16} {2}: {3:X16} {4}: {5:X16} {6}: {7:X16}",
                ABI.GetLabel("o32", ABI.RegType.GPR, 20), state.GetGpr64(20),
                ABI.GetLabel("o32", ABI.RegType.GPR, 21), state.GetGpr64(21),
                ABI.GetLabel("o32", ABI.RegType.GPR, 22), state.GetGpr64(22),
                ABI.GetLabel("o32", ABI.RegType.GPR, 23), state.GetGpr64(23)));

            stringBuilder.AppendLine(String.Format("  {0}: {1:X16} {2}: {3:X16} {4}: {5:X16} {6}: {7:X16}",
                ABI.GetLabel("o32", ABI.RegType.GPR, 24), state.GetGpr64(24),
                ABI.GetLabel("o32", ABI.RegType.GPR, 25), state.GetGpr64(25),
                ABI.GetLabel("o32", ABI.RegType.GPR, 26), state.GetGpr64(26),
                ABI.GetLabel("o32", ABI.RegType.GPR, 27), state.GetGpr64(27)));

            stringBuilder.AppendLine(String.Format("  {0}: {1:X16} {2}: {3:X16} {4}: {5:X16} {6}: {7:X16}",
                ABI.GetLabel("o32", ABI.RegType.GPR, 28), state.GetGpr64(28),
                ABI.GetLabel("o32", ABI.RegType.GPR, 29), state.GetGpr64(29),
                ABI.GetLabel("o32", ABI.RegType.GPR, 30), state.GetGpr64(30),
                ABI.GetLabel("o32", ABI.RegType.GPR, 31), state.GetGpr64(31)));

            stringBuilder.AppendLine(String.Format("  {0}: {1:X16} {2}: {3:X16} {4}: {5:X16} {6}: {7:X16}",
                "pc", m_Target.DeviceCPU.ReadPC(),
                "lo", state.GetLo(),
                "hi", state.GetHi(),
                "ll", (state.LLBit ? 1 : 0)));
        }
    }
}
