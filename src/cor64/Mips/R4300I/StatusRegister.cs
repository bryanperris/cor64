using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using System.Diagnostics;

namespace cor64.Mips.R4300I
{
    public class StatusRegister
    {
        private readonly static Logger Log = LogManager.GetCurrentClassLogger();
        private uint m_Value;
        private BitFiddler m_Fiddler = new BitFiddler();
        private BitFiddler m_IntFiddler = new BitFiddler();

        const int F_KSU = 0;
        const int F_IM = 1;
        const int F_DS = 2;
        const int F_CU = 3;
        const int F_ERL = 4;
        const int F_EXL = 5;

        const int INT_0 = 0;
        const int INT_1 = 1;
        const int INT_2 = 2;
        const int INT_3 = 3;
        const int INT_4 = 4;
        const int INT_5 = 5;
        const int INT_6 = 6;
        const int INT_7 = 7;


        [Flags]
        public enum StatusFlags : uint
        {
            None = 0,
            InterruptsEnabled = 1,
            EnableExceptionLevel = 0b10,
            EnableErrorLevel = 0b100,
            User64Mode = 0b100000,
            Supervisor64Mode = 0b1000000,
            Kernel64Mode = 0b10000000,
            InterruptMask7 = 0b100000000,
            InterruptMask6 = 0b1000000000,
            InterruptMask5 = 0b10000000000,
            InterruptMask4 = 0b100000000000,
            InterruptMask3 = 0b1000000000000,
            InterruptMask2 = 0b10000000000000,
            InterruptMask1 = 0b100000000000000,
            InterruptMask0 = 0b1000000000000000,
            DisableECCExceptions = 0b10000000000000000,
            CacheCheckBits = 0b100000000000000000,
            LastCacheHit = 0b1000000000000000000,
            ResetSignal = 0b10000000000000000000,
            IsTLBShutdown = 0b100000000000000000000,
            UseBootstrapVectors = 0b1000000000000000000000,
            ReverseEndian = 0b10000000000000000000000,
            AdditonalFPU = 0b100000000000000000000000,
            ReducePower = 0b1000000000000000000000000,
            UsableCop3 = 0b10000000000000000000000000,
            UsableCop2 = 0b100000000000000000000000000,
            UsableCop1 = 0b1000000000000000000000000000,
            UsableCop0 = 0b10000000000000000000000000000
        }

        public StatusRegister()
        {
            m_Fiddler.DefineField(3, 2);
            m_Fiddler.DefineField(8, 8); // Interrupt Masks
            m_Fiddler.DefineField(16, 9);
            m_Fiddler.DefineField(28, 4);
            m_Fiddler.DefineField(1, 1); // EXL
            m_Fiddler.DefineField(2, 1); // ERL

            for (int i = 0; i < 8; i++)
            {
                m_IntFiddler.DefineField(i, 1);
            }
        }

        public void Initialize()
        {
            /* Hardware initial value */
            SetFlags(StatusFlags.UsableCop0 | 
                     StatusFlags.UsableCop1 | 
                     StatusFlags.InterruptsEnabled);
        }

        public void Write(uint value)
        {
            DebugChanges(value, m_Value);
            m_Value = value;
        }

        public uint Read()
        {
            return m_Value;
        }

        public bool TestFlags(StatusFlags flags)
        {
            uint _flags = (uint)flags;
            return (m_Value & _flags) == _flags;
        }

        [Conditional("DEBUG")]
        private void DebugChanges(uint newValue, uint currentValue)
        {
            if (TestChange(StatusFlags.UseBootstrapVectors, currentValue, newValue))
            {
                Log.Debug("CPU code changed boot vector bit to: {0}", TestFlags(StatusFlags.UseBootstrapVectors));
            }
        }

        private bool TestChange(StatusFlags testFlags, uint oldVal, uint newVal) 
        {
            uint flags = (uint)testFlags;
            return (oldVal & flags) != (newVal & flags);
        }

        public void SetOrClearFlags(bool value, StatusFlags flags)
        {
            if (value)
            {
                SetFlags(flags);
            }
            else
            {
                ClearFlags(flags);
            }
        }

        public void SetFlags(StatusFlags flags)
        {
            m_Value |= (uint)flags;
        }

        public void ClearFlags(StatusFlags flags)
        {
            m_Value &= ~(uint)flags;
        }

        public void ToggleFlags(StatusFlags flags)
        {
            m_Value ^= (uint)flags;
        }

        public void SetInterruptsEnabled(bool value)
        {
            SetOrClearFlags(value, StatusFlags.InterruptsEnabled);
        }

        public bool IsAddress64 => TestFlags(StatusFlags.Kernel64Mode) || TestFlags(StatusFlags.User64Mode) || TestFlags(StatusFlags.Supervisor64Mode);

        public bool IsOperation64 => ModeBits == 0 || TestFlags(StatusFlags.User64Mode) || TestFlags(StatusFlags.Supervisor64Mode);

        public bool InterruptsEnabled => TestFlags(StatusFlags.InterruptsEnabled) && !ExceptionLevel && !ErrorLevel; 

        public void DebugSet_Address64(bool mode)
        {
            switch (ModeBits)
            {
                case 0: SetOrClearFlags(mode, StatusFlags.Kernel64Mode); break;
                case 1: SetOrClearFlags(mode, StatusFlags.Supervisor64Mode); break;
                case 2: SetOrClearFlags(mode, StatusFlags.User64Mode); break;
                default: break;
            }
        }

        public void DebugSet_Operation64(bool mode)
        {
            switch (ModeBits)
            {
                case 0:
                    {
                        if (!mode)
                        {
                            ModeBits = 1;
                        }

                        break;
                    }
                case 1: SetOrClearFlags(mode, StatusFlags.Supervisor64Mode); break;
                case 2: SetOrClearFlags(mode, StatusFlags.User64Mode); break;
                default: break;
            }
        }

        public bool FRMode => TestFlags(StatusFlags.AdditonalFPU);

        public int ModeBits
        {
            get => (int)m_Fiddler.X(F_KSU, ref m_Value);
            set => m_Fiddler.J(F_KSU, ref m_Value, (uint)value);
        }

        public bool ExceptionLevel
        {
            get => !(m_Fiddler.X(F_EXL, ref m_Value) == 0);
            set => m_Fiddler.J(F_EXL, ref m_Value, value ? 1U : 0);
        }

        public bool ErrorLevel
        {
            get => !(m_Fiddler.X(F_ERL, ref m_Value) == 0);
            set => m_Fiddler.J(F_ERL, ref m_Value, value ? 1U : 0);
        }

        public void SetInterruptMask(int index)
        {
            uint val = m_Fiddler.X(F_IM, ref m_Value);
            m_IntFiddler.J(index, ref val, 1);
            m_Fiddler.J(F_IM, ref m_Value, val);
        }

        public void ClearInterruptMask(int index)
        {
            uint val = m_Fiddler.X(F_IM, ref m_Value);
            m_IntFiddler.J(index, ref val, 0);
            m_Fiddler.J(F_IM, ref m_Value, val);
        }

        public uint InterruptMask => m_Fiddler.X(F_IM, ref m_Value);

        public bool CheckInterruptMask(int index)
        {
            uint val = m_Fiddler.X(F_IM, ref m_Value);
            return m_IntFiddler.X(index, ref val) != 0;
        }
    }
}
