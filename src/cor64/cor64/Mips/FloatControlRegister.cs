using System;
using cor64.IO;

namespace cor64.Mips
{
    public class FloatControlRegister
    {
        private uint m_Value;
        private BitFiddler m_Fiddler = new BitFiddler();
        
        const int F_ROUNDMODE = 0;
        const int F_FLAGS = 1;
        const int F_ENABLES = 2;
        const int F_CAUSE = 3;
        const int F_COND = 4;
        const int F_FS = 5;

        public FloatControlRegister()
        {
            m_Fiddler.DefineField(00, 2);
            m_Fiddler.DefineField(02, 5);
            m_Fiddler.DefineField(07, 5);
            m_Fiddler.DefineField(12, 6);
            m_Fiddler.DefineField(23, 1);
            m_Fiddler.DefineField(24, 1);
        }

        public uint Value
        {
            get 
            {
                return m_Value;
            }

            set
            {
                m_Value = value;
            }
        }

        public RoundingMode RoundMode
        {
            get => (RoundingMode)m_Fiddler.X(F_ROUNDMODE, ref m_Value);
            set => m_Fiddler.J(F_ROUNDMODE, ref m_Value, (byte)value);
        }

        public FpuExceptionFlags Flags
        {
            get => (FpuExceptionFlags)m_Fiddler.X(F_FLAGS, ref m_Value);
            set => m_Fiddler.J(F_FLAGS, ref m_Value, (byte)value);
        }

        public FpuExceptionFlags Enables
        {
            get => (FpuExceptionFlags)m_Fiddler.X(F_ENABLES, ref m_Value);
            set => m_Fiddler.J(F_ENABLES, ref m_Value, (byte)value);
        }

        public FpuExceptionFlags Cause
        {
            get => (FpuExceptionFlags)m_Fiddler.X(F_CAUSE, ref m_Value);
            set => m_Fiddler.J(F_CAUSE, ref m_Value, (byte)value);
        }

        public bool Condition
        {
            get => m_Fiddler.X(F_COND, ref m_Value) != 0;
            set => m_Fiddler.J(F_COND, ref m_Value, (value ? 1U : 0U));
        }

        public bool FS
        {
            get => m_Fiddler.X(F_FS, ref m_Value) != 0;
            set => m_Fiddler.J(F_FS, ref m_Value, (value ? 1U : 0U));
        }
    }
}
