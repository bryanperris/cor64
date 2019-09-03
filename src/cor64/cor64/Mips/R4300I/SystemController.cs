using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NLog;

namespace cor64.Mips.R4300I
{
    /// <summary>
    /// This class simulates functions of the coprocessor 0 in high level emulation
    /// </summary>
    public class SystemController
    {
        private readonly static Logger Log = LogManager.GetCurrentClassLogger();
        private bool m_Debug = true;
        private ExecutionState m_State;
        private bool m_TLBRefillException;
        private bool m_XTLBRefillExcepetion;
        private bool m_CacheErr;
        private bool m_ResetException;
        private MipsInterface m_Interface;
        private Clock m_Clock;
        private bool[] m_InterruptReg = new bool[8];
        private bool m_IP7Multiplexer = false;
        private bool m_Timer;

        public event Action<ulong> ExceptionJump;

        /* Callbacks */
        private Func<ulong> m_CallbackReadPC;
        private Func<bool> m_CallbackDelaySlot;

        protected CauseRegister CR => m_State.Cp0.Cause;
        protected StatusRegister SR => m_State.Cp0.Status;
        protected ControlRegisters REGS => m_State.Cp0;

        public SystemController(ExecutionState coreState)
        {
            m_State = coreState;
        }

        public void Attach_State(MipsInterface iface, Clock clock)
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
            SR.Initialize();
        }

        private void StatusValueChanged(object o)
        {
            REGS.Write(CTS.CP0_REG_SR, SR.Read());
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
                REGS.Write(CTS.CP0_REG_COUNT, 0);
                m_Clock.ClearCountClock();
            }
        }

        private void IncrementCount()
        {
            /* TODO: Instead of adding 1, it should be a value based on Clock, so we keep it in sync */
            /* TODO: When timer fires, somehow it has to turn off and reset, so I think at the end of a whole cycle
             *       we need to check that and reset it
             */
            if (m_Clock == null)
            {
                Log.Warn("Clock source is null, skipping increment");
                return;
            }

            if (m_Clock.CountClock)
            {
                REGS.Write(CTS.CP0_REG_COUNT, REGS.Read(CTS.CP0_REG_COUNT) + 1);

                /* Fire the timer interrupt */
                if (REGS.Read(CTS.CP0_REG_COUNT) >= REGS.Read(CTS.CP0_REG_COMPARE))
                {
                    m_Timer = true;
                }
            }
        }

        private bool ReadInterruptMask(int i)
        {
            if (i >= 0 && i < 8)
            {
                return SR.TestInterruptMask((StatusRegister.InterruptMask)i);
            }

            return false;
        }

        private bool ReadInterrupt(int i)
        {

            if (i >= 0 && i < 8)
            {
                return CR.TestInterrupt((CauseRegister.Interrupt)i);
            }

            return false;
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

            if (SR.TestFlags(StatusRegister.StatusFlags.UseBootstrapVectors))
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
            else if (CR.ExceptionThrown)
            {
                CR.ClearThrownException();

                target = othersAddress + 0x180;

                if (!(SR.ExceptionsEnabled))
                {
                    if (m_CallbackDelaySlot())
                    {
                        REGS.Write(CTS.CP0_REG_EPC, m_CallbackReadPC() - 4);
                        CR.SetBranchDelayBit();
                    }
                    else
                    {
                        REGS.Write(CTS.CP0_REG_EPC, m_CallbackReadPC());
                        CR.ClearBranchDelayBit();
                    }
                }

                SR.SetFlags(StatusRegister.StatusFlags.EnableExceptionLevel);
            }
            else
            {
                target = othersAddress + 0x180;
                serviceHandler = false;

                if (SR.InterruptsEnabled)
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
                SR.SetInterruptsEnabled(false);

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

        public bool IsKernelMode => SR.ModeBits == 0;

        public bool IsSupervisorMode => SR.ModeBits == 1;

        public bool IsUserMode => SR.ModeBits == 2;
    }
}
