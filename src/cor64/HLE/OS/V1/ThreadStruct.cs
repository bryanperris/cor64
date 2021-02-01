using System;
using cor64.Mips.R4300I;

namespace cor64.HLE.OS.V1 {
    public class ThreadStruct : CpuStruct
    {
        public CpuPointer32<ThreadStruct> Next;
        public int Priority { get; private set; }
        public CpuPointer32<CpuPointer32<ThreadStruct>> Queue { get; private set; }
        public CpuPointer32<ThreadStruct> TlNext { get; private set; }
        public ushort State { get; private set; }
        public ushort Flags { get; private set; }
        public int ID { get; private set; }
        public int FP { get; private set; }

        // TODO: Thread profiler pointer
        // TODO: Thread context pointer

        public override void Populate(InterpreterBaseR4300I core, uint address)
        {
            Next = NewStruct<CpuPointer32<ThreadStruct>>(core, address); address += 4;
            Priority = (int)core.DataRead32(address); address += 4;
            Queue = NewStruct<CpuPointer32<CpuPointer32<ThreadStruct>>>(core, address); address += 4;
            TlNext = NewStruct<CpuPointer32<ThreadStruct>>(core, address); address += 4;
            State = core.DataRead16(address); address += 2;
            Flags = core.DataRead16(address); address += 2;
            ID = (int)core.DataRead32(address); address += 4;
            FP = (int)core.DataRead32(address); address += 4;
        }
    }
}