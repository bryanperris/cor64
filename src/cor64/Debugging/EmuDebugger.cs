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
    public class EmuDebugger
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        public static EmuDebugger Current { get; private set; }
        private readonly N64System m_Target;
        private long m_TheadCount;

        public event Action<DmaEvent> DmaOperation;
        public event Action DebugBreak;
        public event Action DebugContinue;

        public enum DmaType {
            Unknown,
            SignalProcessor,
            ParallelInterface,
            SerialInterface
        }

        
        // public struct DmaEvent {

        //     public uint address;
        //     public int size;
        // }

        public class DmaEvent {
            public DmaType HwType { get; init; }
            public uint Source { get; init; }
            public uint Dest { get; init; }
            public bool FromRDRAM { get; init; }
            public int Size { get; init; }

            internal DmaEvent(
                DmaType type,
                uint src,
                uint dst,
                bool fromRdram,
                int size
            ) {
                HwType = type;
                Source = src;
                Dest = dst;
                FromRDRAM = fromRdram;
                Size = size;
            }
        }

        public EmuDebugger(N64System target)
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
            StepRspNext = false;

            /* Signal the break active */
            IsBreakActive = true;

            /* Wait until threads have finished tasks */
            while (Interlocked.Read(ref m_TheadCount) > 0)
            {
                Thread.Sleep(100);
            }

            DebugBreak?.Invoke();
        }

        public void Continue()
        {
            IsBreakActive = false;
            DebugContinue?.Invoke();
        }

        public void Step()
        {
            StepNext = true;
            Continue();
        }

        public void StepRsp() {
            StepRspNext = true;
            Continue();
        }

        public bool IsBreakActive { get; private set; }

        public bool StepNext { get; private set; }

        public bool StepRspNext { get; private set; }

        public void ReportDmaFinish(DmaType type, bool fromRdram, uint source, uint dest, int size)
        {
            // #if DEBUG_DMA_CMDS || DEBUG_DMA_CMDS_RSP_ONLY

            // #if DEBUG_DMA_CMDS_RSP_ONLY
            // if (type == DmaType.SignalProcessor) {
            // #endif

            // if (fromRdram) {
            //     Log.Debug("{0} DMA: {1:X8} [RDRAM] to {2:X8} [RCP] size={3:X8}", type.ToString(), source, dest, size);
            // }
            // else {
            //     Log.Debug("{0} DMA: {1:X8} [RCP] to {2:X8} [RDRAM] size={3:X8}", type.ToString(), source, dest, size);
            // }

            // #if DEBUG_DMA_CMDS_RSP_ONLY
            // }
            // #endif

            // #endif

            // if (!CoreConfig.Current.WorkbenchMode)
            //     return;

            // DmaOperation?.Invoke(new DmaEvent(type, source, dest, fromRdram, size));
        }

        [Conditional("DEBUG")]
        public void TurnOnCpuLogging() {
            m_Target.DeviceCPU.SetInstructionDebugMode(InstructionDebugMode.Full);
        }

        public DisasmPrinter CodePrinter { get; }
    }
}
