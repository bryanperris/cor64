using System;
using cor64.IO;
using cor64.Mips;

namespace cor64.HLE.OS.V1 {
    public sealed class CreateThreadHook : CpuCodeHook
    {
        // Function Size: 00000144
        private uint m_ArgThread; // T: struct OSThread *

        public CreateThreadHook(Mips.R4300I.InterpreterBaseR4300I core) : base(core)
        {
            DebugPrintAddressOffset = 0x130;
        }

        public override ReturnControl Execute()
        {
            /* Read Arguments */
            m_ArgThread = (uint)MemHelper.VirtualToPhysical(ReadArg32(0));

            // TODO: HLE implementation

            return ReturnControl.Normal;
        }

        public override void DebugPrint()
        {
            var thread = CpuStruct.NewStruct<ThreadStruct>(Core, m_ArgThread);
            Console.WriteLine("New Thread: #{0} PRI={1}", thread.ID, thread.Priority);
        }
    }
}