using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cor64.IO;
using cor64.Debugging;
using System.Diagnostics;
using System.Threading;

namespace cor64.Mips.R4300I
{
    public class Interpreter : InterpreterBaseR4300I
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private bool m_WarnInfiniteJump;
        private DecodedInstruction? m_InjectedInst;
        private DecodedInstruction m_FailedInstruction;
        private bool m_TakenException;
        private bool m_BranchUnitBusy = false;

        public Interpreter(string abi = "o32") : base(new Disassembler(abi))
        {
            #if CPU_PROFILER
            StartedWithProfiler = true;
            #endif
        }

        private static long L(ulong v)
        {
            return (long)v;
        }

        private bool IsReserved(DecodedInstruction inst)
        {
            #if !CPU_FORCE_32
            return
                (((inst.Op.Flags & ExecutionFlags.Reserved32) == ExecutionFlags.Reserved32) && !IsOperation64) ||
                (((inst.Op.Flags & ExecutionFlags.Reserved64) == ExecutionFlags.Reserved64) && IsOperation64);
            #else
            return false;
            #endif
        }

        public override string Description => "MIPS Interpreter";

        public void CheckForInterrupts()
        {
            // Do not do anything if the system controll has not signeled interrupts that need servicing
            if (Cop0.InterpreterPendingInterrupts && (State.Cp0.Status.ErrorLevel || State.Cp0.Status.ExceptionLevel)) {
                Cop0.InterpreterPendingInterrupts = false;

                m_Pc = Cop0.ExceptionHandlerAddress;

                if (State.Cp0.Status.ErrorLevel)
                {
                    #if DEBUG_INTERRUPTS
                    Log.Debug("Error Handler Taken: " + m_Pc.ToString("X8"));
                    #endif
                }
                else {
                    #if DEBUG_INTERRUPTS
                    Log.Debug("Exception Handler Taken: " + m_Pc.ToString("X8"));
                    #endif
                }

                m_TakenException = true;

                BranchControl.SwitchInterrupt();
            }
        }

        public sealed override void ExceptionReturn(DecodedInstruction inst)
        {
            #if DEBUG_INTERRUPTS

            if (State.Cp0.Status.ErrorLevel) {
                Log.Debug("Jump to Error EPC: " + m_Pc.ToString("X16"));
            }
            else {
                Log.Debug("Jump to EPC: " + m_Pc.ToString("X16"));
            }

            if (!m_TakenException) {
                Log.Debug("ERET called outside a handler: {0:X8}", m_Pc);
            }
            else {
                Log.Debug("ERET called inside a handler");
            }

            #endif

            if (m_TakenException) {
                Cop0.ClearPendingExceptions();

                if (State.Cp0.Status.ErrorLevel)
                {
                    // when servicing an error trap
                    m_Pc = (uint)State.Cp0.RegRead(CTS.CP0_REG_ERROR_EPC);
                    State.Cp0.Status.ErrorLevel = false;
                }
                else
                {
                    m_Pc = (uint)State.Cp0.RegRead(CTS.CP0_REG_EPC);
                    State.Cp0.Status.ExceptionLevel = false;
                }

                BranchControl.SwitchNormal();

                #if DEBUG_INTERRUPTS
                Log.Debug("Interrupt service finished");
                #endif
            }
            else {
                m_Pc = (uint)State.Cp0.RegRead(CTS.CP0_REG_EPC);
                State.Cp0.Status.ExceptionLevel = false;
            }

            m_Pc &= 0xFFFFFFFF;

            // We must subtract by 4 due to the PC increment made after the instuction call
            m_Pc -= 4;

            m_TakenException = false;
            State.LLBit = false;
        }

        private void ExecuteNextInst() {
            /* Delay Slot Logic */
            if (WillJump)
            {
                if (BranchDelay)
                {
                    BranchDelay = false;

                    if (!ExecuteInst())
                    {
                        throw new Exception("Failed to execute instruction in delay slot");
                    }
                }

                TakeBranch = false;
                UnconditionalJump = false;

                /* Should always be a word-aligned relative PC jump */
                /* Always force 32-bit addresses */
                m_Pc = (uint)TargetAddress;
            }
            else
            {
                /* Nornal execution path */
                if (ExecuteInst())
                {
                    m_Pc += 4;
                }
                else
                {
                    throw new Exception(String.Format("Failed to execute instruction: 0x{0:X8} 0x{1:X8}", m_FailedInstruction.Address, m_FailedInstruction.Inst.inst));
                }
            }
        }

        public override void Step()
        {
            /* Step clock */
            CoreClock.NextTick();

            /* Step coprocessor 0 */
            Cop0.ProcessorTick(m_Pc, BranchDelay);

            /* Execute next instruction */
            ExecuteNextInst();

            /* Check for pending exception events only when the branch unit is not busy */
            if (!WillJump) {
                CheckForInterrupts();
            }
        }

        private bool ExecuteInst()
        {
            DecodedInstruction decoded;

            if (m_InjectedInst.HasValue)
            {
                decoded = m_InjectedInst.Value;
                m_InjectedInst = null;
            }
            else
            {
                decoded = Decode();
            }

            // Process NOP
            if (decoded.Inst.op == 0) {
                Cop0.MipsTimerTick(1);
            }

            if (ValidateInstruction(decoded))
            {
                var call = CallTable[decoded];

                if (call == null)
                {
                    throw new NotSupportedException(String.Format("Opcode {0} not supported", decoded.Op.Op));
                }
                else
                {
                    CurrentInst = decoded;

                    if (!NullifyNext)
                    {
                        CoreDbg.TestForInstBreakpoint(decoded);
                        DebugInstruction(decoded);
                        TraceInstruction(decoded, false, m_TakenException);

                        #if CPU_PROFILER
                        BeginInstructionProfile(decoded);
                        #endif

                        call(decoded);

                        #if CPU_PROFILER
                        EndInstructionProfile();
                        #endif
                    }
                    else
                    {
                        NullifyNext = false;
                        TraceInstruction(decoded, true, m_TakenException);
                    }
                    
                    return true;
                }
            }
            else
            {
                m_FailedInstruction = decoded;
                return false;
            }
        }

        [Conditional("DEBUG")]
        private void DebugInstruction(DecodedInstruction instruction)
        {
            if (core_InstDebugMode != InstructionDebugMode.None)
            {
                if (core_InstDebugMode == InstructionDebugMode.Full || (core_InstDebugMode == InstructionDebugMode.ProgramOnly && !InBootMode)) {
                    Console.WriteLine("{0:X8} |{1}| {2}",
                        m_Pc,
                        m_TakenException ? "INTR" : (InBootMode ? "BOOT" : "PROG"),
                        Disassembler.GetFullDisassembly(instruction)
                        );
                }
            }
        }

