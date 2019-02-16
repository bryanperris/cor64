using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using cor64.Mips.R4300I.CP0;
using cor64.IO;
using cor64.Debugging;

namespace cor64.Mips.R4300I
{
    public class SimpleInterpreter : Interpreter
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();


        public SimpleInterpreter(bool debug) :
            base(new Disassembler("o32", debug ? BaseDisassembler.Mode.Debug : BaseDisassembler.Mode.Fast))
        {
        }

        private static long L(ulong v)
        {
            return (long)v;
        }

        private bool IsReserved(DecodedInstruction inst)
        {
            return
                (((inst.Op.Flags & ExecutionFlags.Reserved32) == ExecutionFlags.Reserved32) && !IsOperation64) ||
                (((inst.Op.Flags & ExecutionFlags.Reserved64) == ExecutionFlags.Reserved64) && IsOperation64);
        }

        public override string Description => "The SIMPLE MIPS Interpreter";

        protected override bool Execute()
        {
            /* Step clock */
            CoreClock.NextTick();

            /* Step coprocessor 0 */
            Cop0.ProcessorTick();

            return base.Execute();
        }

        protected override void Add32(DecodedInstruction inst)
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

        protected override void BitwiseLogic(DecodedInstruction inst)
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

        protected override void Add64(DecodedInstruction inst)
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

