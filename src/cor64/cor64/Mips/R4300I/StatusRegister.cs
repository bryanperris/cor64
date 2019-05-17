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

        const int F_KSU = 0;
        const int F_IM = 1;
        const int F_DS = 2;
        const int F_CU = 3;

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

        [Flags]
        public enum InterruptMask : byte
        {
            None = 0,
            Int0 = 0b1,
            Int1 = 0b10,
            Int2 = 0b100,
            Int3 = 0b1000,
            Int4 = 0b10000,
            Int5 = 0b100000,
            Int6 = 0b1000000,
            Int7 = 0b01000000,
            All =  0b11111111
        }

        public StatusRegister()
        {
            m_Fiddler.DefineField(04, 2);
            m_Fiddler.DefineField(15, 8);
            m_Fiddler.DefineField(24, 9);
            m_Fiddler.DefineField(31, 4);
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
            const uint BEVFlag = (uint)StatusFlags.UseBootstrapVectors;

            if ( (currentValue & BEVFlag) != (newValue & BEVFlag) )
            {
                Log.Debug("CPU code changed boot vector bit to: {0}", (newValue & BEVFlag) == BEVFlag);
            }
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

        public bool InterruptsEnabled => TestFlags(StatusFlags.InterruptsEnabled); 

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

        public bool ExceptionsEnabled => TestFlags(StatusFlags.EnableExceptionLevel);

        public bool FRMode => TestFlags(StatusFlags.AdditonalFPU);

        public int ModeBits
        {
            get => (int)m_Fiddler.X(F_KSU, ref m_Value);
            set => m_Fiddler.J(F_KSU, ref m_Value, (uint)value);
        }

        public void SetInterruptMask(InterruptMask interruptMask)
        {
            uint val = m_Fiddler.X(F_IM, ref m_Value);
            val |= (byte)interruptMask;
            m_Fiddler.J(F_IM, ref m_Value, val);
        }

        public void ClearInterruptMask(InterruptMask interruptMask)
        {
            uint val = m_Fiddler.X(F_IM, ref m_Value);
            val &= ~(uint)interruptMask;
            m_Fiddler.J(F_IM, ref m_Value, val);
        }

        public bool TestInterruptMask(InterruptMask interruptMask)
        {
            uint val = m_Fiddler.X(F_IM, ref m_Value);
            return (val & (uint)interruptMask) == (uint)interruptMask;
        }
    }
}
