using System;
using cor64.Mips;

namespace cor64.HLE.OS.V1 {
    public sealed class DestroyThreadHook : CpuCodeHook
    {
        // Function End: 8032AF64

        private uint m_ArgThread; // T: struct OSThread *

        public DestroyThreadHook(Mips.R4300I.InterpreterBaseR4300I core) : base(core)
        {
            DebugPrintAddressOffset = 0xF0;
        }

        public override ReturnControl Execute()
        {
            /* Read Arguments */
            m_ArgThread = ReadArg32(0);

            // TODO: HLE implementation

            return ReturnControl.Normal;
        }

        public override void DebugPrint()
        {
            var thread = CpuStruct.NewStruct<ThreadStruct>(Core, m_ArgThread);
            Console.WriteLine("Destroy Thread: #{0}", thread.ID);
        }
    }
}