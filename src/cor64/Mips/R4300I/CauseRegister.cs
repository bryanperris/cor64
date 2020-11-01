using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.Mips.R4300I
{
    public class CauseRegister
    {
        private readonly static Logger Log = LogManager.GetCurrentClassLogger();
        private uint m_Value;
        private bool m_ExceptionThrown;
        private ExceptionType m_Exception;
        private BitFiddler m_Fiddler = new BitFiddler();
        private BitFiddler m_IntFiddler = new BitFiddler();

        const int F_EXCEPTCODE = 0;
        const int F_IP = 1;
        const int F_CE = 2;
        const int F_BD = 3;

        const int INT_0 = 0;
        const int INT_1 = 1;
        const int INT_2 = 2;
        const int INT_3 = 3;
        const int INT_4 = 4;
        const int INT_5 = 5;
        const int INT_6 = 6;
        const int INT_7 = 7;

        public CauseRegister()
        {
            m_Fiddler.DefineField(02, 5);
            m_Fiddler.DefineField(08, 8);
            m_Fiddler.DefineField(28, 2);
            m_Fiddler.DefineField(31, 1);

            for (int i = 0; i < 8; i++)
            {
                m_IntFiddler.DefineField(i, 1);
            }
        }

        public uint Value
        {
            get => m_Value;
            set => m_Value = value;
        }

        public void SetException(ExceptionType exceptionType)
        {
            m_Exception = exceptionType;
            m_ExceptionThrown = true;

            byte code = 0;

            #if DEBUG_INTERRUPTS
                Log.Debug("Set CPU Exception Code: {0}", exceptionType.ToString());
            #endif

            switch (exceptionType)
            {
                default: throw new InvalidOperationException("invalid exception type");
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
            }

            m_Fiddler.J(F_EXCEPTCODE, ref m_Value, code);
        }

        public void SetBranchDelayBit()
        {
            m_Fiddler.J(F_BD, ref m_Value, 1);
        }

        public void ClearBranchDelayBit()
        {
            m_Fiddler.J(F_BD, ref m_Value, 0);
        }

        public ExceptionType Exception => m_Exception;

        public bool ExceptionThrown => m_ExceptionThrown;

        internal void ClearThrownException()
        {
            m_Exception = ExceptionType.Undefined;
            m_ExceptionThrown = false;
        }

        public void SetInterruptPending(int interrupt)
        {
            uint val = m_Fiddler.X(F_IP, ref m_Value);
            m_IntFiddler.J(interrupt, ref val, 1);
            m_Fiddler.J(F_IP, ref m_Value, val);

            #if DEBUG_INTERRUPTS_PENDING
                Log.Debug("CPU Interrupt Pending Updated: {0:X2}", InterruptPending);
            #endif
        }

        public void ClearPendingInterrupt(int interrupt)
        {
            uint val = m_Fiddler.X(F_IP, ref m_Value);
            m_IntFiddler.J(interrupt, ref val, 0);
            m_Fiddler.J(F_IP, ref m_Value, val);

            #if DEBUG_INTERRUPTS_PENDING
                Log.Debug("CPU Interrupt Pending Updated: {0:X2}", InterruptPending);
            #endif
        }

        public bool ReadInterruptPending(int interrupt)
        {
            uint val = m_Fiddler.X(F_IP, ref m_Value);
            return m_IntFiddler.X(interrupt, ref val) != 0;
        }

        public void ClearAllPending() {
            m_Value &= ~0xFF00U;
        }

        public uint InterruptPending => m_Fiddler.X(F_IP, ref m_Value);
    }
}
