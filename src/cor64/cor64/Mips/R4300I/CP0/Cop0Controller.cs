using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace cor64.Mips.R4300I.CP0
{
    /// <summary>
    /// This class simulates functions of the coprocessor 0 in high level emulation
    /// </summary>
    public class Cop0Controller
    {
        private readonly static Logger Log = LogManager.GetCurrentClassLogger();
        private bool m_Debug = true;
        private Cop0RegsterSet m_Registers;
        private StatusRegister m_StatusReg;
        private CauseRegister m_CauseReg;
        private bool m_TLBRefillException;
        private bool m_XTLBRefillExcepetion;
        private bool m_CacheErr;
        private bool m_ResetException;
        private ChipInterface m_Interface;
        private Clock m_Clock;
        private bool[] m_InterruptReg = new bool[6];
        private bool m_IP7Multiplexer = false;
        private bool m_Timer;

        public event Action<ulong> ExceptionJump;

        /* Callbacks */
        private Func<ulong> m_CallbackReadPC;
        private Func<bool> m_CallbackDelaySlot;

        public Cop0Controller()
        {
            m_Registers = new Cop0RegsterSet();

            m_StatusReg = new StatusRegister();
            m_CauseReg = new CauseRegister();

            m_Registers.ValueChanged += OnRegUpdate;
            m_StatusReg.ValueChanged += StatusValueChanged;
            m_CauseReg.ValueChanged += CauseValueChanged;
        }

        private void CauseValueChanged(object data)
        {
            m_Registers.Write(CTS.CP0_REG_CAUSE, m_CauseReg.Read());
        }

        public void Attach_State(ChipInterface iface, Clock clock)
        {
            m_Interface = iface;
            m_Clock = clock;
        }

        public void Attach_Callbacks(Func<ulong> cbReadPC, Func<bool> cbDelaySlot)
        {
            m_CallbackReadPC = cbReadPC;
            m_CallbackDelaySlot = cbDelaySlot;
        }

        public void SetTLBRefillException()
        {
            m_TLBRefillException = true;
        }

        public void SetXTLBRefillException()
        {
            m_TLBRefillException = true;
        }

        public void SetCacheError()
        {
            m_CacheErr = true;
        }

        public void Initialize()
        {
            m_StatusReg.Initialize();
        }

        private void StatusValueChanged(object o)
        {
            m_Registers.Write(CTS.CP0_REG_SR, m_StatusReg.Read());
        }

        private void OnRegUpdate(int i)
        {
            switch (i)
            {
                case CTS.CP0_REG_COMPARE:
                    {
                        if (m_IP7Multiplexer)
                            ClearInterrupt(7);
                        break;
                    }
                case CTS.CP0_REG_SR: m_StatusReg.Write((uint)m_Registers.Read(CTS.CP0_REG_SR)); break;
                case CTS.CP0_REG_CAUSE: m_CauseReg.Write((uint)m_Registers.Read(CTS.CP0_REG_CAUSE)); break;
                default: break;
            }
        }

        /// <summary>
        /// Called to simulate a processor tick for coprocessor 0
        /// </summary>
        public void ProcessorTick()
        {
            /* Increment count register */
            IncrementCount();

            /* Process all system events */
            CheckInterrupts();

            /* Reset timer */
            if (m_Timer)
            {
                m_Timer = false;
                m_Registers.Write(CTS.CP0_REG_COUNT, 0);
                m_Clock.ClearCountClock();
            }
        }

        private void IncrementCount()
        {
            /* TODO: Instead of adding 1, it should be a value based on Clock, so we keep it in sync */
            /* TODO: When timer fires, somehow it has to turn off and reset, so I think at the end of a whole cycle
             *       we need to check that and reset it
             */
            if (m_Clock.CountClock)
            {
                m_Registers.Write(CTS.CP0_REG_COUNT, m_Registers.Read(CTS.CP0_REG_COUNT) + 1);

                /* Fire the timer interrupt */
                if (m_Registers.Read(CTS.CP0_REG_COUNT) >= m_Registers.Read(CTS.CP0_REG_COMPARE))
                {
                    m_Timer = true;
                }
            }
        }

        private bool ReadInterruptMask(int i)
        {
            switch (i)
            {
                case 0: return SR.InterruptMask0;
                case 1: return SR.InterruptMask1;
                case 2: return SR.InterruptMask2;
                case 3: return SR.InterruptMask3;
                case 4: return SR.InterruptMask4;
                case 5: return SR.InterruptMask5;
                case 6: return SR.InterruptMask6;
                case 7: return SR.InterruptMask7;
                default: return false;
            }
        }

        private bool ReadInterrupt(int i)
        {
            switch (i)
            {
                case 0: return Cause.Interrupt0;
                case 1: return Cause.Interrupt1;
                case 2: return Cause.Interrupt2;
                case 3: return Cause.Interrupt3;
                case 4: return Cause.Interrupt4;
                case 5: return Cause.Interrupt5;
                case 6: return Cause.Interrupt6;
                case 7: return Cause.Interrupt7;
                default: return false;
            }
        }

        private void CheckExceptions()
        {
        }

        /*********************************************
         * Master function to check all processor events 
         * **********************************************
         */
        private void CheckInterrupts()
        {
            /* TODO: Check watch stuff unless EXL = 1 ? */

            /* TODO: Process TLB exception related stuff */

            /* TODO: The code related to setting the exception code needs to handle priority */

            /* TODO: Coprocessor unuable exceptions */

            /* For exceptions that are not Reset or Cache related */

            ulong target = 0;
            ulong cacheAddress = 0;
            ulong othersAddress = 0;
            bool serviceHandler = true;

            if (m_StatusReg.TestFlags(StatusFlags.UseBootstrapVectors))
            {
                cacheAddress = 0xFFFFFFFFBFC00200;
                othersAddress = 0xFFFFFFFFBFC00200;
            }
            else
            {
                cacheAddress = 0xFFFFFFFFA0000000;
                othersAddress = 0xFFFFFFFF80000000;
            }

            if (m_ResetException)
            {
                target = 0xFFFFFFFFBFC00000;
                m_ResetException = false;
            }
            if (m_TLBRefillException)
            {
                target = othersAddress;
                m_TLBRefillException = false;
            }
            else if (m_XTLBRefillExcepetion)
            {
                target = othersAddress + 0x080;
                m_XTLBRefillExcepetion = false;
            }
            else if (m_CacheErr)
            {
                target = cacheAddress + 0x100;
                m_CacheErr = false;
            }
            else if (m_CauseReg.ExceptionThrown)
            {
                m_CauseReg.ClearThrownException();

                target = othersAddress + 0x180;

                if (!m_StatusReg.ExceptionsEnabled)
                {
                    if (m_CallbackDelaySlot())
                    {
                        m_Registers.WriteFromInternal(CTS.CP0_REG_EPC, m_CallbackReadPC() - 4);
                        m_CauseReg.SetFlags(CauseFlags.IsBranchDelaySlot);
                    }
                    else
                    {
                        m_Registers.WriteFromInternal(CTS.CP0_REG_EPC, m_CallbackReadPC());
                        m_CauseReg.ClearFlags(CauseFlags.IsBranchDelaySlot);
                    }
                }

                m_StatusReg.SetFlags(StatusFlags.EnableExceptionLevel);
                
            }
            else
            {
                target = othersAddress + 0x180;
                serviceHandler = false;

                if (m_StatusReg.InterruptsEnabled)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        /* Software interrupts */
                        if (i < 3)
                        {
                            if (CheckSWInterrupt(i))
                            {
                                serviceHandler = true;
                                break;
                            }
                        }
                        else
                        {
                            if (CheckHWInterrupt(i))
                            {
                                serviceHandler = true;
                                break;
                            }
                        }
                    }
                }
            }

            if (serviceHandler)
            {
                if (m_Debug)
                {
                    Log.Debug("Servicing interrupt: {0:X8}", target);
                }

                /* Disable all other interrupts */
                m_StatusReg.SetInterruptsEnabled(false);

                /* Set where PC is going to be next */
                ExceptionJump(target);
            }
        }

        private void FireInterrupt(int i)
        {

        }

        private void ClearInterrupt(int i)
        {

        }

        public StatusRegister SR => m_StatusReg;

        public CauseRegister Cause => m_CauseReg;

        public Cop0RegsterSet Registers => m_Registers;

        private bool ReadInterruptPin(int i)
        {
            switch (i)
            {
                case 0: return m_Interface.Int0;
                case 1: return m_Interface.Int1;
                case 2: return m_Interface.Int2;
                case 3: return m_Interface.Int3;
                case 4: return m_Interface.Int4;
                case 5: return m_Interface.Int5;
                default: return false;
            }
        }


        private bool CheckSWInterrupt(int i)
        {
            return !ReadInterruptMask(i) && ReadInterrupt(i);
        }

        private bool CheckHWInterrupt(int i)
        {
            if (i != 5)
            {
                return !ReadInterruptMask(i) && ( m_InterruptReg[i] || ReadInterruptPin(i));
            }
            else
            {
                if (!m_IP7Multiplexer) {
                    return !ReadInterruptMask(i) && ((m_Clock.SClock && ReadInterruptPin(i)) || m_InterruptReg[i]);
                }
                else
                {
                    return m_Timer;
                }
            }
        }

        public bool IsKernelMode => m_StatusReg.ModeBits == 0;

        public bool IsSupervisorMode => m_StatusReg.ModeBits == 1;

        public bool IsUserMode => m_StatusReg.ModeBits == 2;
    }
}
