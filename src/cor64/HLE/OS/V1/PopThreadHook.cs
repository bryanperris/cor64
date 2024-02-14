using System;
using cor64.IO;
using cor64.Mips;

namespace cor64.HLE.OS.V1 {
    public sealed class PopThreadHook : CpuCodeHook
    {
        // Function End: 

        private uint m_ArgQueue; // Queue: (OSThread **)


        private CpuPointer32<CpuPointer32<ThreadStruct>> m_Queue;

        public PopThreadHook(Mips.R4300I.InterpreterBaseR4300I core) : base(core)
        {
            DebugPrintAddressOffset = 0xC;
        }

        public override ReturnControl Execute()
        {
            /* Read Arguments */
            m_ArgQueue = (uint)MemHelper.VirtualToPhysical(ReadArg32(0));

            m_Queue = CpuStruct.NewStruct<CpuPointer32<CpuPointer32<ThreadStruct>>>(Core, m_ArgQueue);

            // TODO: HLE implementation
            // Read address from arg0 pointer, store into v0
            // Read addrsss pointed by v0, store into temp
            // Return $ra
            // Delay Slot: Store temp into arg0 pointer

            return ReturnControl.Normal;
        }

        public override void DebugPrint()
        {
            if (m_Queue.Valid) {
                if (!m_Queue.Read(Core).IsNull) {
                    Console.WriteLine("Pop Thread: Next Thread #{0}", m_Queue.Read(Core).Read(Core).ID);
                }
                else {
                    Console.WriteLine("Pop Thread: No Next Thread");
                }
            }
            else {
                Console.WriteLine("Pop Thread: Empty Queue");
            }
            // Console.WriteLine("Pop Thread: #{0:X8}", );
        }
    }
}