        public void InjectInst(DecodedInstruction inst)
        {
            m_InjectedInst = inst;
        }

        private void ConvertFromSingle(int source, int dest, ExecutionFlags flags)
        {
            float value = ReadFPR_S(source);

            if ((flags & ExecutionFlags.DataS) == ExecutionFlags.DataS)
            {
                // TODO: Throw invalid exception
                throw new InvalidOperationException("float32 to float32");
            }
            else if ((flags & ExecutionFlags.DataD) == ExecutionFlags.DataD)
            {
                WriteFPR_D(dest, (double)value);
            }
            else if ((flags & ExecutionFlags.Data64) == ExecutionFlags.Data64)
            {
                WriteFPR_DW(dest, (ulong)value);
            }
            else
            {
                WriteFPR_W(dest, (uint)value);
            }
        }

        private void ConvertFromDouble(int source, int dest, ExecutionFlags flags)
        {
            double value = ReadFPR_D(source);

            if ((flags & ExecutionFlags.DataS) == ExecutionFlags.DataS)
            {
                WriteFPR_S(dest, (float)value);
            }
            else if ((flags & ExecutionFlags.DataD) == ExecutionFlags.DataD)
            {
                // TODO: Throw invalid exception
                throw new InvalidOperationException("float64 to float64");
            }
            else if ((flags & ExecutionFlags.Data64) == ExecutionFlags.Data64)
            {
                WriteFPR_DW(dest, (ulong)(long)value);
            }
            else
            {
                WriteFPR_W(dest, (uint)(long)value);
            }
        }

        private void ConvertFromUInt32(int source, int dest, ExecutionFlags flags)
        {
            if ((flags & ExecutionFlags.DataS) == ExecutionFlags.DataS)
            {
                WriteFPR_SNR(dest, (int)ReadFPR_W(source));
            }
            else if ((flags & ExecutionFlags.DataD) == ExecutionFlags.DataD)
            {
                WriteFPR_DNR(dest, (int)ReadFPR_W(source));
            }
            else if ((flags & ExecutionFlags.Data64) == ExecutionFlags.Data64)
            {
                // TODO: Throw invalid exception
                throw new InvalidOperationException("word to word");
            }
            else
            {
                // TODO: Throw invalid exception
                throw new InvalidOperationException("word to dword");
            }
        }

        private void ConvertFromUInt64(int source, int dest, ExecutionFlags flags)
        {
            if ((flags & ExecutionFlags.DataS) == ExecutionFlags.DataS)
            {
                WriteFPR_SNR(dest, (long)ReadFPR_DW(source));
            }
            else if ((flags & ExecutionFlags.DataD) == ExecutionFlags.DataD)
            {
                WriteFPR_DNR(dest, (long)ReadFPR_DW(source));
            }
            else if ((flags & ExecutionFlags.Data64) == ExecutionFlags.Data64)
            {
                // TODO: Throw invalid exception
                throw new InvalidOperationException("word to word");
            }
            else
            {
                // TODO: Throw invalid exception
                throw new InvalidOperationException("word to dword");
            }
        }


        /* ------------------------------------------------------------------
         * Implemented Opcodes
         * ------------------------------------------------------------------
         */

        public override void Add32(DecodedInstruction inst)
        {
            if (IsReserved(inst))
            {
                SetExceptionState(ExceptionType.Reserved);
                return;
            }

            /* Operands must be valid 32-bit sign extended in 64-bit mode */
            /* Immediate is always signed-extended */
            /* Unsigned operations don't cause integer overflow exceptions */

            /* We cheat by doing 32-bit sign-extending for immedate in 64-bit mode,
             * the processor will use 64-bit values to add, and check overflow
             * then store the result as a signed 32-bit value, we force everything to
             * use 32-bits so the CLR runtime can throw exceptions for us */

            bool isUnsigned = inst.IsUnsigned();
            bool isImmediate = inst.IsImmediate();
            uint operandA = ReadGPR32(inst.Source);
            uint operandB = isImmediate ? (uint)(short)inst.Immediate : ReadGPR32(inst.Target);
            uint result = 0;
            int dest = isImmediate ? inst.Target : inst.Destination;

            if (isUnsigned)
            {
                unchecked
                {
                    result = operandA + operandB;
                }
            }
            else
            {
                int r = 0;
                int _a = (int)operandA;
                int _b = (int)operandB;

                try
                {
                    checked
                    {
                        r = _a + _b;
                    }
                }
                catch (OverflowException)
                {
                    SetExceptionState(ExceptionType.Overflow);
                    return;
                }

                result = (uint)r;
            }

            Writeback32(dest, result);
        }

        public override void BitwiseLogic(DecodedInstruction inst)
        {
            if (IsReserved(inst))
            {
                SetExceptionState(ExceptionType.Reserved);
                return;
            }

            bool isImmediate = inst.IsImmediate();
            int dest = isImmediate ? inst.Target : inst.Destination;
            ulong source = ReadGPR64(inst.Source);
            ulong target = isImmediate ? inst.Immediate : ReadGPR64(inst.Target);

            void Bitwise32(ArithmeticOp compareOp, uint a, uint b)
            {
                uint result = 0;

                switch (compareOp)
                {
                    case ArithmeticOp.AND: result = a & b; break;
                    case ArithmeticOp.OR:  result = a | b; break;
                    case ArithmeticOp.XOR: result = a ^ b; break;
                    case ArithmeticOp.NOR: result = ~(a | b); break;
                    default: throw new InvalidOperationException("Bitwise logic 32");
                }

                Writeback64((isImmediate ? inst.Target : inst.Destination), result);
            }

            void Bitwise64(ArithmeticOp compareOp, ulong a, ulong b)
            {
                ulong result = 0;

                switch (compareOp)
                {
                    case ArithmeticOp.AND: result = a & b; break;
                    case ArithmeticOp.OR: result = a | b; break;
                    case ArithmeticOp.XOR: result = a ^ b; break;
                    case ArithmeticOp.NOR: result = ~(a | b); break;
                    default: throw new InvalidOperationException("Bitwise logic 64");
                }

                Writeback64((isImmediate ? inst.Target : inst.Destination), result);
            }


            if (IsOperation64)
            {
                Bitwise64(inst.Op.ArithmeticType, source, target);
            }
            else
            {
                Bitwise32(inst.Op.ArithmeticType, (uint)source, (uint)target);
            }
        }

