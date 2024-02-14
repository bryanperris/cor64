using System;
using cor64.IO;
using cor64.Mips;
using cor64.Mips.R4300I;

namespace cor64.HLE {
    public class CpuPointer32<TMipsStruct> : CpuStruct
        where TMipsStruct : CpuStruct {

        private uint m_Address;
        private bool m_InvalidPointer = true;

        public override void Populate(InterpreterBaseR4300I core, uint address)
        {
            var read = core.DataRead32(address);

            if (MemHelper.TryVirtualToPhysical(read, out long physicalAddress)) {
                m_Address = (uint)physicalAddress;
                m_InvalidPointer = false;
            }
        }

        public TMipsStruct Read(InterpreterBaseR4300I core) {
                return CpuStruct.NewStruct<TMipsStruct>(core, m_Address);
        }

        public bool IsNull => m_Address == 0;

        public bool Valid => !m_InvalidPointer;
     }
}