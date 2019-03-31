using System;
using cor64.IO;

namespace cor64.Mips
{
    public class FloatControlRegister : SpecialRegister32
    {
        private uint m_Value;

        public override uint Value
        {
            get { return m_Value; }
            set
            {
                m_Value = value;
            }
        }

        protected override void InternalSet(uint value)
        {
            m_Value = value;
        }

        public RoundingMode RoundMode
        {
            get => (RoundingMode)ReadField(1, 2);
            set => WriteField(1, 2, (byte)value);
        }

        public FpuExceptionFlags Flags
        {
            get => (FpuExceptionFlags)ReadField(6, 5);
            set => WriteField(6, 5, (byte)value);
        }

        public FpuExceptionFlags Enables
        {
            get => (FpuExceptionFlags)ReadField(11, 5);
            set => WriteField(11, 5, (byte)value);
        }

        public FpuExceptionFlags Cause
        {
            get => (FpuExceptionFlags)ReadField(17, 6);
            set => WriteField(17, 6, (byte)value);
        }

        public bool Condition
        {
            get => ReadField(23, 1) != 0;
            set => WriteField(23, 1, (value ? 1U : 0U));
        }

        public bool FS
        {
            get => ReadField(25, 1) != 0;
            set => WriteField(25, 1, (value ? 1U : 0U));
        }
    }
}
