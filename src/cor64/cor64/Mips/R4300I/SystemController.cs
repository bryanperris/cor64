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
        private bool m_ResetException;
        private MipsInterface m_Interface;
        private Clock m_Clock;

        /* Callbacks */
        private Func<ulong> m_CallbackReadPC;
        private Func<bool> m_CallbackDelaySlot;

        protected CauseRegister CR => m_State.Cp0.Cause;
        protected StatusRegister SR => m_State.Cp0.Status;
        protected ControlRegisters REGS => m_State.Cp0;

        public ulong ExceptionHandlerAddress { get; private set; }

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
        public void ProcessorTick(ulong pc)
        {
            /* Increment count register */
            // TODO: Allow some clock emulator to suggest the increment amount instead
            MipsTimerTick(1);

            /* Process all system events */
            CheckInterrupts(pc);
        }

        private void MipsTimerTick(ulong count)
        {
            REGS.Write(CTS.CP0_REG_COUNT, REGS.Read(CTS.CP0_REG_COUNT) + count);
        }

        private void CheckInterrupts(ulong pc)
        {
            if (!SR.InterruptsEnabled)
                return;
               
            /* TODO: Process TLB exception related stuff */

            /* TODO: The code related to setting the exception code needs to handle priority */

            /* TODO: Coprocessor unuable exceptions */

            /* For exceptions that are not Reset or Cache related */

            ulong target = 0;
            ulong cacheAddress = 0;
            ulong othersAddress = 0;
            bool executeHandler = false;
            int sourceInterrupt = -1;

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

            /* NMI MIPS Interrupt */
            if (m_ResetException)
            {
                target = 0xFFFFFFFFBFC00000;
                m_ResetException = false;
            }

            /* 32-bit TLB Exception */
            if (m_TLBRefillException)
            {
                target = othersAddress;
                m_TLBRefillException = false;
            }

            /* 64-bit TLB Exception */
            else if (m_XTLBRefillExcepetion)
            {
                target = othersAddress + 0x080;
                m_XTLBRefillExcepetion = false;
            }

            /* CPU Cache Exception */
            else if (m_CacheErr)
            {
                target = cacheAddress + 0x100;
                m_CacheErr = false;
            }

            /* CPU Instruction Exception */
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

                executeHandler = true;
            }

            /* Interrupts */
            else
            {
                target = othersAddress + 0x180;

                for (int i = 0; i < 8; i++)
                {
                    if (SR.CheckInterruptMask(i))
                    {
                        /* Software interrupt */
                        if (i == 0 || i == 1)
                        {
                            if (CR.ReadInterruptPending(i))
                            {
                                CR.ClearInterrupt(i);
                                executeHandler = true;
                                sourceInterrupt = i;
                                break;
                            }

                            continue;
                        }

                        /* RCP Interrupt */
                        if (i == 2)
                        {
                            if ((m_Interface.Interrupt & m_Interface.Mask) != 0)
                            {
                                CR.SetInterruptPending(i);
                                sourceInterrupt = i;
                                executeHandler = true;
                                break;
                            }
                            else
                            {
                                CR.ClearInterrupt(i);
                            }

                            continue;
                        }

                        /* Timer Event */
                        if (i == 7)
                        {
                            if (REGS.RegRead(CTS.CP0_REG_COMPARE) == REGS.RegRead(CTS.CP0_REG_COUNT))
                            {
                                CR.SetInterruptPending(i);
                                executeHandler = true;
                                sourceInterrupt = i;
                                break;
                            }
                            else
                            {
                                CR.ClearInterrupt(i);
                            }

                            continue;
                        }

                        /* Unimplemented others: (3) Cartridge, (4) Reset button, (5) RDB Read, (6) RDB Write */
                    }
                }
            }

            /* If true, then prepare to jump to the dedicated exception handler */
            if (executeHandler)
            {
                SR.SetInterruptsEnabled(false);
                REGS.RegWrite(CTS.CP0_REG_EPC, pc);
                REGS.RegWrite(CTS.CP0_REG_ERROR_EPC, 0);
                ExceptionHandlerAddress = (uint)target;
                SR.ExceptionLevel = true;

                //if (sourceInterrupt >= 0)
                //{
                //    Log.Debug("!Interrupt - " + ABI.GetInterruptType(sourceInterrupt));
                //    sourceInterrupt = -1;
                //}
            }
        }

        private void ClearMiInterrupt(int i)
        {
            m_Interface.SetInterrupt(i, false);
        }

        public bool IsKernelMode => SR.ModeBits == 0;

        public bool IsSupervisorMode => SR.ModeBits == 1;

        public bool IsUserMode => SR.ModeBits == 2;
    }
}
