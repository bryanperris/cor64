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
using cor64.HLE;
using cor64.HLE.OS;
using cor64.HLE.Debug;

namespace cor64.Mips.R4300I
{
    public class Interpreter : InterpreterBaseR4300I
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private bool m_WarnInfiniteJump;
        private DecodedInstruction? m_InjectedInst;
        private bool m_TakenException;
        private long? m_InfiniteJumpAddress;
        private bool m_ExceptionReturn;
        private readonly Dictionary<long, CpuCodeHook> m_Hooks = new();
        private long m_LastFetchAddress;

        public Interpreter(string abi = "o32") : base(new Disassembler(abi))
        {
            #if CPU_PROFILER
            StartedWithProfiler = true;
            #endif
        }

        private bool IsReserved(DecodedInstruction inst)
        {
            #if !CPU_CHECK_RESERVED
            return false;
            #else
            #if !CPU_FORCE_32
            return
                (((inst.Op.Flags & ExecutionFlags.Reserved32) == ExecutionFlags.Reserved32) && !IsOperation64) ||
                (((inst.Op.Flags & ExecutionFlags.Reserved64) == ExecutionFlags.Reserved64) && IsOperation64);
            #else
            return false;
            #endif
            #endif
        }

        protected override void EntryPointHit()
        {
            base.EntryPointHit();

            DebugHooks.AddHooks(this, m_Hooks);

            // OSHooks_1.AddHooks(this, m_Hooks);
            // m_Hooks.Add(0x8018D298, new FatalPrintf(this));
        }

        public override string Description => "MIPS Interpreter";

        public bool CheckIfNeedServicing()
        {
            // Do not do anything if the system control has not signeled interrupts that need servicing
            if (Cop0.ServicingRequred && (Cop0State.Status.ErrorLevel || Cop0State.Status.ExceptionLevel)) {
                Cop0.ServicingRequred = false;

                // Clear the branch state
                ClearBranchState();

                PC = Cop0.ExceptionHandlerAddress;

                if (Cop0State.Status.ErrorLevel)
                {
                    #if DEBUG_INTERRUPTS
                    Log.Debug("Error Handler Taken: " + PC.ToString("X8"));
                    #endif
                }
                else {
                    #if DEBUG_INTERRUPTS
                    Log.Debug("Exception Handler Taken: " + PC.ToString("X8"));
                    #endif
                }

                m_TakenException = true;

                #if DEBUG
                TraceInterrupt();
                #endif

                return true;
            }

            return false;
        }

        public sealed override void ExceptionReturn(DecodedInstruction inst)
        {
            #if DEBUG_INTERRUPTS || DEBUG_ERET

            if (!m_TakenException) {
                Log.Debug("ERET called outside a handler: {0:X8}", PC);
            }
            else {
                Log.Debug("ERET called inside a handler");
            }

            #endif

            if (m_TakenException) {
                if (Cop0State.Status.ErrorLevel)
                {
                    // when servicing an error trap
                    PC = (uint)Cop0.CpuRegisterRead(CTS.CP0_REG_ERROR_EPC);
                    Cop0State.Status.ErrorLevel = false;
                }
                else
                {
                    PC = (uint)Cop0.CpuRegisterRead(CTS.CP0_REG_EPC);

                    #if DEBUG_EPC
                    Log.Debug("EPC -> PC: {0:X8}", PC);
                    #endif

                    Cop0State.Status.ExceptionLevel = false;
                }

                #if DEBUG_INTERRUPTS
                Log.Debug("Interrupt service finished");
                #endif
            }
            else {
                PC = (uint)Cop0.CpuRegisterRead(CTS.CP0_REG_EPC);

                #if DEBUG_EPC
                Log.Debug("EPC -> PC: {0:X8}", PC);
                #endif

                Cop0State.Status.ExceptionLevel = false;
            }

            #if DEBUG_INTERRUPTS || DEBUG_ERET

            if (Cop0State.Status.ErrorLevel && m_TakenException) {
                Log.Debug("Jump to Error EPC: " + PC.ToString("X16"));
            }
            else {
                Log.Debug("Jump to EPC: " + PC.ToString("X16"));
            }

            #endif

            PC &= 0xFFFFFFFF;

            m_TakenException = false;
            State.LLBit = false;
            m_ExceptionReturn = true;
        }

        public override void Step()
        {
            ExecuteNext();
        }