        public override void Add64(DecodedInstruction inst)
        {
            if (IsReserved(inst))
            {
                SetExceptionState(ExceptionType.Reserved);
                return;
            }

            bool isUnsigned = inst.IsUnsigned();
            bool isImmediate = inst.IsImmediate();
            ulong operandA = ReadGPR64(inst.Source);
            ulong operandB = isImmediate ? (ulong)(short)inst.Immediate : ReadGPR64(inst.Target);
            ulong result = 0;
            int dest = isImmediate ? inst.Target : inst.Destination;

            if (isUnsigned)
            {
                unchecked
                {
                    result = operandA + operandB;
                }
            }
            else
            {
                long r = 0;
                long _a = (long)operandA;
                long _b = (long)operandB;

                try
                {
                    checked
                    {
                        r = _a + _b;
                    }
                }
                catch (OverflowException)
                {
                    SetExceptionState(ExceptionType.Overflow);
                    return;
                }

                result = (ulong)r;
            }

            Writeback64(dest, result);
        }

        public override void Divide32(DecodedInstruction inst)
        {
            if (IsReserved(inst))
            {
                SetExceptionState(ExceptionType.Reserved);
                return;
            }

            bool isUnsigned = inst.IsUnsigned();
            uint operandA = ReadGPR32(inst.Source);
            uint operandB = ReadGPR32(inst.Target);
            uint result = 0;
            uint remainder = 0;

            try
            {
                if (isUnsigned)
                {
                    /* Divide by zero */
                    if (operandB == 0)
                    {
                        WritebackHiLo32(operandA, 0xFFFFFFFFU);
                        return;
                    }

                    unchecked
                    {
                        result = operandA / operandB;
                        remainder = operandA % operandB;
                    }
                }
                else
                {
                    /* Divide by zero */
                    if (operandB == 0)
                    {
                        WritebackHiLo32(operandA, (int)operandA < 0 ? 1U : 0xFFFFFFFFU);
                        return;
                    }

                    int q = 0;
                    int r = 0;
                    int _a = (int)operandA;
                    int _b = (int)operandB;

                    checked
                    {
                        q = _a / _b;
                        r = _a % _b;
                    }

                    result = (uint)q;
                    remainder = (uint)r;
                }

                WritebackHiLo32(remainder, result);
            }
            catch (ArithmeticException)
            {
                WritebackHiLo32(0, operandA);
                return;
            }
        }

        public override void Divide64(DecodedInstruction inst)
        {
            if (IsReserved(inst))
            {
                SetExceptionState(ExceptionType.Reserved);
                return;
            }

            bool isUnsigned = inst.IsUnsigned();
            ulong operandA = ReadGPR64(inst.Source);
            ulong operandB = ReadGPR64(inst.Target);
            ulong result = 0;
            ulong remainder = 0;

            try
            {
                if (isUnsigned)
                {
                    /* Divide by zero */
                    if (operandB == 0)
                    {
                        WritebackHiLo64(operandA, 0xFFFFFFFFFFFFFFFFUL);
                        return;
                    }

                    unchecked
                    {
                        result = operandA / operandB;
                        remainder = operandA % operandB;
                    }
                }
                else
                {
                    /* Divide by zero */
                    if (operandB == 0)
                    {
                        WritebackHiLo64(operandA, (long)operandA < 0 ? 1UL : 0xFFFFFFFFFFFFFFFFUL);
                        return;
                    }

                    long q = 0;
                    long r = 0;
                    long _a = (long)operandA;
                    long _b = (long)operandB;

                    checked
                    {
                        q = _a / _b;
                        r = _a % _b;
                    }

                    result = (ulong)q;
                    remainder = (ulong)r;
                }

                WritebackHiLo64(remainder, result);
            }
            catch (ArithmeticException)
            {
                WritebackHiLo64(0, operandA);
                return;
            }
        }

        public override void Multiply32(DecodedInstruction inst)
        {
            if (IsReserved(inst))
            {
                SetExceptionState(ExceptionType.Reserved);
                return;
            }

            bool isUnsigned = inst.IsUnsigned();
            uint operandA = ReadGPR32(inst.Source);
            uint operandB = ReadGPR32(inst.Target);
            ulong result = 0;
            uint resultLo = 0;
            uint resultHi = 0;

            /* MIPS document says no overflow exceptions, ever... */
            unchecked
            {
                if (isUnsigned)
                    result = (ulong)operandA * (ulong)operandB;
                else
                    result = (ulong)((long)(int)operandA * (long)(int)operandB);

            }

            resultLo = (uint)(result);
            resultHi = (uint)(result >> 32);

            WritebackHiLo32(resultHi, resultLo);
        }

        public override void Multiply64(DecodedInstruction inst)
        {
            if (IsReserved(inst))
            {
                SetExceptionState(ExceptionType.Reserved);
                return;
            }

            bool isUnsigned = inst.IsUnsigned();
            ulong operandA = ReadGPR64(inst.Source);
            ulong operandB = ReadGPR64(inst.Target);

            /* MIPS document says no overflow exceptions, ever... */
            unchecked
            {
                UInt128 value;

                if (isUnsigned)
                {
                    value = CoreUtils.Multiply64_Unsigned(operandA, operandB);
                }
                else
                {
                    value = CoreUtils.Multiply64_Signed(operandA, operandB);
                }

                WritebackHiLo64(value.hi, value.lo);
            }
        }

        public override void Shift32(DecodedInstruction inst)
        {
            if (IsReserved(inst))
            {
                SetExceptionState(ExceptionType.Reserved);
                return;
            }

            int shiftAmount = 0;

            if (inst.IsVariableShift())
            {
                shiftAmount = (int)(ReadGPR32(inst.Source) & 0x3F);
            }
            else
            {
                shiftAmount = inst.ShiftAmount;
            }

            uint value = ReadGPR32(inst.Target);

            if (inst.Op.ArithmeticType == ArithmeticOp.LSHIFT)
            {
                value <<= shiftAmount;
            }
            else
            {
                bool sign = ((value >> 31) == 1);

                value >>= shiftAmount;

                if (sign && !inst.IsUnsigned())
                {
                    /* Sign extend */
                    value |= ~(~0U >> shiftAmount);
                }
            }

            Writeback32(inst.Destination, value);
        }