        protected override void Divide32(DecodedInstruction inst)
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
                    unchecked
                    {
                        result = operandA / operandB;
                        remainder = operandA % operandB;
                    }
                }
                else
                {
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
            catch (DivideByZeroException)
            {
                return;
            }
        }

        protected override void Divide64(DecodedInstruction inst)
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
                    unchecked
                    {
                        result = operandA / operandB;
                        remainder = operandA % operandB;
                    }
                }
                else
                {
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
            catch (DivideByZeroException)
            {
                return;
            }
        }

        protected override void Multiply32(DecodedInstruction inst)
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

        protected override void Multiply64(DecodedInstruction inst)
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

        protected override void Shift32(DecodedInstruction inst)
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

        protected override void Shift64(DecodedInstruction inst)
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

        protected override void Subtract32(DecodedInstruction inst)
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

        protected override void Subtract64(DecodedInstruction inst)
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

        protected override void SetOnLessThan(DecodedInstruction inst)
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
                operandB = (ulong)(int)inst.Immediate;
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

        protected override void TransferReg(DecodedInstruction inst)
        {
            ulong value = 0;

            /* Op Notes --
             * Hi/Lo - so far we always treat them as double-words, makes sense
             * Control Move - Has to do with a specific GPU register
             * Value Move - Copy a register value between GPR and a selceted coprocess (Each COP processes the value different)
             */

            /* Source value to copy */
            switch (inst.Op.XferSource)
            {
                case RegBoundType.Hi: value = State.Hi; break;
                case RegBoundType.Lo: value = State.Lo; break;
                case RegBoundType.Gpr: value = ReadGPR64(inst.Source); break;
                case RegBoundType.Cp0:
                    {
                        if (!EnableCp0)
                        {
                            SetExceptionState(ExceptionType.Unusable);
                            return;
                        }

                        value = Cp0Regs.Read(inst.Destination);

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

                        // TODO: Read from FPU

                        /* If running 64-bit mode, with the 32-bit version, then sign extend */
                        if (IsOperation64 && inst.IsData32())
                        {
                            value = (ulong)(int)(uint)value;
                        }

                        throw new NotImplementedException();
                    }

                case RegBoundType.Cp1Ctl:
                    {
                        if (!EnableCp1)
                        {
                            SetExceptionState(ExceptionType.Unusable);
                            return;
                        }

                        // TODO: Read from FPU control reg, this opcode is always 32-bit

                        /* If running 64-bit mode, then sign extend */
                        if (IsOperation64)
                        {
                            value = (ulong)(int)(uint)value;
                        }

                        throw new NotImplementedException();
                    }
            }

            /* Target */
            switch (inst.Op.XferTarget)
            {
                case RegBoundType.Gpr:
                    {
                        Writeback64(inst.Destination, IsOperation64 ? value : (uint)value);
                        break;
                    }

                case RegBoundType.Hi:
                    {
                        State.Hi = IsOperation64 ? value : (uint)value;
                        break;
                    }

                case RegBoundType.Lo:
                    {
                        State.Lo = IsOperation64 ? value : (uint)value;
                        break;
                    }
                case RegBoundType.Cp0:
                    {
                        Cp0Regs.WriteFromGpr(inst.Destination, value);
                        break;
                    }
                case RegBoundType.Cp1:
                    {
                        // TODO: Write to FPU
                        throw new NotImplementedException();
                    }
                case RegBoundType.Cp1Ctl:
                    {
                        // TODO: Write to FPU control reg
                        throw new NotImplementedException();
                    }
            }
        }

        protected override void Branch(DecodedInstruction inst)
        {
            bool isLikely = inst.IsLikely();
            bool isLink = inst.IsLink();
            TargetAddress = CoreUtils.ComputeBranchPC(IsOperation64, m_Pc, CoreUtils.ComputeBranchTargetOffset(inst.Immediate));

            TakeBranch = CoreUtils.ComputeBranchCondition(
                IsOperation64,
                State.GPR_64[inst.Source],
                State.GPR_64[inst.Target],
                EnableCp1, 
                inst.Op.XferTarget, 
                inst.Op.ArithmeticType);


            if (isLink)
            {
                Writeback64(31, m_Pc + 8);
            }

            BranchDelay = !isLikely || (TakeBranch && isLikely);

            /* Clear target if not taken */
            if (!TakeBranch)
            {
                TargetAddress = 0;
            }
        }

        protected override void Jump(DecodedInstruction inst)
        {
            bool isLink = inst.IsLink();
            bool isRegister = inst.IsRegister();
            TargetAddress = CoreUtils.ComputeTargetPC(IsOperation64, isRegister, m_Pc, State.GPR_64[inst.Source], inst.Inst.target);
            UnconditionalJump = true;

            if (isLink)
            {
                Writeback64(31, m_Pc + 8);
            }

            if (!isRegister && (uint)TargetAddress == (uint)m_Pc)
            {
                throw new InvalidOperationException("An unconditional infinite jump was hit");
            }
        }

        protected override void Store(DecodedInstruction inst)
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
                case 2: m_DataMemory.Data16Swp = (ushort)ReadGPR64(inst.Target); break;
                case 4: m_DataMemory.Data32Swp = (uint)ReadGPR32(inst.Target); break;
                case 8: m_DataMemory.Data64Swp = ReadGPR64(inst.Target); break;
            }

            try
            {
                if (inst.IsLeft())
                {
                    int numOff = (int)(address % (uint)size);
                    int numBytes = size - numOff;
                    long addr = (((long)address - numOff) + (size - 1)) - numOff;
                    int index = size - numOff - 1;
                    byte[] data = m_DataMemory.ReadBuffer();

                    for (int i = 0; i < numBytes; i++)
                    {
                        m_DataMemory.Data8 = data[index - i];
                        m_DataMemory.WriteData(addr - i, 1, true);
                    }
                }
                else if (inst.IsRight())
                {
                    int numOff = (int)(address % (uint)size);
                    int numBytes = 5 - (size - numOff);
                    byte[] data = m_DataMemory.ReadBuffer();

                    for (int i = 0; i < numBytes; i++)
                    {
                        m_DataMemory.Data8 = data[3 - i];
                        m_DataMemory.WriteData((long)address - i, 1, true);
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
                        m_DataMemory.WriteData((long)address, size, true);

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

        protected override void Load(DecodedInstruction inst)
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

                    if (left)
                    {
                        int numOff = (int)(address % (uint)size);
                        int numBytes = size - numOff;
                        uint val = ReadGPR32(inst.Target);

                        for (int i = 0; i < numBytes; i++)
                        {
                            int leftShift = 8 * (numOff + i);
                            uint mask = 0xFFU << leftShift;
                            m_DataMemory.ReadData((long)address + i, 1, true);
                            val &= ~mask;
                            val |= ((uint)m_DataMemory.Data8 << leftShift);
                        }

                        m_DataMemory.Data32 = val;
                    }
                    else if (right)
                    {
                        int numOff = (int)(address % (uint)size);
                        int numBytes = 5 - (size - numOff);
                        uint val = ReadGPR32(inst.Target);

                        for (int i = 0; i < numBytes; i++)
                        {
                            int shift = 8 * (i + numOff);
                            uint mask = 0xFFU << shift;
                            m_DataMemory.ReadData((long)address - i, 1, true);
                            val &= ~mask;
                            val |= ((uint)m_DataMemory.Data8 << shift);
                        }

                        m_DataMemory.Data32Swp = val;
                    }
                    else
                    {
                        if (loadLinked)
                        {
                            State.LLBit = true;
                        }

                        m_DataMemory.ReadData((long)address, size, true);
                    }

                    if (unsigned)
                    {
                        switch (size)
                        {
                            default: throw new InvalidOperationException("How did this happen (unsigned)?");
                            case 1: Writeback64(inst.Target, m_DataMemory.Data8); break;
                            case 2: Writeback64(inst.Target, m_DataMemory.Data16Swp); break;
                            case 4: Writeback64(inst.Target, m_DataMemory.Data32Swp); break;
                            case 8: Writeback64(inst.Target, m_DataMemory.Data64Swp); break;
                        }
                    }
                    else
                    {
                        switch (size)
                        {
                            default: throw new InvalidOperationException("How did this happen?");
                            case 1: Writeback64(inst.Target, (ulong)(sbyte)m_DataMemory.Data8); break;
                            case 2: Writeback64(inst.Target, (ulong)(short)m_DataMemory.Data16Swp); break;
                            case 4: Writeback64(inst.Target, (ulong)(int)m_DataMemory.Data32Swp); break;
                            case 8: Writeback64(inst.Target, (ulong)(long)m_DataMemory.Data64Swp); break;
                        }
                    }
                }
            }
            catch (MipsException e)
            {
                SetExceptionState(e.Exception);
            }
        }

        protected override void Cache(DecodedInstruction inst)
        {


        }

        protected override void Sync(DecodedInstruction inst)
        {
            
        }
    }
}