        private bool ExecuteNext() {
            // TODO: still handle the result

            #if !OPTIMAL
            if (Debugger.IsBreakActive) {
                return true;
            }
            #endif

            TLBTick();

            // Check if the system controller detects pending events
            Cop0.CheckInterrupts(PC, BU_ExecuteBranchDelay);

            // Check if the CPU should start service handling
            CheckIfNeedServicing();

            /* Virtual To Physical */
            if (!TryVirtToPhys(false, PC, out long physicalAddress))
            {
                // Exception raised during fetch stage
                // Return right away, let the exception handler work on the next step
                return true;
            }

            bool hleJump = false;

            // CLEANUP: Move this into a conditional method?
            #if ENABLE_CPU_HOOKS
            if (m_Hooks.TryGetValue(PC, out CpuCodeHook hook))
            {
                var result = hook.Execute();

                if (result == CpuCodeHook.ReturnControl.HLECall) {
                    // Execute a 'jr ra' branch
                    CurrentInst = Disassembler.Decode(0x03E00008);
                    hleJump = true;
                }
                else {
                    CurrentInst = DecodeNext(physicalAddress);
                }
            }
            else {
                CurrentInst = DecodeNext(physicalAddress);
            }

            #else

            CurrentInst = DecodeNext(physicalAddress);

            #endif

            m_LastFetchAddress = PC;

            Cop0.MipsTimerTick(1);

            if (CurrentInst.IsInvalid)
                return false;

            var instHandler = CallTable[CurrentInst];

            if (instHandler == null)
                throw new NotSupportedException(String.Format("Opcode {0} not supported", CurrentInst.Op.Op));

            #if !OPTIMAL
            Debugger.CheckInstructionBreakpoints(CurrentInst);
            #endif

            #if DEBUG
            TraceInstruction(PC, CurrentInst);
            #endif

            /* --- EXECUTE INSTRUCTION HANDLER */

            #if CPU_PROFILER
            BeginInstructionProfile(CurrentInst);
            #endif

            // EXECUTE HANDLER
            instHandler(CurrentInst);

            #if CPU_PROFILER
            EndInstructionProfile();
            #endif

            #if DEBUG
            DebugInstruction(CurrentInst, PC, m_TakenException);
            #endif

            DebugFpu(CurrentInst);

            /* --------------------------------- */

            // Returned from vector routine via ERET
            // We don't do anything else, because we have to
            // still execute the interrupted instruction
            if (m_ExceptionReturn) {
                m_ExceptionReturn = false;
                return true;
            }

            // Checked for rasied exceptions
            Cop0.CheckExceptions(PC, BU_ExecuteBranchDelay);

            // Check if the CPU should start service handling
            if (!hleJump) {
                if (!CheckIfNeedServicing()) {
                    // Shift PC to next instruction
                    PC += 4;
                }
            }

            BU_ExecuteBranchDelay = false;

            // Peform the branch or jump if conditions are met
            if (hleJump || ((BU_ConditionalPending || BU_UnconditionalPending) && !BU_DelaySlotNext)) {
                /* Should always be a word-aligned relative PC jump */
                /* Always force 32-bit addresses */
                PC = (uint)BU_TargetAddress;
                BU_ConditionalPending = false;
                BU_UnconditionalPending = false;
                return true;
            }

            // The delay slot is next
            if (BU_DelaySlotNext) {

                if (BU_NullifyNext) {
                    BU_ExecuteBranchDelay = false;
                    BU_DelaySlotNext = false;
                    BU_NullifyNext = false;
                    PC += 4; // Move PC to inst after delay slot
                }
                else {
                    BU_DelaySlotNext = false;
                    BU_ExecuteBranchDelay = true;
                }
            }

            return true;
        }

        private DecodedInstruction DecodeNext(long physicalAddress) {
            if (m_InjectedInst.HasValue)
            {
                var decoded = m_InjectedInst.Value;
                m_InjectedInst = null;
                return decoded;
            }
            else
            {
                return Decode(physicalAddress);
            }
        }