        public override void Shift64(DecodedInstruction inst)
        {
            if (IsReserved(inst))
            {
                SetExceptionState(ExceptionType.Reserved);
                return;
            }

            int shiftAmount = 0;

            if (inst.IsVariableShift())
            {
                shiftAmount = (int)(ReadGPR32(inst.Source) & 0x3F);
            }
            else
            {
                shiftAmount = inst.ShiftAmount;

                if (inst.IsShift32())
                {
                    shiftAmount += 32;
                }
            }

            ulong value = ReadGPR64(inst.Target);

            if (inst.Op.ArithmeticType == ArithmeticOp.LSHIFT)
            {
                value <<= shiftAmount;
            }
            else
            {
                bool sign = ((value >> 63) == 1);

                value >>= shiftAmount;

                if (sign && !inst.IsUnsigned())
                {
                    /* Sign extend */
                    value |= ~(~0UL >> shiftAmount);
                }
            }

            Writeback64(inst.Destination, value);
        }

        public override void Subtract32(DecodedInstruction inst)
        {
            if (IsReserved(inst))
            {
                SetExceptionState(ExceptionType.Reserved);
                return;
            }

            bool isUnsigned = inst.IsUnsigned();
            uint operandA = ReadGPR32(inst.Source);
            uint operandB = ReadGPR32(inst.Target);
            uint result = 0;

            if (isUnsigned)
            {
                unchecked
                {
                    result = operandA - operandB;
                }
            }
            else
            {
                int r = 0;
                int _a = (int)operandA;
                int _b = (int)operandB;

                try
                {
                    checked
                    {
                        r = _a - _b;
                    }
                }
                catch (OverflowException)
                {
                    SetExceptionState(ExceptionType.Overflow);
                    return;
                }

                result = (uint)r;
            }

            Writeback32(inst.Destination, result);
        }

        public override void Subtract64(DecodedInstruction inst)
        {
            if (IsReserved(inst))
            {
                SetExceptionState(ExceptionType.Reserved);
                return;
            }

            bool isUnsigned = inst.IsUnsigned();
            ulong operandA = ReadGPR64(inst.Source);
            ulong operandB = ReadGPR64(inst.Target);
            ulong result = 0;

            if (isUnsigned)
            {
                unchecked
                {
                    result = operandA - operandB;
                }
            }
            else
            {
                long r = 0;
                long _a = (long)operandA;
                long _b = (long)operandB;

                try
                {
                    checked
                    {
                        r = _a - _b;
                    }
                }
                catch (OverflowException)
                {
                    SetExceptionState(ExceptionType.Overflow);
                    return;
                }

                result = (ulong)r;
            }

            Writeback64(inst.Destination, result);
        }

        public override void SetOnLessThan(DecodedInstruction inst)
        {
            if (IsReserved(inst))
            {
                SetExceptionState(ExceptionType.Reserved);
                return;
            }

            bool unsigned = inst.IsUnsigned();
            bool immediate = inst.IsImmediate();
            ulong mask = IsOperation64 ? 0xFFFFFFFFFFFFFFFF : 0x00000000FFFFFFFF;
            int dest = immediate ? inst.Target : inst.Destination;
            byte result = 0;

            ulong operandA = ReadGPR64(inst.Source) & mask;
            ulong operandB = 0;

            if (immediate)
            {
                operandB = (ulong)(long)(short)inst.Immediate;
            }
            else
            {
                operandB = ReadGPR64(inst.Target);
            }

            operandB &= mask;

            if (unsigned)
            {
                if (operandA < operandB)
                {
                    result = 1;
                }
            }
            else
            {
                if (!IsOperation64)
                {
                    operandA = (ulong)(int)operandA;
                    operandB = (ulong)(int)operandB;
                }

                if ((long)operandA < (long)operandB)
                {
                    result = 1;
                }
            }

            Writeback64(dest, result);
        }

        public override void TransferReg(DecodedInstruction inst)
        {
            ulong value = 0;
            int gpr_Source = inst.Target;
            int gpr_Target = inst.Destination;

            RegTransferGprHelper(inst, out gpr_Source, out gpr_Target);

            /* Source value to copy */
            switch (inst.Op.XferSource)
            {
                case RegBoundType.Hi: value = ReadHi(); break;
                case RegBoundType.Lo: value = ReadLo(); break;
                case RegBoundType.Gpr:
                    {
                        value = ReadGPR64(gpr_Source); break;
                    }
                case RegBoundType.Cp0:
                    {
                        if (!EnableCp0)
                        {
                            SetExceptionState(ExceptionType.Unusable);
                            return;
                        }

                        value = State.Cp0.RegRead(inst.Destination);

                        /* If running 64-bit mode, with the 32-bit version, then sign extend */
                        if (IsOperation64 && inst.IsData32())
                        {
                            value = (ulong)(int)(uint)value;
                        }

                        break;
                    }
                case RegBoundType.Cp1:
                    {
                        if (!EnableCp1)
                        {
                            SetExceptionState(ExceptionType.Unusable);
                            return;
                        }

                        value = ReadFPR_W(inst.FloatSource);

                        /* If running 64-bit mode, with the 32-bit version, then sign extend */
                        if (IsOperation64 && inst.IsData32())
                        {
                            value = (ulong)(int)(uint)value;
                        }

                        break;
                    }

                case RegBoundType.Cp1Ctl:
                    {
                        if (!EnableCp1)
                        {
                            SetExceptionState(ExceptionType.Unusable);
                            return;
                        }

                        value = State.FCR.Value;

                        /* If running 64-bit mode, then sign extend */
                        if (IsOperation64)
                        {
                            value = (ulong)(int)(uint)value;
                        }

                        break;
                    }
            }

            /* Target */
            switch (inst.Op.XferTarget)
            {
                case RegBoundType.Gpr:
                    {
                        Writeback64(gpr_Target, IsOperation64 ? value : (uint)value);
                        break;
                    }

                case RegBoundType.Hi:
                    {
                        WriteHi(IsOperation64 ? value : (uint)value);
                        break;
                    }

                case RegBoundType.Lo:
                    {
                        WriteLo(IsOperation64 ? value : (uint)value);
                        break;
                    }
                case RegBoundType.Cp0:
                    {
                        State.Cp0.RegWrite(inst.Destination, value);
                        break;
                    }
                case RegBoundType.Cp1:
                    {
                        if (!EnableCp1)
                        {
                            SetExceptionState(ExceptionType.Unusable);
                            return;
                        }

                        if (State.Cp0.Status.FRMode)
                        {
                            value &= 0xFFFFFFFF;
                            WriteFPR_DW(inst.FloatSource, value);
                        }
                        else
                        {
                            // XXX: Needs testing
                            // Load hi for even
                            if (inst.FloatSource % 2 == 0)
                            {
                                value >>= 16;
                            }
                            else
                            {
                                value &= 0xFFFF;
                            }

                            WriteFPR_W(inst.FloatSource, (uint)value);
                        }

                        break;
                    }
                case RegBoundType.Cp1Ctl:
                    {
                        if (!EnableCp1)
                        {
                            SetExceptionState(ExceptionType.Unusable);
                            return;
                        }

                        State.FCR.Value = (uint)value;
                        break;
                    }
            }
        }

