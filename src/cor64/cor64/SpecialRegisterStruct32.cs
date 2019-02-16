using System;
using cor64.IO;

namespace cor64
{
    public abstract class SpecialRegisterStruct32
    {
        private IntPtr m_ValuePtr;

        protected SpecialRegisterStruct32(IntPtr ptr)
        {
            m_ValuePtr = ptr;
        }

        protected uint ReadField(int bitOffset, int bitCount)
        {
            bitOffset = 31 - bitOffset;
            uint x = m_ValuePtr.AsType_32Swp();
            x = x << bitOffset >> bitOffset;
            return x >> (32 - bitOffset - bitCount);
        }

        // TODO: Write field
    }
}
