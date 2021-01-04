using cor64.Mips;
using cor64.Mips.R4300I;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading;

namespace cor64.Debugging
{
    public class Debugger
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public static Debugger Current { get; private set; }
        private readonly N64System m_Target;
        private long m_TheadCount;

        public event Action<DmaEvent> SpDmaWrite;

        
        public struct DmaEvent {
            public uint address;
            public int size;
        }

        public Debugger(N64System target)
        {
            m_Target = target;
            Current = this;
            CodePrinter = new DisasmPrinter(target);
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

        public bool IsBreakActive { get; private set; }

        public bool StepNext { get; private set; }

        [Conditional("DEBUG")]
        public void ReportDmaFinish(string type, bool toRcp, uint source, uint dest, int size)
        {
            switch (type) {
                case "SP": {
                    if (toRcp) {
                         SpDmaWrite?.Invoke(new DmaEvent { address = dest, size = size });
                    }
                    break;
                }

                default: break;
            }

            #if DEBUG_DMA_CMDS

            if (toRcp) {
                Log.Debug("{0} DMA: {1:X8} [RDRAM] to {2:X8} [RCP] size={3:X8}", type, source, dest, size);
            }
            else {
                Log.Debug("{0} DMA: {1:X8} [RCP] to {2:X8} [RDRAM] size={3:X8}", type, source, dest, size);
            }

            #endif
        }

        [Conditional("DEBUG")]
        public void TurnOnCpuLogging() {
            m_Target.DeviceCPU.SetInstructionDebugMode(InstructionDebugMode.Full);
        }

        public DisasmPrinter CodePrinter { get; }
    }
}