        public override void Branch(DecodedInstruction inst)
        {
            bool isLikely = inst.IsLikely();
            bool isLink = inst.IsLink();
            TargetAddress = CoreUtils.ComputeBranchPC(IsOperation64, m_Pc, CoreUtils.ComputeBranchTargetOffset(inst.Immediate));
            ulong source = ReadGPR64(inst.Source);
            ulong target = ReadGPR64(inst.Target);

            TakeBranch = ComputeBranchCondition(IsOperation64, source, target, inst.Op.XferTarget, inst.Op.ArithmeticType);

            if (isLink)
            {
                Writeback64(31, m_Pc + 8);
            }

            CoreDbg.TestForBranchBreakpoint((uint)TargetAddress, TakeBranch);

            // Branch delay is always taken for non-likely else, if likely, then condition must be true
            BranchDelay = !isLikely || (TakeBranch && isLikely);

            /* Clear target if not taken */
            if (!TakeBranch)
            {
                TargetAddress = 0;

                if (!BranchDelay)
                {
                    // Nullify the branch delay slot
                    NullifyNext = true;
                }
            }
        }

        public override void Jump(DecodedInstruction inst)
        {
            bool isLink = inst.IsLink();
            bool isRegister = inst.IsRegister();
            BranchDelay = true;
            TargetAddress = CoreUtils.ComputeTargetPC(isRegister, m_Pc, ReadGPR64(inst.Source), inst.Inst.target);
            UnconditionalJump = true;

            if (isLink)
            {
                // TODO: The recompiler will need this fix
                if (isRegister && (inst.Target & 0b11) != 0) {
                    Writeback64(inst.Target, m_Pc + 8);
                }
                else {
                    Writeback64(31, m_Pc + 8);
                }
            }

            CoreDbg.TestForBranchBreakpoint((uint)TargetAddress, true);

            if (!isRegister && (uint)TargetAddress == (uint)m_Pc && !m_WarnInfiniteJump)
            {
                Log.Warn("An unconditional infinite jump was hit: " + inst.Address.ToString("X8"));
                m_WarnInfiniteJump = true;
            }
        }

        public override void Store(DecodedInstruction inst)
        {
            ulong address = 0;
            int size = inst.DataSize();
            ExecutionFlags flags = inst.Op.Flags;

            if (!IsOperation64 && size == 8)
            {
                SetExceptionState(ExceptionType.Reserved);
                return;
            }

            if (!IsOperation64)
            {
                int baseAddress = (int)ReadGPR32(inst.Source);
                int offset = (short)inst.Immediate;
                address = (ulong)(baseAddress + offset);
            }
            else
            {
                long baseAddress = (long)ReadGPR64(inst.Source);
                long offset = (short)inst.Immediate;
                address = (ulong)(baseAddress + offset);
            }

            switch (size)
            {
                default: throw new InvalidOperationException("How did this happen?");
                case 1: m_DataMemory.Data8 = (byte)ReadGPR64(inst.Target); break;
                case 2: m_DataMemory.Data16 = (ushort)ReadGPR64(inst.Target); break;
                case 4: m_DataMemory.Data32 = (uint)ReadGPR32(inst.Target); break;
                case 8: m_DataMemory.Data64 = ReadGPR64(inst.Target); break;
            }

            try
            {
                if (inst.IsLeft())
                {
                    switch (size)
                    {
                        default: break;

                        case 4:
                            {
                                int index = (int)((uint)address & 3);
                                m_DataMemory.ReadData((long)address & ~3, 4);
                                uint val = m_DataMemory.Data32;
                                val &= CTS.SWL_MASK[index];
                                val |= (ReadGPR32(inst.Target) >> CTS.SWL_SHIFT[index]);
                                m_DataMemory.Data32 = val;
                                m_DataMemory.WriteData((long)address, 4);
                                break;
                            }

                        case 8:
                            {
                                int index = (int)((uint)address & 7);
                                m_DataMemory.ReadData((long)address & ~7, 8);
                                ulong val = m_DataMemory.Data64;
                                val &= CTS.SDL_MASK[index];
                                val |= (ReadGPR64(inst.Target) >> CTS.SDL_SHIFT[index]);
                                m_DataMemory.Data64 = val;
                                m_DataMemory.WriteData((long)address, 8);
                                break;
                            }
                    }
                }
                else if (inst.IsRight())
                {
                    switch (size)
                    {
                        default: break;

                        case 4:
                            {
                                int index = (int)((uint)address & 3);
                                m_DataMemory.ReadData((long)address & ~3, 4);
                                uint val = m_DataMemory.Data32;
                                val &= CTS.SWR_MASK[index];
                                val |= (ReadGPR32(inst.Target) << CTS.SWR_SHIFT[index]);
                                m_DataMemory.Data32 = val;
                                m_DataMemory.WriteData((long)address, 4);
                                break;
                            }

                        case 8:
                            {
                                int index = (int)((uint)address & 7);
                                m_DataMemory.ReadData((long)address & ~7, 8);
                                ulong val = m_DataMemory.Data64;
                                val &= CTS.SDR_MASK[index];
                                val |= (ReadGPR64(inst.Target) << CTS.SDR_SHIFT[index]);
                                m_DataMemory.Data64 = val;
                                m_DataMemory.WriteData((long)address, 8);
                                break;
                            }
                    }
                }
                else
                {
                    /* Conditional store notes:
                     * The LLBit must be set a Load linked operation before this type of operation
                     * TODO: When ERET instruction is supported, it causes any conditional store to fail afterwards
                     */

                    bool LLMode = (flags & ExecutionFlags.Link) == ExecutionFlags.Link;

                    if (!LLMode || (LLMode && State.LLBit))
                    {
                        m_DataMemory.WriteData((long)address, size);

                        //if (m_Debug && inst.Target == 31)
                        //{
                        //    AddInstructionNote(String.Format("Stored return address {0:X8} into memory", m_DataMemory.Data32));
                        //}
                    }

                    /* Store conditional */
                    if (LLMode)
                    {
                        Writeback64(inst.Target, State.LLBit ? 1U : 0U);
                    }
                }
            }
            catch (MipsException e)
            {
                SetExceptionState(e.Exception);
            }
        }

