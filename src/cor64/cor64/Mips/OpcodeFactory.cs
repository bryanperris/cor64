using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace cor64.Mips
{
    public static class OpcodeFactory
    {
        private static int g_NextId = 0;

        public static Opcode Define(OperationFamily family, String op, OperandType operandFmt)
        {
            return new Opcode(op, 0, operandFmt, family, 0, 0, 0, g_NextId++);
        }

        public static Opcode Define(OperationFamily family, String op, OperandType operandFmt, ArithmeticOp arithmeticOp)
        {
            return new Opcode(op, 0, operandFmt, family, arithmeticOp, 0, 0, g_NextId++);
        }

        public static Opcode Define(OperationFamily family, String op, OperandType operandFmt, ExecutionFlags flags)
        {
            return new Opcode(op, flags, operandFmt, family, 0, 0, 0, g_NextId++);
        }

        public static Opcode Define(OperationFamily family, String op, OperandType operandFmt, ArithmeticOp arithmeticOp, ExecutionFlags flags)
        {
            return new Opcode(op, flags, operandFmt, family, arithmeticOp, 0, 0, g_NextId++);
        }

        public static Opcode Define(OperationFamily family, String op, OperandType operandFmt, ArithmeticOp arithmeticOp, ExecutionFlags flags, RegBoundType target)
        {
            return new Opcode(op, flags, operandFmt, family, arithmeticOp, 0, 0, g_NextId++);
        }

        public static Opcode Define(OperationFamily family, String op, OperandType operandFmt, RegBoundType source, RegBoundType target, ExecutionFlags executionFlags = ExecutionFlags.None)
        {
            return new Opcode(op, executionFlags, operandFmt, family, 0, source, target, g_NextId++);
        }

        public static Opcode Define(OperationFamily tableFamily)
        {
            return new Opcode(null, 0, 0, tableFamily, 0, 0, 0, 0);
        }

        public static Opcode DefineNull()
        {
            return new Opcode(null, 0, 0, OperationFamily.Null, 0, 0, 0, 0);
        }

        public static int LastID => g_NextId - 1;
    }

    public enum OperationFamily
    {
        Null,   // Unimplemented type of opcode
        Sub,    // jump to another opcode table
        Branch, // Branches and jumps
        BranchFpu, // Branch based on FPU compare
        Load,   // Load values from memory
        Store,  // Store values to memory
        Reg,    // Register based operations (no memory access)
        Cop0,   // Cop0 register operations
        Fpu,    // FPU operations,
        Tlb,    // TLB cache operations
        Trap,   // Exceptions, Traps, break, syscall
        Synchronize,   // Sync operation (atomic)
        Cache,  // Cache operation
        LoadFpu, // Load values from memory to FPU
        StoreFpu, // Store values to memory from FPU
        TBL_SPECIAL,
        TBL_REGIMM,
        TBL_COP0,
        TBL_COP1,
        TBL_BC1,
        TBL_FPU
    }
}
