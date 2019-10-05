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
        private ExecutionState m_State;
        private bool m_TLBRefillException;
        private bool m_XTLBRefillExcepetion;
        private bool m_CacheErr;
        private bool m_NMI;
        private MipsInterface m_Interface;
        private Clock m_Clock;
        private bool m_SoftReset;


        const int INTERRUPT_SW0 = 0;
        const int INTERRUPT_SW1 = 1;
        const int INTERRUPT_RCP = 2;
        const int INTERRUPT_CART = 3;
        const int INTERRUPT_RESET = 4;
        const int INTERRUPT_RDB_READ = 5;
        const int INTERRUPT_RDB_WRITE = 6;
        const int INTERRUPT_TIMER = 7;


        protected CauseRegister CR => m_State.Cp0.Cause;
        protected StatusRegister SR => m_State.Cp0.Status;
        protected ControlRegisters REGS => m_State.Cp0;

        public ulong ExceptionHandlerAddress { get; private set; }

        public SystemController(ExecutionState coreState, Clock clock)
        {
            m_State = coreState;
            m_Clock = clock;
        }

        public void AttachInterface(MipsInterface mipsInterface)
        {
            m_Interface = mipsInterface;
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

        public void SetSoftReset()
        {
            m_SoftReset = true;
        }

        public void SetNonMaskableInterrupt()
        {
            m_NMI = true;
        }

        /// <summary>
        /// Called to simulate a processor tick for coprocessor 0
        /// </summary>
        public void ProcessorTick(ulong pc, bool isDelaySlot)
        {
            /* Increment count register */
            // TODO: Allow some clock emulator to suggest the increment amount instead
            MipsTimerTick(1);

            /* Process all system events */
            CheckInterrupts(pc, isDelaySlot);
        }

        private void MipsTimerTick(ulong count)
        {
            REGS.Write(CTS.CP0_REG_COUNT, REGS.Read(CTS.CP0_REG_COUNT) + count);
        }

        private void CheckInterrupts(ulong pc, bool isDelaySlot)
        {
            if (!SR.InterruptsEnabled)
                return;

            uint target;
            uint cacheTarget;
            bool executeHandler;
            bool error = false;

            if (SR.TestFlags(StatusRegister.StatusFlags.UseBootstrapVectors))
            {
                cacheTarget = 0xBFC00200;
                target = 0xBFC00200;
            }
            else
            {
                cacheTarget = 0xA0000000;
                target = 0x80000000;
            }

            /* NMI MIPS Interrupt */
            if (m_NMI)
            {
                target = 0xBFC00000;
                m_NMI = false;
                error = true;
            }

            /* 32-bit TLB Exception */
            if (m_TLBRefillException)
            {
                m_TLBRefillException = false;
                executeHandler = true;
            }

            /* 64-bit TLB Exception */
            else if (m_XTLBRefillExcepetion)
            {
                target += 0x080;
                m_XTLBRefillExcepetion = false;
                executeHandler = true;
            }

            /* CPU Cache Exception */
            else if (m_CacheErr)
            {
                target += 0x100;
                m_CacheErr = false;
                error = true;
                executeHandler = true;
            }

            /* CPU Instruction Exception */
            else if (CR.ExceptionThrown)
            {
                CR.ClearThrownException();

                target += 0x180;

                if (isDelaySlot)
                {
                    REGS.Write(CTS.CP0_REG_EPC, pc - 4);
                    CR.SetBranchDelayBit();
                }
                else
                {
                    REGS.Write(CTS.CP0_REG_EPC, pc);
                    CR.ClearBranchDelayBit();
                }

                executeHandler = true;
            }

            /* Interrupts */
            else
            {
                target += 0x180;
                
                if (SR.InterruptMask != 0)
                {
                    /* RCP Hardware Interrupt */
                    if ((m_Interface.Interrupt & m_Interface.Mask) != 0)
                    {
                        CR.SetInterruptPending(INTERRUPT_RCP);
                    }

                    /* Timer Interrupt */
                    if (REGS.RegRead(CTS.CP0_REG_COMPARE) == REGS.RegRead(CTS.CP0_REG_COUNT))
                    {
                        CR.SetInterruptPending(INTERRUPT_TIMER);
                    }

                    /* SoftReset */
                    if (m_SoftReset)
                    {
                        m_SoftReset = false;
                        CR.SetInterruptPending(INTERRUPT_RESET);
                        error = true;
                    }
                }

                executeHandler = (CR.InterruptPending & SR.InterruptMask) != 0;
            }

            /* If true, then prepare to jump to the dedicated exception handler */
            if (executeHandler)
            {
                SR.SetInterruptsEnabled(false);

                if (!error)
                {
                    REGS.RegWrite(CTS.CP0_REG_EPC, pc);
                    SR.ExceptionLevel = true;
                }
                else
                {
                    REGS.RegWrite(CTS.CP0_REG_ERROR_EPC, pc);
                    SR.ErrorLevel = true;
                }

                ExceptionHandlerAddress = target;
            }
        }

        public bool IsKernelMode => SR.ModeBits == 0;

        public bool IsSupervisorMode => SR.ModeBits == 1;

        public bool IsUserMode => SR.ModeBits == 2;
    }
}
