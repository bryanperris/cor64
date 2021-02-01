using System;
using cor64.Mips;
using cor64.Mips.R4300I;

namespace cor64.HLE {
    public class CpuPointer32<TMipsStruct> : CpuStruct
        where TMipsStruct : CpuStruct {

        private uint m_Address;

        public override void Populate(InterpreterBaseR4300I core, uint address)
        {
            m_Address = core.DataRead32(address);
        }

        public TMipsStruct Read(InterpreterBaseR4300I core) {
                return CpuStruct.NewStruct<TMipsStruct>(core, m_Address);
        }

        public bool IsNull => m_Address == 0;
     }
}