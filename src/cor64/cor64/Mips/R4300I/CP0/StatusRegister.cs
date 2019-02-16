using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;
using System.Diagnostics;

namespace cor64.Mips.R4300I.CP0
{
    public class StatusRegister : IWatchableRegister<Object>
    {
        private readonly static Logger Log = LogManager.GetCurrentClassLogger();
        private uint m_Value;

        /* Cached bits */
        private bool m_User64;
        private bool m_Supervisor64;
        private bool m_Kernel64;
        private bool m_IntMask0;
        private bool m_IntMask1;
        private bool m_IntMask2;
        private bool m_IntMask3;
        private bool m_IntMask4;
        private bool m_IntMask5;
        private bool m_IntMask6;
        private bool m_IntMask7;
        private bool m_InterruptsEnabled;
        private bool m_EXL;
        private bool m_FR;

        public event ValueChangedHandler<Object> ValueChanged;

        public byte ModeBits { get; private set; }

        public StatusRegister()
        {
            
        }

        public void Initialize()
        {
            /* Hardware initial value */
            SetFlags(StatusFlags.UsableCop0 | 
                     StatusFlags.UsableCop1 | 
                     StatusFlags.InterruptsEnabled);
        }

        /// <summary>
        /// Not related to the CPU cache, but we cache some important flags for quick lookup
        /// </summary>
        private void UpdateQuickCache()
        {
            m_User64 = (ModeBits == 2 && TestFlags(StatusFlags.User64Mode));
            m_Supervisor64 = (ModeBits == 1 && TestFlags(StatusFlags.Supervisor64Mode));
            m_Kernel64 = (ModeBits == 0 && TestFlags(StatusFlags.Kernel64Mode));
            m_IntMask0 = TestFlags(StatusFlags.InterruptMask0);
            m_IntMask1 = TestFlags(StatusFlags.InterruptMask1);
            m_IntMask2 = TestFlags(StatusFlags.InterruptMask2);
            m_IntMask3 = TestFlags(StatusFlags.InterruptMask3);
            m_IntMask4 = TestFlags(StatusFlags.InterruptMask4);
            m_IntMask5 = TestFlags(StatusFlags.InterruptMask5);
            m_IntMask6 = TestFlags(StatusFlags.InterruptMask6);
            m_IntMask7 = TestFlags(StatusFlags.InterruptMask7);
            m_FR = TestFlags(StatusFlags.AdditonalFPU);
            m_InterruptsEnabled = TestFlags(StatusFlags.InterruptsEnabled);
            m_EXL = TestFlags(StatusFlags.EnableExceptionLevel);
        }

        public void Write(uint value)
        {
            DebugChanges(value, m_Value);
            m_Value = value;
            ModeBits = (byte)((value >> 3) & 0x3);
            UpdateQuickCache();
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
            OnValueChanged();
            UpdateQuickCache();
        }

        public void ClearFlags(StatusFlags flags)
        {
            m_Value &= ~(uint)flags;
            OnValueChanged();
            UpdateQuickCache();
        }

        public void ToggleFlags(StatusFlags flags)
        {
            m_Value ^= (uint)flags;
            OnValueChanged();
            UpdateQuickCache();
        }

        private void SetKSU(byte ksu)
        {
            ModeBits = (byte)(ksu & 0x3);
            m_Value &= ~0b1100U;
            m_Value |= (uint)(ModeBits << 3);
            OnValueChanged();
            UpdateQuickCache();
        }

        public void SetInterruptsEnabled(bool value)
        {
            SetOrClearFlags(value, StatusFlags.InterruptsEnabled);
        }

        public bool IsAddress64
        {
            get {
                return m_Kernel64 || m_User64 || m_Supervisor64;
            }
        }

        public bool IsOperation64
        {
            get {
                return ModeBits == 0 || m_User64 || m_Supervisor64;
            }
        }

        public bool InterruptsEnabled => m_InterruptsEnabled; 

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
                            SetKSU(1);
                        }

                        break;
                    }
                case 1: SetOrClearFlags(mode, StatusFlags.Supervisor64Mode); break;
                case 2: SetOrClearFlags(mode, StatusFlags.User64Mode); break;
                default: break;
            }
        }

        public bool InterruptMask0 => m_IntMask0;

        public bool InterruptMask1 => m_IntMask1;

        public bool InterruptMask2 => m_IntMask2;

        public bool InterruptMask3 => m_IntMask3;

        public bool InterruptMask4 => m_IntMask4;

        public bool InterruptMask5 => m_IntMask5;

        public bool InterruptMask6 => m_IntMask6;

        public bool InterruptMask7 => m_IntMask7;

        public bool ExceptionsEnabled => m_EXL;

        public bool FRMode => m_FR;

        public void SetInterruptMask(int i)
        {
            switch (i)
            {
                case 0: SetFlags(StatusFlags.InterruptMask0); break;
                case 1: SetFlags(StatusFlags.InterruptMask1); break;
                case 2: SetFlags(StatusFlags.InterruptMask2); break;
                case 3: SetFlags(StatusFlags.InterruptMask3); break;
                case 4: SetFlags(StatusFlags.InterruptMask4); break;
                case 5: SetFlags(StatusFlags.InterruptMask5); break;
                case 6: SetFlags(StatusFlags.InterruptMask6); break;
                case 7: SetFlags(StatusFlags.InterruptMask7); break;
                default: break;
            }
        }

        public void ClearInterruptMask(int i)
        {
            switch (i)
            {
                case 0: ClearFlags(StatusFlags.InterruptMask0); break;
                case 1: ClearFlags(StatusFlags.InterruptMask1); break;
                case 2: ClearFlags(StatusFlags.InterruptMask2); break;
                case 3: ClearFlags(StatusFlags.InterruptMask3); break;
                case 4: ClearFlags(StatusFlags.InterruptMask4); break;
                case 5: ClearFlags(StatusFlags.InterruptMask5); break;
                case 6: ClearFlags(StatusFlags.InterruptMask6); break;
                case 7: ClearFlags(StatusFlags.InterruptMask7); break;
                default: break;
            }
        }

        protected virtual void OnValueChanged()
        {
            ValueChanged?.Invoke(null);
        }
    }
}