        private void DebugInstruction(DecodedInstruction instruction, long pc, bool inInt)
        {
            if (core_InstDebugMode != InstructionDebugMode.None)
            {
                if (core_InstDebugMode == InstructionDebugMode.Full || (core_InstDebugMode == InstructionDebugMode.ProgramOnly && !InBootMode)) {
                    if (m_InfiniteJumpAddress != null) {
                        if (PC == m_InfiniteJumpAddress || PC == (m_InfiniteJumpAddress + 4)) {
                            return;
                        }
                    }
                    else {
                        if (m_WarnInfiniteJump) {
                            m_InfiniteJumpAddress = PC;
                        }
                    }

                    string memNote = null;

                    if (core_MemAccessNote != null) {
                        memNote = "     # " + core_MemAccessNote.ReadMeta();
                        core_MemAccessNote = null;
                    }

                    String type = inInt ? "INTR" : (InBootMode ? "BOOT" : "PROG");

                    #if TESTING
                    type = "TEST";
                    #endif

                    // if (memNote != null)
                    Console.WriteLine("CPU {0:X8} {4:X8} |{1}| {2} {3}",
                        pc,
                        type,
                        Disassembler.Disassemble(pc, instruction),
                        memNote ?? "",
                        instruction.Inst.inst
                        );
                }
            }
        }

        [Conditional("FPU_DEBUG_INST")]
        private void DebugFpu(DecodedInstruction inst) {
            if (inst.Op.Family == OperationFamily.Fpu || inst.Op.Family == OperationFamily.LoadFpu || inst.Op.Family == OperationFamily.StoreFpu) {
                Console.WriteLine("FPU {0:X8} {1}",
                    PC,
                    Disassembler.Disassemble(inst)
                    );
            }
        }

        public void InjectInst(DecodedInstruction inst)
        {
            m_InjectedInst = inst;
        }

        private void ConvertToSingle(FpuValueType format, int source, int dest)
        {
            switch (format) {
                case FpuValueType.FSingle: throw new InvalidOperationException("float32 to float32");
                case FpuValueType.FDouble: WriteFPR_S(dest, (float)ReadFPR_D(source)); break;
                case FpuValueType.Word: WriteFPR_S(dest, (int)ReadFPR_W(source)); break;
                case FpuValueType.Doubleword:  WriteFPR_S(dest, (long)ReadFPR_DW(source)); break;
                default: throw new InvalidOperationException("cannot convert to reserved format");
            }
        }

        private void ConvertToDouble(FpuValueType format, int source, int dest)
        {
            switch (format) {
                case FpuValueType.FSingle: WriteFPR_D(dest, ReadFPR_S(source)); break;
                case FpuValueType.FDouble: throw new InvalidOperationException("float64 to float64");
                case FpuValueType.Word: WriteFPR_D(dest, (int)ReadFPR_W(source)); break;
                case FpuValueType.Doubleword: WriteFPR_D(dest, (long)ReadFPR_DW(source)); break;
                default: throw new InvalidOperationException("cannot convert to reserved format");
            }
        }

        private void ConvertToFixed32(FpuValueType format, int source, int dest)
        {
            switch (format) {
                case FpuValueType.FSingle: WriteFPR_DW(dest, 0); WriteFPR_W(dest, (uint)(int)ReadFPR_S(source)); break;
                case FpuValueType.FDouble: WriteFPR_W(dest, (uint)(int)ReadFPR_D(source)); break;
                case FpuValueType.Word: throw new InvalidOperationException("u32 to u32");
                case FpuValueType.Doubleword: throw new InvalidOperationException("u32 to u64");
                default: throw new InvalidOperationException("cannot convert to reserved format");
            }
        }