        public override void Load(DecodedInstruction inst)
        {
            /* Modes: Upper Immediate, Unsigned / Signed (8, 16, 32, 64), Left / Right (32, 64), Load Linked */
            ulong address = 0;
            int size = 0;

            bool upperImm = inst.IsImmediate();
            bool loadLinked = inst.IsLink();
            bool left = inst.IsLeft();
            bool right = inst.IsRight();
            bool unsigned = inst.IsUnsigned();

            try
            {
                if (upperImm)
                {
                    uint imm = inst.Immediate;
                    imm <<= 16;

                    Writeback32(inst.Target, imm);

                    return;
                }
                else
                {
                    size = inst.DataSize();

                    if (!IsOperation64 && size == 8)
                    {
                        SetExceptionState(ExceptionType.Reserved);
                        return;
                    }

                    if (!IsOperation64)
                    {
                        int baseAddress = (int)ReadGPR32(inst.Source);
                        int offset = (short)inst.Immediate;
                        address = (ulong)(baseAddress + offset);
                    }
                    else
                    {
                        long baseAddress = (long)ReadGPR64(inst.Source);
                        long offset = (short)inst.Immediate;
                        address = (ulong)(baseAddress + offset);
                    }

                    // REF: https://www2.cs.duke.edu/courses/cps104/fall02/homework/lwswlr.html
                    // REF: Project64 source

                    if (left)
                    {
                        switch (size)
                        {
                            default: break;

                            case 4:
                                {
                                    int index = (int)((uint)address & 3);
                                    m_DataMemory.ReadData((long)address & ~3, 4);
                                    uint val = ReadGPR32(inst.Target);
                                    val &= CTS.LWL_MASK[index];
                                    val |= (m_DataMemory.Data32 << CTS.LWL_SHIFT[index]);
                                    m_DataMemory.Data32 = val;
                                    break;
                                }

                            case 8:
                                {
                                    int index = (int)((uint)address & 7);
                                    m_DataMemory.ReadData((long)address & ~7, 8);
                                    ulong val = ReadGPR64(inst.Target);
                                    val &= CTS.LDL_MASK[index];
                                    val |= (m_DataMemory.Data64 << CTS.LDL_SHIFT[index]);
                                    m_DataMemory.Data64 = val;
                                    break;
                                }
                        }
                    }
                    else if (right)
                    {
                        switch (size)
                        {
                            default: break;

                            case 4:
                                {
                                    int index = (int)((uint)address & 3);
                                    m_DataMemory.ReadData((long)address & ~3, 4);
                                    uint val = ReadGPR32(inst.Target);
                                    val &= CTS.LWR_MASK[index];
                                    val |= (m_DataMemory.Data32 >> CTS.LWR_SHIFT[index]);
                                    m_DataMemory.Data32 = val;
                                    break;
                                }

                            case 8:
                                {
                                    int index = (int)((uint)address & 7);
                                    m_DataMemory.ReadData((long)address & ~7, 8);
                                    ulong val = ReadGPR64(inst.Target);
                                    val &= CTS.LDR_MASK[index];
                                    val |= (m_DataMemory.Data64 >> CTS.LDR_SHIFT[index]);
                                    m_DataMemory.Data64 = val;
                                    break;
                                }
                        }
                    }
                    else
                    {
                        if (loadLinked)
                        {
                            State.LLBit = true;
                        }

                        m_DataMemory.ReadData((long)address, size);
                    }

                    if (unsigned)
                    {
                        switch (size)
                        {
                            default: throw new InvalidOperationException("How did this happen (unsigned)?");
                            case 1: Writeback64(inst.Target, m_DataMemory.Data8); break;
                            case 2: Writeback64(inst.Target, m_DataMemory.Data16); break;
                            case 4: Writeback64(inst.Target, m_DataMemory.Data32); break;
                            case 8: Writeback64(inst.Target, m_DataMemory.Data64); break;
                        }
                    }
                    else
                    {
                        switch (size)
                        {
                            default: throw new InvalidOperationException("How did this happen?");
                            case 1: Writeback64(inst.Target, (ulong)(sbyte)m_DataMemory.Data8); break;
                            case 2: Writeback64(inst.Target, (ulong)(short)m_DataMemory.Data16); break;
                            case 4: Writeback64(inst.Target, (ulong)(int)m_DataMemory.Data32); break;
                            case 8: Writeback64(inst.Target, (ulong)(long)m_DataMemory.Data64); break;
                        }
                    }
                }
            }
            catch (MipsException e)
            {
                SetExceptionState(e.Exception);
            }
        }

        public override void Cache(DecodedInstruction inst)
        {
        }

        public override void Sync(DecodedInstruction inst)
        {
        }

        public override void FloatLoad(DecodedInstruction inst)
        {
            try
            {
                var size = inst.DataSize();

                long baseAddress = (long)ReadGPR64(inst.Source);
                long offset = (short)inst.Immediate;
                var address = (ulong)(baseAddress + offset);

                m_DataMemory.ReadData((long)address, size);

                switch (size)
                {
                    default: throw new InvalidOperationException("Unsupported FPU Load Size: " + size.ToString());
                    case 4: WriteFPR_W(inst.FloatTarget, m_DataMemory.Data32); break;
                    case 8: WriteFPR_DW(inst.FloatTarget, m_DataMemory.Data64); break;
                }

                /* If loading a doubleword and FR = 0, we don't care, we bypass 32-bit stuff */
            }
            catch (MipsException e)
            {
                SetExceptionState(e.Exception);
            }

            /* TODO: Simulate odd result registers for 64-bit reads ? */
        }

        public override void FloatStore(DecodedInstruction inst)
        {
            var size = inst.DataSize();

            long baseAddress = (long)ReadGPR64(inst.Source);
            long offset = (short)inst.Immediate;
            var address = (ulong)(baseAddress + offset);

            switch (size)
            {
                default: throw new InvalidOperationException("Unsupported FPU Store Size: " + size.ToString());
                case 4: m_DataMemory.Data32 = ReadFPR_W(inst.FloatTarget); break;
                case 8: m_DataMemory.Data64 = ReadFPR_DW(inst.FloatTarget); break;
            }

            try
            {
                m_DataMemory.WriteData((long)address, size);
            }
            catch (MipsException e)
            {
                SetExceptionState(e.Exception);
            }
        }

