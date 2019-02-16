using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.Mips.R4300I.CP0
{
    public class CauseRegister : IWatchableRegister<Object>
    {
        private readonly static Logger Log = LogManager.GetCurrentClassLogger();
        public event ValueChangedHandler<object> ValueChanged;
        private uint m_Value;
        private bool m_ExceptionThrown;

        /* Cached bits */
        private bool m_Int0;
        private bool m_Int1;
        private bool m_Int2;
        private bool m_Int3;
        private bool m_Int4;
        private bool m_Int5;
        private bool m_Int6;
        private bool m_Int7;
        private ExceptionType m_ExceptionState;

        public byte CopUnusableExceptionSrc { get; private set; }
        public byte ExceptionCode { get; set; }

        public CauseRegister()
        {

        }

        public void Initialize()
        {

        }

        private void UpdateQuickCache()
        {
            m_Int0 = TestFlags(CauseFlags.Interrupt0);
            m_Int1 = TestFlags(CauseFlags.Interrupt1);
            m_Int2 = TestFlags(CauseFlags.Interrupt2);
            m_Int3 = TestFlags(CauseFlags.Interrupt3);
            m_Int4 = TestFlags(CauseFlags.Interrupt4);
            m_Int5 = TestFlags(CauseFlags.Interrupt5);
            m_Int6 = TestFlags(CauseFlags.Interrupt6);
            m_Int7 = TestFlags(CauseFlags.Interrupt7);

            switch (ExceptionCode)
            {
                case 0: m_ExceptionState = ExceptionType.Interrupt; break;
                case 1: m_ExceptionState = ExceptionType.TLBMod; break;
                case 2: m_ExceptionState = ExceptionType.TLBLoad; break;
                case 3: m_ExceptionState = ExceptionType.TLBStore; break;
                case 4: m_ExceptionState = ExceptionType.AddressLoad; break;
                case 5: m_ExceptionState = ExceptionType.AddressStore; break;
                case 6: m_ExceptionState = ExceptionType.InstructionBusError; break;
                case 7: m_ExceptionState = ExceptionType.DataBusError; break;
                case 8: m_ExceptionState = ExceptionType.Syscall; break;
                case 9: m_ExceptionState = ExceptionType.Breakpoint; break;
                case 10: m_ExceptionState = ExceptionType.Reserved; break;
                case 11: m_ExceptionState = ExceptionType.Unusable; break;
                case 12: m_ExceptionState = ExceptionType.Overflow; break;
                case 13: m_ExceptionState = ExceptionType.Trap; break;
                case 14: m_ExceptionState = ExceptionType.VCEInstruction; break;
                case 15: m_ExceptionState = ExceptionType.Float; break;
                case 23: m_ExceptionState = ExceptionType.Watch; break;
                case 31: m_ExceptionState = ExceptionType.VCEData; break;
                default: Log.Error("Unknown exception code {0}", ExceptionCode); break;
            }
        }

        private void SetCopUnusableExceptionSrc(byte value)
        {
            CopUnusableExceptionSrc = (byte)(value & 0x3);
            m_Value &= ~(0x3U << 28);
            m_Value |= (uint)(CopUnusableExceptionSrc << 28);
            OnValueChanged();
            UpdateQuickCache();
        }

        private void SetExceptionCode(byte value)
        {
            ExceptionCode = (byte)(value & 0x1F);
            m_Value &= ~(0x1FU << 2);
            m_Value |= (uint)(ExceptionCode << 2);
            OnValueChanged();
            UpdateQuickCache();
        }

        public void CauseException(ExceptionType type)
        {
            m_ExceptionThrown = true;
            byte code = 0;

            switch (type)
            {
                case ExceptionType.Interrupt: break;
                case ExceptionType.TLBMod: code = 1; break;
                case ExceptionType.TLBLoad: code = 2; break;
                case ExceptionType.TLBStore: code = 3; break;
                case ExceptionType.AddressLoad: code = 4; break;
                case ExceptionType.AddressStore: code = 5; break;
                case ExceptionType.InstructionBusError: code = 6; break;
                case ExceptionType.DataBusError: code = 7; break;
                case ExceptionType.Syscall: code = 8; break;
                case ExceptionType.Breakpoint: code = 9; break;
                case ExceptionType.Reserved: code = 10; break;
                case ExceptionType.Unusable: code = 11; break;
                case ExceptionType.Overflow: code = 12; break;
                case ExceptionType.Trap: code = 13; break;
                case ExceptionType.VCEInstruction: code = 14; break;
                case ExceptionType.Float: code = 15; break;
                case ExceptionType.Watch: code = 23; break;
                case ExceptionType.VCEData: code = 31; break;
                default: throw new InvalidOperationException("Reserved exception type");
            }

            SetExceptionCode(code);
        }

        public ExceptionType ExceptionState => m_ExceptionState;

        public uint Read()
        {
            return m_Value;
        }

        public void Write(uint value)
        {
            m_Value = value;
            CopUnusableExceptionSrc = (byte)((value >> 28) & 0x3);
            ExceptionCode = (byte)((value >> 2) & 0x1F);
            UpdateQuickCache();
        }

        public bool TestFlags(CauseFlags flags)
        {
            uint _flags = (uint)flags;
            return (m_Value & _flags) == _flags;
        }

        public void SetOrClearFlags(bool value, CauseFlags flags)
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

        public void SetFlags(CauseFlags flags)
        {
            m_Value |= (uint)flags;
            OnValueChanged();
            UpdateQuickCache();
        }

        public void ClearFlags(CauseFlags flags)
        {
            m_Value &= ~(uint)flags;
            OnValueChanged();
            UpdateQuickCache();
        }

        public void ToggleFlags(CauseFlags flags)
        {
            m_Value ^= (uint)flags;
            OnValueChanged();
            UpdateQuickCache();
        }

        protected virtual void OnValueChanged()
        {
            ValueChanged?.Invoke(null);
        }

        public bool Interrupt0 => m_Int0;

        public bool Interrupt1 => m_Int1;

        public bool Interrupt2 => m_Int2;

        public bool Interrupt3 => m_Int3;

        public bool Interrupt4 => m_Int4;

        public bool Interrupt5 => m_Int5;

        public bool Interrupt6 => m_Int6;

        public bool Interrupt7 => m_Int7;

        public bool ExceptionThrown => m_ExceptionThrown;

        public void ClearThrownException()
        {
            m_ExceptionThrown = false;
        }

        public void SetInterrupt(int i)
        {
            switch (i)
            {
                case 0: SetFlags(CauseFlags.Interrupt0); break;
                case 1: SetFlags(CauseFlags.Interrupt1); break;
                case 2: SetFlags(CauseFlags.Interrupt2); break;
                case 3: SetFlags(CauseFlags.Interrupt3); break;
                case 4: SetFlags(CauseFlags.Interrupt4); break;
                case 5: SetFlags(CauseFlags.Interrupt5); break;
                case 6: SetFlags(CauseFlags.Interrupt6); break;
                case 7: SetFlags(CauseFlags.Interrupt7); break;
                default: break;
            }
        }

        public void ClearInterrupt(int i)
        {
            switch (i)
            {
                case 0: ClearFlags(CauseFlags.Interrupt0); break;
                case 1: ClearFlags(CauseFlags.Interrupt1); break;
                case 2: ClearFlags(CauseFlags.Interrupt2); break;
                case 3: ClearFlags(CauseFlags.Interrupt3); break;
                case 4: ClearFlags(CauseFlags.Interrupt4); break;
                case 5: ClearFlags(CauseFlags.Interrupt5); break;
                case 6: ClearFlags(CauseFlags.Interrupt6); break;
                case 7: ClearFlags(CauseFlags.Interrupt7); break;
                default: break;
            }
        }
    }
}