        private void ConvertToFixed64(FpuValueType format, int source, int dest)
        {
            switch (format) {
                case FpuValueType.FSingle: WriteFPR_DW(dest, (ulong)(int)ReadFPR_S(source)); break;
                case FpuValueType.FDouble: WriteFPR_DW(dest, (ulong)(long)ReadFPR_D(source)); break;
                case FpuValueType.Word: throw new InvalidOperationException("u32 to u64");
                case FpuValueType.Doubleword: throw new InvalidOperationException("u64 to u64");
                default: throw new InvalidOperationException("cannot convert to reserved format");
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

            ulong value;

            bool isSigned;

            if (IsOperation64) {
                value = ReadGPR64(inst.Target);
                isSigned = (value & 0x4000000000000000) == 0x4000000000000000;
            }
            else {
                value = ReadGPR32(inst.Target);
                isSigned = (value & 0x80000000) == 0x80000000;
            }

            if (inst.Op.ArithmeticType == ArithmeticOp.LSHIFT)
            {
                value <<= shiftAmount;
            }
            else
            {
                // Unsigned always inserts 0s
                if (inst.IsUnsigned())
                    value &= 0xFFFFFFFFU;

                // Do a 64-bit shift
                value >>= shiftAmount;

                // Force the value to be 32-bit
                value &= 0xFFFFFFFFU;

                // Sign extend
                if (!inst.IsUnsigned() && isSigned)
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
                bool isSigned = (value & 0x4000000000000000) == 0x4000000000000000;

                value >>= shiftAmount;

                if (!inst.IsUnsigned() && isSigned)
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

                        value = Cop0.CpuRegisterRead(inst.Destination);

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

                        if (inst.IsData64()) {
                            value = ReadFPR_DW(inst.FloatSource);
                        }
                        else {
                            // 32-bit value must be sign extended
                            value = (ulong)(long)(int)ReadFPR_W(inst.FloatSource);
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

                        // FPU co-processor revision info
                        if (inst.Destination == 0) {
                            value = 0xA00U;
                        }

                        if (inst.Destination == 31) {

                            value = State.FCR.Value;

                            /* If running 64-bit mode, then sign extend */
                            if (IsOperation64)
                            {
                                value = (ulong)(int)(uint)value;
                            }
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
                        int index = inst.Destination;

                        if (index == CTS.CP0_REG_CONTEXT) {
                            value &= 0xFFFFFFFFFF800000UL;
                        }

                        if (index == CTS.CP0_REG_XCONTEXT) {
                            value &= 0xFFFFFFFE00000000UL;
                        }

                        if (index == CTS.CP0_REG_BADVADDR) {
                            break;
                        }

                        if (index == CTS.CP0_REG_LLADDR) {
                            value &= 0xFFFFFFFFU;
                        }

                        Cop0.CpuRegisterWrite(index, value);
                        break;
                    }
                case RegBoundType.Cp1:
                    {
                        if (!EnableCp1)
                        {
                            SetExceptionState(ExceptionType.Unusable);
                            return;
                        }

                        if (inst.IsData64()) {
                            WriteFPR_DW(inst.FloatSource, value);
                        }
                        else {
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

                        if (inst.Destination == 31)
                            State.FCR.Value = (uint)value;

                        break;
                    }
            }
        }

        public override void Branch(DecodedInstruction inst)
        {
            bool isLikely = inst.IsLikely();
            bool isLink = inst.IsLink();

            // The delay slot RA side effect
            if (BU_ExecuteBranchDelay ) {
                if (isLink) {
                    Writeback64(31, (ulong)(BU_TargetAddress + 4));
                }

                return;
            }

            ulong source = ReadGPR64(inst.Source);
            ulong target = ReadGPR64(inst.Target);

            BU_ConditionalPending = ComputeBranchCondition(IsOperation64, source, target, inst.Op.XferTarget, inst.Op.ArithmeticType);

            if (BU_ConditionalPending) {
                BU_TargetAddress = CoreUtils.ComputeBranchPC(IsOperation64, PC, CoreUtils.ComputeBranchTargetOffset(inst.Immediate));
            }

            if (isLink)
            {
                Writeback64(31, (ulong)(PC + 8));
            }

            #if !OPTIMAL
            Debugger.CheckBranchBreakpoints((uint)BU_TargetAddress, BU_ConditionalPending);
            #endif

            // Nullify the delay slot if likely branch not taken
            BU_NullifyNext = isLikely && !BU_ConditionalPending;

            // Always indicate the delay slot if next
            BU_DelaySlotNext = true;
        }

        public override void Jump(DecodedInstruction inst)
        {
            bool isLink = inst.IsLink();
            bool isRegister = inst.IsRegister();

            if (BU_ExecuteBranchDelay) {
                // Jump doesn't execute but there is a side-effect for the RA

                if (isLink) {
                    if (isRegister && inst.Destination != 0) {
                        Writeback64(inst.Destination, (ulong)(BU_TargetAddress + 4));
                    }
                    else {
                        Writeback64(31,  (ulong)(BU_TargetAddress + 4));
                    }
                }

                return;
            }


            BU_TargetAddress = CoreUtils.ComputeTargetPC(isRegister, PC, (long)ReadGPR64(inst.Source), inst.Inst.target);
            BU_UnconditionalPending = true;
            BU_DelaySlotNext = true;

            if (isLink)
            {
                // TODO: The recompiler will need this fix
                if (isRegister && inst.Destination != 0) {
                    Writeback64(inst.Destination,  (ulong)(PC + 8));
                }
                else {
                    Writeback64(31,  (ulong)(PC + 8));
                }
            }

            #if !OPTIMAL
            Debugger.CheckBranchBreakpoints((uint)BU_TargetAddress, true);

            if (!isRegister && (uint)BU_TargetAddress == (uint)PC && !m_WarnInfiniteJump)
            {
                Log.Warn("An unconditional infinite jump was hit: " + m_LastFetchAddress.ToString("X8"));
                m_WarnInfiniteJump = true;
            }

            #endif
        }

        public override void Store(DecodedInstruction inst)
        {
            long address = 0;
            int size = inst.DataSize();
            ExecutionFlags flags = inst.Op.Flags;
            bool LLMode = (flags & ExecutionFlags.Link) == ExecutionFlags.Link;

            if (!IsOperation64 && size == 8)
            {
                SetExceptionState(ExceptionType.Reserved);
                return;
            }

            /* Conditional store notes:
             * The LLBit must be set a Load linked operation before this type of operation
             * TODO: When ERET instruction is supported, it causes any conditional store to fail afterwards
             */

            if (LLMode && State.LLBit) return;

            if (!IsOperation64)
            {
                int baseAddress = (int)ReadGPR32(inst.Source);
                int offset = (short)inst.Immediate;
                address = baseAddress + offset;
            }
            else
            {
                long baseAddress = (long)ReadGPR64(inst.Source);
                long offset = (short)inst.Immediate;
                address = baseAddress + offset;
            }

            /* Always 32-bit address */
            address &= 0xFFFFFFFFU;

            /* Virtual To Physical */
            if (!TryVirtToPhys(false, address, out address))
            {
                // Exception raised during fetch stage
                // Return right away, let the exception handler work on the next step
                return;
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
                                uint val = PhysicalMemory.U32(address & ~3);
                                val &= CTS.SWL_MASK[index];
                                val |= ReadGPR32(inst.Target) >> CTS.SWL_SHIFT[index];
                                PhysicalMemory.U32(address & ~3, val);
                                break;
                            }

                        case 8:
                            {
                                int index = (int)((uint)address & 7);
                                ulong val = PhysicalMemory.U64(address & ~7);
                                val &= CTS.SDL_MASK[index];
                                val |= ReadGPR64(inst.Target) >> CTS.SDL_SHIFT[index];
                                PhysicalMemory.U64(address & ~7, val);
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
                                uint val = PhysicalMemory.U32(address & ~3);
                                val &= CTS.SWR_MASK[index];
                                val |= ReadGPR32(inst.Target) << CTS.SWR_SHIFT[index];
                                PhysicalMemory.U32(address & ~3, val);
                                break;
                            }

                        case 8:
                            {
                                int index = (int)((uint)address & 7);
                                ulong val = PhysicalMemory.U64(address & ~7);
                                val &= CTS.SDR_MASK[index];
                                val |= ReadGPR64(inst.Target) << CTS.SDR_SHIFT[index];
                                PhysicalMemory.U64(address & ~7, val);
                                break;
                            }
                    }
                }
                else
                {
                    switch (size)
                    {
                        default: throw new InvalidOperationException("How did this happen?");
                        case 1: PhysicalMemory.U8(address, (byte)ReadGPR64(inst.Target)); break;
                        case 2: PhysicalMemory.U16(address, (ushort)ReadGPR64(inst.Target)); break;
                        case 4: PhysicalMemory.U32(address, (uint)ReadGPR32(inst.Target)); break;
                        case 8: PhysicalMemory.U64(address, ReadGPR64(inst.Target)); break;
                    }

                    /* Store conditional */
                    if (LLMode)
                    {
                        Writeback64(inst.Target, State.LLBit ? 1U : 0U);
                    }

                    // n64_systemtest isviewer
                    #if ENABLE_ISVIEWER
                    if (size == 4 && address == MemHelper.VirtualToPhysical(0xB3FF0014)) {
                        int len = PhysicalMemory.S32(address);

                        if (len > 0) {
                            long addr = MemHelper.VirtualToPhysical(0xB3FF0020);

                            for (int i = 0; i < len; i++) {
                                char readChar = (char)PhysicalMemory.U8(addr + i);
                                if (readChar == '\0') break;
                                Console.Write(readChar);
                            }
                        }
                    }
                    #endif
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
            long address = 0;
            int size = 0;

            bool upperImm = inst.IsImmediate();
            bool loadLinked = inst.IsLink();
            bool left = inst.IsLeft();
            bool right = inst.IsRight();
            bool unsigned = inst.IsUnsigned();

            uint read32 = 0;
            ulong read64 = 0;

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
                        address = baseAddress + offset;
                    }
                    else
                    {
                        long baseAddress = (long)ReadGPR64(inst.Source);
                        long offset = (short)inst.Immediate;
                        address = baseAddress + offset;
                    }

                    /* Always 32-bit address */
                    address &= 0xFFFFFFFFU;

                    /* Virtual To Physical */
                    if (!TryVirtToPhys(false, address, out address))
                    {
                        // Exception raised during fetch stage
                        // Return right away, let the exception handler work on the next step
                        return;
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
                                    read32 = PhysicalMemory.U32(address & ~3);
                                    uint val = ReadGPR32(inst.Target);
                                    val &= CTS.LWL_MASK[index];
                                    val |= read32 << CTS.LWL_SHIFT[index];
                                    read32 = val;
                                    break;
                                }

                            case 8:
                                {
                                    int index = (int)((uint)address & 7);
                                    read64 = PhysicalMemory.U64(address & ~7);
                                    ulong val = ReadGPR64(inst.Target);
                                    val &= CTS.LDL_MASK[index];
                                    val |= read64 << CTS.LDL_SHIFT[index];
                                    read64 = val;
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
                                    read32 = PhysicalMemory.U32(address & ~3);
                                    uint val = ReadGPR32(inst.Target);
                                    val &= CTS.LWR_MASK[index];
                                    val |= read32 >> CTS.LWR_SHIFT[index];
                                    read32 = val;
                                    break;
                                }

                            case 8:
                                {
                                    int index = (int)((uint)address & 7);
                                    read64 = PhysicalMemory.U64(address & ~7);
                                    ulong val = ReadGPR64(inst.Target);
                                    val &= CTS.LDR_MASK[index];
                                    val |= read64 >> CTS.LDR_SHIFT[index];
                                    read64 = val;
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

                        if (size == 4) {
                            read32 = PhysicalMemory.U32(address);
                        }
                        else if (size == 8) {
                            read64 = PhysicalMemory.U64(address);
                        }
                    }

                    #if TESTING
                    StringBuilder sb = new StringBuilder();
                    sb.Append("LOAD ...");
                    for (int i = 0; i < 8; i++) {
                        sb.Append(PhysicalMemory.DirectReadByte(address + i).ToString("X2"));
                    }
                    sb.Append("...");
                    Console.WriteLine(sb.ToString());
                    #endif

                    if (unsigned)
                    {
                        switch (size)
                        {
                            default: throw new InvalidOperationException("How did this happen (unsigned)?");
                            case 1: Writeback64(inst.Target, PhysicalMemory.U8(address)); break;
                            case 2: Writeback64(inst.Target, PhysicalMemory.U16(address)); break;
                            case 4: Writeback64(inst.Target, read32); break;
                            case 8: Writeback64(inst.Target, read64); break;
                        }
                    }
                    else
                    {
                        switch (size)
                        {
                            default: throw new InvalidOperationException("How did this happen?");
                            case 1: Writeback64(inst.Target, (ulong)(sbyte)PhysicalMemory.U8(address)); break;
                            case 2: Writeback64(inst.Target, (ulong)(short)PhysicalMemory.U16(address)); break;
                            case 4: Writeback64(inst.Target, (ulong)(int)read32); break;
                            case 8: Writeback64(inst.Target, (ulong)(long)read64); break;
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
                var address = baseAddress + offset;

                /* Always 32-bit address */
                address &= 0xFFFFFFFFU;

                /* Virtual To Physical */
                if (!TryVirtToPhys(false, address, out address))
                {
                    // Exception raised during fetch stage
                    // Return right away, let the exception handler work on the next step
                    return;
                }

                switch (size)
                {
                    default: throw new InvalidOperationException("Unsupported FPU Load Size: " + size.ToString());
                    case 4: WriteFPR_W(inst.FloatTarget, PhysicalMemory.U32(address)); break;
                    case 8: WriteFPR_DW(inst.FloatTarget, PhysicalMemory.U64(address)); break;
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
            long address = baseAddress + offset;

            /* Always 32-bit address */
            address &= 0xFFFFFFFFU;

            /* Virtual To Physical */
            if (!TryVirtToPhys(false, address, out address))
            {
                // Exception raised during fetch stage
                // Return right away, let the exception handler work on the next step
                return;
            }

            try
            {
                switch (size)
                {
                    default: throw new InvalidOperationException("Unsupported FPU Store Size: " + size.ToString());
                    case 4: PhysicalMemory.U32(address, ReadFPR_W(inst.FloatTarget)); break;
                    case 8: PhysicalMemory.U64(address, ReadFPR_DW(inst.FloatTarget)); break;
                }
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
            if (inst.Format == FpuValueType.FDouble || inst.Format == FpuValueType.FSingle)
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
            double value;

            if (inst.Format == FpuValueType.FDouble) {
                value = ReadFPR_D(inst.FloatSource);
            }
            else if (inst.Format == FpuValueType.FSingle) {
                value = ReadFPR_S(inst.FloatSource);
            }
            else {
                SetExceptionState(FpuExceptionFlags.Unimplemented);
                return;
            }

            if (inst.Op.Flags == ExecutionFlags.Data32)
            {
                WriteFPR_DW(inst.FloatDest, 0);
                WriteFPR_W(inst.FloatDest, (uint)(float)Math.Truncate(value));
            }
            else if (inst.Op.Flags == ExecutionFlags.Data64)
            {
                WriteFPR_DW(inst.FloatDest, (ulong)(double)Math.Truncate(value));
            }
            else
            {
                SetExceptionState(FpuExceptionFlags.Unimplemented);
                return;
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
            decimal fprValue;

            if (inst.Format == FpuValueType.FDouble) fprValue = new decimal(ReadFPR_D(inst.FloatSource));
            else if (inst.Format == FpuValueType.FSingle) fprValue = new decimal(ReadFPR_S(inst.FloatSource));
            else {
                SetExceptionState(FpuExceptionFlags.Unimplemented);
                return;
            }

            if (inst.Op.Flags == ExecutionFlags.Data32)
            {
                WriteFPR_DW(inst.FloatDest, 0);
                WriteFPR_W(inst.FloatDest, (uint)(float)Math.Floor(fprValue));
            }
            else if (inst.Op.Flags == ExecutionFlags.Data64)
            {
                WriteFPR_DW(inst.FloatDest, (ulong)(double)Math.Floor(fprValue));
            }
            else
            {
                SetExceptionState(FpuExceptionFlags.Unimplemented);
                return;
            }
        }

        public override void Convert(DecodedInstruction inst)
        {
            // Console.WriteLine("Convert To {6}: S:{0} T:{1} D:{2} FS:{3} FT:{4} FD:{5}", inst.Source, inst.Target, inst.Destination, inst.FloatSource, inst.FloatTarget, inst.FloatDest, inst.Format.ToString());

            if (inst.Op.Flags == ExecutionFlags.DataS) {
                ConvertToSingle(inst.Format, inst.FloatSource, inst.FloatDest);
            }
            else if (inst.Op.Flags == ExecutionFlags.DataD) {
                ConvertToDouble(inst.Format, inst.FloatSource, inst.FloatDest);
            }
            else if (inst.Op.Flags == ExecutionFlags.Data32) {
                ConvertToFixed32(inst.Format, inst.FloatSource, inst.FloatDest);
            }
            else if (inst.Op.Flags == ExecutionFlags.Data64) {
                ConvertToFixed64(inst.Format, inst.FloatSource, inst.FloatDest);
            }
            else {
                SetExceptionState(FpuExceptionFlags.Unimplemented);
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
                Cop0.TrapException();
            }
        }

        public override void Break(DecodedInstruction inst) {
            SetBreakException();
        }

        public override void TLBProbe(DecodedInstruction inst) {
            TLB.Probe();
        }

        public override void TLBWrite(DecodedInstruction inst) {
            TLB.Write(inst.Op.Flags.TestFlag(ExecutionFlags.Random));
        }

        public override void TLBRead(DecodedInstruction inst) {
            TLB.Read();
        }

        public override void Syscall(DecodedInstruction inst)
        {
            SetSyscallException();
        }

        public bool InfiniteLoopWarn => m_WarnInfiniteJump;
    }
}