        public override void Add(DecodedInstruction inst)
        {
            if (inst.Format == FpuValueType.FSingle)
            {
                WriteFPR_S(inst.FloatDest, ReadFPR_S(inst.FloatSource) + ReadFPR_S(inst.FloatTarget));
            }
            else if (inst.Format == FpuValueType.FDouble)
            {
                WriteFPR_D(inst.FloatDest, ReadFPR_D(inst.FloatSource) + ReadFPR_D(inst.FloatTarget));
            }
            else
            {
                SetExceptionState(FpuExceptionFlags.Unimplemented);
            }
        }

        public override void Subtract(DecodedInstruction inst)
        {
            if (inst.Format == FpuValueType.FSingle)
            {
                WriteFPR_S(inst.FloatDest, ReadFPR_S(inst.FloatSource) - ReadFPR_S(inst.FloatTarget));
            }
            else if (inst.Format == FpuValueType.FDouble)
            {
                WriteFPR_D(inst.FloatDest, ReadFPR_D(inst.FloatSource) - ReadFPR_D(inst.FloatTarget));
            }
            else
            {
                SetExceptionState(FpuExceptionFlags.Unimplemented);
            }
        }

        public override void Multiply(DecodedInstruction inst)
        {
            if (inst.Format == FpuValueType.FSingle)
            {
                WriteFPR_S(inst.FloatDest, ReadFPR_S(inst.FloatSource) * ReadFPR_S(inst.FloatTarget));
            }
            else if (inst.Format == FpuValueType.FDouble)
            {
                WriteFPR_D(inst.FloatDest, ReadFPR_D(inst.FloatSource) * ReadFPR_D(inst.FloatTarget));
            }
            else
            {
                SetExceptionState(FpuExceptionFlags.Unimplemented);
            }
        }

        public override void Divide(DecodedInstruction inst)
        {
            if (inst.Format == FpuValueType.FSingle)
            {
                WriteFPR_S(inst.FloatDest, ReadFPR_S(inst.FloatSource) / ReadFPR_S(inst.FloatTarget));
            }
            else if (inst.Format == FpuValueType.FDouble)
            {
                WriteFPR_D(inst.FloatDest, ReadFPR_D(inst.FloatSource) / ReadFPR_D(inst.FloatTarget));
            }
            else
            {
                SetExceptionState(FpuExceptionFlags.Unimplemented);
            }
        }

        public override void SqrRoot(DecodedInstruction inst)
        {
            if (inst.Format == FpuValueType.FSingle)
            {
                WriteFPR_S(inst.FloatDest, (float)Math.Sqrt(ReadFPR_S(inst.FloatSource)));
            }
            else if (inst.Format == FpuValueType.FDouble)
            {
                WriteFPR_D(inst.FloatDest, Math.Sqrt(ReadFPR_D(inst.FloatSource)));
            }
            else
            {
                SetExceptionState(FpuExceptionFlags.Unimplemented);
            }
        }

        public override void Abs(DecodedInstruction inst)
        {
            if (inst.Format == FpuValueType.FSingle)
            {
                WriteFPR_S(inst.FloatDest, Math.Abs(ReadFPR_S(inst.FloatSource)));
            }
            else if (inst.Format == FpuValueType.FDouble)
            {
                WriteFPR_D(inst.FloatDest, Math.Abs(ReadFPR_D(inst.FloatSource)));
            }
            else
            {
                SetExceptionState(FpuExceptionFlags.Unimplemented);
            }
        }

        public override void Mov(DecodedInstruction inst)
        {
            if (inst.Format == FpuValueType.FSingle)
            {
                WriteFPR_S(inst.FloatDest, ReadFPR_S(inst.FloatSource));
            }
            else if (inst.Format == FpuValueType.FDouble)
            {
                WriteFPR_D(inst.FloatDest, ReadFPR_D(inst.FloatSource));
            }
            else
            {
                SetExceptionState(FpuExceptionFlags.Unimplemented);
            }
        }

        public override void Neg(DecodedInstruction inst)
        {
            if (inst.Format == FpuValueType.FSingle)
            {
                WriteFPR_S(inst.FloatDest, -ReadFPR_S(inst.FloatSource));
            }
            else if (inst.Format == FpuValueType.FDouble)
            {
                WriteFPR_D(inst.FloatDest, -ReadFPR_D(inst.FloatSource));
            }
            else
            {
                SetExceptionState(FpuExceptionFlags.Unimplemented);
            }
        }

        public override void Round(DecodedInstruction inst)
        {
            double roundedValue;

            if (inst.Format == FpuValueType.FSingle)
            {
                roundedValue = Math.Round(ReadFPR_S(inst.FloatSource));
            }
            else if (inst.Format == FpuValueType.FDouble)
            {
                roundedValue = Math.Round(ReadFPR_D(inst.FloatSource));
            }
            else
            {
                SetExceptionState(FpuExceptionFlags.Unimplemented);
                return;
            }

            if (inst.IsData32())
            {
                WriteFPR_W(inst.FloatDest, (uint)roundedValue);
            }
            else
            {
                WriteFPR_DW(inst.FloatDest, (ulong)roundedValue);
            }
        }

        public override void Truncate(DecodedInstruction inst)
        {
            double roundedValue;

            if (inst.Format == FpuValueType.FSingle)
            {
                roundedValue = Math.Truncate(ReadFPR_S(inst.FloatSource));
            }
            else if (inst.Format == FpuValueType.FDouble)
            {
                roundedValue = Math.Truncate(ReadFPR_D(inst.FloatSource));
            }
            else
            {
                SetExceptionState(FpuExceptionFlags.Unimplemented);
                return;
            }

            if (inst.IsData32())
            {
                WriteFPR_W(inst.FloatDest, (uint)roundedValue);
            }
            else
            {
                WriteFPR_DW(inst.FloatDest, (ulong)roundedValue);
            }
        }

        public override void Ceil(DecodedInstruction inst)
        {
            double roundedValue;

            if (inst.Format == FpuValueType.FSingle)
            {
                roundedValue = Math.Ceiling(ReadFPR_S(inst.FloatSource));
            }
            else if (inst.Format == FpuValueType.FDouble)
            {
                roundedValue = Math.Ceiling(ReadFPR_D(inst.FloatSource));
            }
            else
            {
                SetExceptionState(FpuExceptionFlags.Unimplemented);
                return;
            }

            if (inst.IsData32())
            {
                WriteFPR_W(inst.FloatDest, (uint)roundedValue);
            }
            else
            {
                WriteFPR_DW(inst.FloatDest, (ulong)roundedValue);
            }
        }

