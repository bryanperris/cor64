using System.Runtime.ConstrainedExecution;
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
        private readonly ExecutionState m_State;
        private bool m_TLBRefillException;
        private bool m_XTLBRefillExcepetion;
        private bool m_CacheErr;
        private bool m_NMI;
        private MipsInterface m_Interface;
        private bool m_SoftReset;
        private bool m_Direct;
        private bool m_TLBRaisedException = false;
        private bool m_Syscall = false;
        private bool m_Break = false;
        private bool m_Trap = false;

        const int INTERRUPT_SW0 = 0;  // An interrupt set by user
        const int INTERRUPT_SW1 = 1;  // An interrupt set by user
        const int INTERRUPT_RCP = 2;  // RCP Hardware Events
        const int INTERRUPT_CART = 3; // Cartidge Events
        const int INTERRUPT_RESET = 4; // Reset Button
        const int INTERRUPT_RDB_READ = 5; // Debugger Read
        const int INTERRUPT_RDB_WRITE = 6; // Debugger Write
        const int INTERRUPT_TIMER = 7; // MIPS Timer


        protected CauseRegister CR { get; }
        protected StatusRegister SR { get; }
        public ControlRegisters REGS { get; }

        private bool m_EnableTimer = false;

        public ulong ExceptionHandlerAddress { get; private set; }

        public SystemController(ControlRegisters cop0State, ExecutionState state)
        {
            m_State = state;
            REGS = cop0State;
            CR = cop0State.Cause;
            SR = cop0State.Status;
        }

        public void AttachInterface(MipsInterface mipsInterface)
        {
            m_Interface = mipsInterface;
        }

        public void SetTLBRefillException(bool isStore)
        {
            CR.SetException(isStore ? ExceptionType.TLBStore : ExceptionType.TLBLoad);
            // TODO: Set CE
            m_TLBRaisedException = true;
            m_TLBRefillException = true;
        }

        public void SetXTLBRefillException(bool isStore)
        {
            CR.SetException(isStore ? ExceptionType.TLBStore : ExceptionType.TLBLoad);
            // TODO: Set CE
            m_TLBRaisedException = true;
            m_TLBRefillException = true;
        }

        public void SetInvalidTLBException(bool isStore) {
            throw new NotImplementedException("TODO: Implement TLB invalid");
        }

        public void SetCacheError()
        {
            m_CacheErr = true;
        }

        public void SetSoftReset()
        {
            m_SoftReset = true;
        }

        public void UseDirectAddresses() {
            m_Direct = true;
        }

        public void SetNonMaskableInterrupt()
        {
            m_NMI = true;
        }

        public void MipsTimerTick(ulong count)
        {
            #if HACK_FASTER_TIMER
            count += 100;
            #endif

            uint countReg = (uint)REGS.Read(CTS.CP0_REG_COUNT) + (uint)count;

            REGS.Write(CTS.CP0_REG_COUNT, countReg);
        }

        public uint TimerCount => (uint)REGS.Read(CTS.CP0_REG_COUNT);

        public uint TimerMax => (uint)REGS.Read(CTS.CP0_REG_COMPARE);

        public ExecutionState State => m_State;

        public void CpuRegisterWrite(int i, ulong value) {
            // On timer compare write, clear the timer pending interrupt
            if (i == CTS.CP0_REG_COMPARE) {

                // if (value <= REGS.Read(CTS.CP0_REG_COUNT)) {
                //     Log.Debug("MIPS Why? Compare {0:X8} <= Count {1:X8}", value, REGS.Read(CTS.CP0_REG_COUNT));
                // }

                m_EnableTimer = value > REGS.Read(CTS.CP0_REG_COUNT);
                CR.ClearPendingInterrupt(INTERRUPT_TIMER);

#if DEBUG_MIPS_TIMER
                Log.Debug("Mips Timer Compare set to {0}", value);
#endif
            }


#if DEBUG_INTERRUPTS
            if (i == CTS.CP0_REG_EPC) {
                Log.Debug("EPC was modified by CPU");
            }

            if (i == CTS.CP0_REG_ERROR_EPC) {
                Log.Debug("Error EPC was modified by CPU");
            }
#endif

#if DEBUG_EPC
            if (i == CTS.CP0_REG_EPC) {
                Log.Debug("EPC CPU Write: {0:X8}", value);
            }
#endif

            REGS.CpuRegWrite(i, value);
        }

        public ulong CpuRegisterRead(int i) {
            return REGS.CpuRegRead(i);
        }

        private bool ActiveNMException {
            // TODO: Should compute this at the write of any of these flags 
            get {
                return m_Syscall ||
                m_Break ||
                m_Trap ||
                m_TLBRefillException ||
                m_XTLBRefillExcepetion ||
                m_CacheErr;
            }
        }

        private bool IsServicing => SR.ErrorLevel || SR.ExceptionLevel;

        private bool InterruptsEnabled => SR.InterruptsEnabled && !IsServicing;

        private void CheckTimerInterrupt() {
            /* Timer Interrupt */
            // XXX: Active timer always set the pending bit even if interrupts are disabled
            if (m_EnableTimer) {
                if (REGS.Read(CTS.CP0_REG_COUNT) >= REGS.Read(CTS.CP0_REG_COMPARE))
                {
                    CR.SetInterruptPending(INTERRUPT_TIMER);
                    m_EnableTimer = false;

                    #if DEBUG_MIPS_TIMER
                        Log.Debug("Mips timer has hit the target");

                        if (SR.InterruptsEnabled && !SR.ErrorLevel && !SR.ExceptionLevel) {
                            Log.Debug("MIPS Timer has fired but interrupts are disabled");
                        }
                        else {
                            Log.Debug("MIPS Timer will trigger an interrupt");
                        }
                    #endif
                }
            }
        }

        private void CheckRcpInterrupt() {
            /* RCP Hardware Interrupt */
            if (m_Interface != null) {
                CR.ClearPendingInterrupt(INTERRUPT_RCP);

                if ((m_Interface.Interrupt & m_Interface.Mask) != 0)
                {
                    #if DEBUG_INTERRUPTS && DEBUG_MI
                    Log.Debug("RCP INTERRUPT: {0:X8} AND {1:X8}", m_Interface.Interrupt, m_Interface.Mask);
                    #endif

                    #if !FILTER_RCP_INTERRUPTS
                    CR.SetInterruptPending(INTERRUPT_RCP);
                    #endif
                }
            }
        }

        private void SignalServiceNeeded(ulong pc, ulong target, bool isErrorException, bool isDelaySlot) {
                #if DEBUG_INTERRUPTS
                Log.Debug("Execute Interrupt Handler");
                #endif

                SR.ExceptionLevel = !isErrorException;
                SR.ErrorLevel = isErrorException;

                var selectedEPC = isErrorException ? CTS.CP0_REG_ERROR_EPC : CTS.CP0_REG_EPC;

                if (isDelaySlot)
                {
                    REGS.Write(selectedEPC, pc - 4);
                    CR.SetBranchDelayBit();
                }
                else
                {
                    REGS.Write(selectedEPC, pc);
                }

                #if DEBUG_INTERRUPTS
                Log.Debug("PC to return {0:X8} after servicing interrupt/exception", REGS.CpuRegRead(error ? CTS.CP0_REG_ERROR_EPC : CTS.CP0_REG_EPC));
                #endif

                ExceptionHandlerAddress = target;
                m_TLBRaisedException = false;

                // Ready for servicing
                ServicingRequred = true;
        }

        public void CheckInterrupts(ulong pc, bool isDelaySlot) {
            // Reset some cause bits
            CR.ClearCe();
            CR.ClearBranchDelayBit();

            // TODO: Should precompute based during the write to this flag
            //       Use vars for interrupt_cacheTarget, interrupt_target
            // if (SR.TestFlags(StatusRegister.StatusFlags.UseBootstrapVectors))
            // {
            //     cacheTarget = 0xBFC00200;
            //     target = 0xBFC00200;
            // }
            // else
            // {
            //     cacheTarget = 0xA0000180;
            //     target = 0x80000180;
            // }

            CheckTimerInterrupt();
            CheckRcpInterrupt();

            if (m_Direct) {
                SignalServiceNeeded(pc, 0, false, isDelaySlot);
            }

            else if (m_NMI) {
                m_NMI = false;
                SignalServiceNeeded(pc, 0xBFC00000, true, isDelaySlot);
            }

            /* Reset button has been pushed, set its interrupt */
            else if (m_SoftReset)
            {
                m_SoftReset = false;
                CR.SetInterruptPending(INTERRUPT_RESET);
                SignalServiceNeeded(pc, 0xBFC00000, false, isDelaySlot);
            }

            /* Normal interrupts */
            else if (InterruptsEnabled && (CR.InterruptPending & SR.InterruptMask) != 0) {
                    CR.SetException(ExceptionType.Interrupt, false);
                    SignalServiceNeeded(pc, 0x80000180, false, isDelaySlot);

                    // #if DEBUG_INTERRUPTS
                    // Log.Debug("IP: {0:X8} SR: {1:X8} = {2}", CR.InterruptPending, SR.InterruptMask, executeHandler);
                    // #endif
            }

            CR.ClearThrownException();
        }

        public void CheckExceptions(ulong pc, bool isDelaySlot) {
            // Reset some cause bits
            CR.ClearCe();
            CR.ClearBranchDelayBit();

            // if (SR.TestFlags(StatusRegister.StatusFlags.UseBootstrapVectors))
            // {
            //     cacheTarget = 0xBFC00200;
            //     target = 0xBFC00200;
            // }
            // else
            // {
            //     cacheTarget = 0xA0000000;
            //     target = 0x80000000;
            // }

            if (ActiveNMException || CR.ExceptionThrown) {
                /* 32-bit TLB Exception */
                if (m_TLBRefillException)
                {
                    m_TLBRefillException = false;
                    SignalServiceNeeded(pc, 0x80000080, false, isDelaySlot);
                }

                /* 64-bit TLB Exception */
                else if (m_XTLBRefillExcepetion)
                {
                    m_XTLBRefillExcepetion = false;
                    SignalServiceNeeded(pc, 0x80000080, false, isDelaySlot);
                }

                /* CPU Cache Error */
                else if (m_CacheErr)
                {
                    m_CacheErr = false;
                    SignalServiceNeeded(pc, 0x80000100, true, isDelaySlot);
                }

                /* SYSCALL: Non maskable exception */
                else if (m_Syscall) {
                    m_Syscall = false;
                    SignalServiceNeeded(pc, 0x80000180, false, isDelaySlot);
                }

                /* BREAK: Non maskable exception */
                else if (m_Break) {
                    m_Break = false;
                    SignalServiceNeeded(pc, 0x80000180, false, isDelaySlot);
                }

                /* TRAP: Non maskable exception */
                else if (m_Trap) {
                    m_Trap = false;
                    SignalServiceNeeded(pc, 0x80000180, false, isDelaySlot);
                }

                /* MIPS General Exception */
                else if (CR.ExceptionThrown)
                {
                    Log.Debug("MIPS threw an exception: {0}", CR.Exception);
                    SignalServiceNeeded(pc, 0x80000180, false, isDelaySlot);
                }

                CR.ClearThrownException();
            }
        }

        public bool IsKernelMode => SR.ModeBits == 0;

        public bool IsSupervisorMode => SR.ModeBits == 1;

        public bool IsUserMode => SR.ModeBits == 2;

        public bool IsOperation64 => SR.IsOperation64;

        public bool TLBException => m_TLBRaisedException;

        public void RaiseMipsException(ExceptionType type) {
            State.LLBit = false; // All exceptions cause atomic operations to fail
            CR.SetException(type);

            switch (type) {
                case ExceptionType.TLBLoad:
                case ExceptionType.TLBMod:
                case ExceptionType.TLBStore: m_TLBRaisedException = true; break;
                default: break;
            }
        }

        public void SyscallException() {
            CR.SetException(ExceptionType.Syscall);
            m_Syscall = true;
        }

        public void BreakException() {
            CR.SetException(ExceptionType.Breakpoint);
            m_Break = true;
        }

        public void TrapException() {
            CR.SetException(ExceptionType.Trap);
            m_Trap = true;
        }

        /// <summary>
        /// This should be only set when the system controller detects interrupts waiting to be serviced
        /// </summary>
        public bool ServicingRequred;
    }
}