        public  override void Floor(DecodedInstruction inst)
        {
            double roundedValue;

            if (inst.Format == FpuValueType.FSingle)
            {
                roundedValue = Math.Floor(ReadFPR_S(inst.FloatSource));
            }
            else if (inst.Format == FpuValueType.FDouble)
            {
                roundedValue = Math.Floor(ReadFPR_D(inst.FloatSource));
            }
            else
            {
                SetExceptionState(FpuExceptionFlags.Unimplemented);
                return;
            }

            if (inst.IsData32())
            {
                WriteFPR_W(inst.FloatDest, (uint)roundedValue);
            }
            else
            {
                WriteFPR_DW(inst.FloatDest, (ulong)roundedValue);
            }
        }

        public override void Convert(DecodedInstruction inst)
        {
            switch (inst.Format)
            {
                case FpuValueType.FSingle: ConvertFromSingle(inst.FloatSource, inst.FloatDest, inst.Op.Flags); break;
                case FpuValueType.FDouble: ConvertFromDouble(inst.FloatSource, inst.FloatDest, inst.Op.Flags); break;
                case FpuValueType.Word: ConvertFromUInt32(inst.FloatSource, inst.FloatDest, inst.Op.Flags); break;
                case FpuValueType.Doubleword: ConvertFromUInt64(inst.FloatSource, inst.FloatDest, inst.Op.Flags); break;
                default: SetExceptionState(FpuExceptionFlags.Unimplemented); break;
            }
        }

        /* Exceptions: Invalid and Unimplemented */
        public override void Condition(DecodedInstruction inst)
        {
            bool flag_signaling = inst.Op.ArithmeticType == ArithmeticOp.SIGNALING;
            bool flag_equal = inst.Op.Flags.TestFlag(ExecutionFlags.CondEq);
            bool flag_less_than = inst.Op.Flags.TestFlag(ExecutionFlags.CondLT);
            bool flag_greater_than = inst.Op.Flags.TestFlag(ExecutionFlags.CondGT);
            bool flag_not = inst.Op.Flags.TestFlag(ExecutionFlags.CondNot);
            bool flag_unordered = inst.Op.Flags.TestFlag(ExecutionFlags.CondUn);
            bool flag_forced_ordered = inst.Op.Flags.TestFlag(ExecutionFlags.CondOrd);

            bool result = false;

            double a = 0.0d;
            double b = 0.0d;
            bool hasNaN = false;

            if (inst.Format == FpuValueType.FSingle || inst.Format == FpuValueType.FDouble)
            {
                if (inst.Format == FpuValueType.FSingle)
                {
                    a = ReadFPR_S(inst.FloatSource);
                    b = ReadFPR_S(inst.FloatTarget);
                }
                else
                {
                    a = ReadFPR_D(inst.FloatSource);
                    b = ReadFPR_D(inst.FloatTarget);
                }

                hasNaN = double.IsNaN(a) || double.IsNaN(b);

                if (flag_forced_ordered && hasNaN)
                {
                    SetExceptionState(FpuExceptionFlags.Invalid);
                }
                else
                {
                    if (flag_unordered)
                    {
                        result |= hasNaN;

                        if (flag_signaling)
                        {
                            /* Compare that both are signaling NaNs */
                            // TODO: This needs real testing, and on some website I found that this always results false.
                            throw new Exception();
                            //result |= (State.FPR.SignalNaNTable[inst.FloatSource] == State.FPR.SignalNaNTable[inst.FloatTarget]);
                        }
                    }

                    if (flag_equal)
                    {
                        result |= (a == b);
                    }

                    if (flag_less_than)
                    {
                        result |= (a < b);
                    }

                    if (flag_greater_than)
                    {
                        result |= (a > b);
                    }
                }

                if (flag_not)
                {
                    result = !result;
                }
             
                State.FCR.Condition = result;
            }
            else
            {
                SetExceptionState(FpuExceptionFlags.Unimplemented);
            }
        }

        public override void Trap(DecodedInstruction inst) {
            bool isImmediate = inst.IsImmediate();
            bool isUnsigned = inst.IsUnsigned();
            var result = false;

            void Compare64() {
                if (isUnsigned) {
                    var source = ReadGPR64(inst.Source);
                    var target = isImmediate ? (ulong)(short)inst.Immediate : ReadGPR64(inst.Target);

                    result = inst.Op.ArithmeticType switch
                    {
                        ArithmeticOp.EQUAL => source == target,
                        ArithmeticOp.LESS_THAN => source < target,
                        ArithmeticOp.GREATER_THAN => source > target,
                        ArithmeticOp.NOT_EQUAL => source != target,
                        _ => throw new EmuException("Invalid trap condition type"),
                    };
                }
                else {
                    var source = (long)ReadGPR64(inst.Source);
                    var target = isImmediate ? (long)(short)inst.Immediate : (long)ReadGPR64(inst.Target);

                    result = inst.Op.ArithmeticType switch
                    {
                        ArithmeticOp.EQUAL => source == target,
                        ArithmeticOp.LESS_THAN => source < target,
                        ArithmeticOp.GREATER_THAN => source > target,
                        ArithmeticOp.NOT_EQUAL => source != target,
                        _ => throw new EmuException("Invalid trap condition type"),
                    };
                }
            }

            void Compare32() {
                if (isUnsigned) {
                    var source = ReadGPR32(inst.Source);
                    var target = isImmediate ? (uint)(short)inst.Immediate : ReadGPR32(inst.Target);

                    result = inst.Op.ArithmeticType switch
                    {
                        ArithmeticOp.EQUAL => source == target,
                        ArithmeticOp.LESS_THAN => source < target,
                        ArithmeticOp.GREATER_THAN => source > target,
                        ArithmeticOp.NOT_EQUAL => source != target,
                        _ => throw new EmuException("Invalid trap condition type"),
                    };
                }
                else {
                    var source = (int)ReadGPR32(inst.Source);
                    var target = isImmediate ? (int)(short)inst.Immediate : (int)ReadGPR32(inst.Target);

                    result = inst.Op.ArithmeticType switch
                    {
                        ArithmeticOp.EQUAL => source == target,
                        ArithmeticOp.LESS_THAN => source < target,
                        ArithmeticOp.GREATER_THAN => source > target,
                        ArithmeticOp.NOT_EQUAL => source != target,
                        _ => throw new EmuException("Invalid trap condition type"),
                    };
                }
            }

            if (IsOperation64) {
                Compare64();
            }
            else {
                Compare32();
            }

            if (result) {
                SetExceptionState(ExceptionType.Trap);
            }
        }

        public override void Break(DecodedInstruction inst) {
            SetExceptionState(ExceptionType.Breakpoint);
        }

        public bool InfiniteLoopWarn => m_WarnInfiniteJump;
    }
}
