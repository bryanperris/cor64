using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OPCALL = System.Action<cor64.Mips.DecodedInstruction>;

namespace cor64.Mips
{
    public abstract class OpcodeFactory
    {
        private static int CurrentID { get; set; }

        public static int Count { get; protected set; }


        public static Opcode Define(OperationFamily family, String op, OperandType operandFmt)
        {
            Count++;
            return new Opcode(op, 0, operandFmt, family, 0, 0, 0, 0, CurrentID++);
        }

        public static Opcode Define(OperationFamily family, String op, OperandType operandFmt, VectorOpFlags vuFlags)
        {
            Count++;
            return new Opcode(op, 0, operandFmt, family, 0, 0, 0, vuFlags, CurrentID++);
        }

        public static Opcode Define(OperationFamily family, String op, OperandType operandFmt, ExecutionFlags executionFlags, VectorOpFlags vuFlags)
        {
            Count++;
            return new Opcode(op, executionFlags, operandFmt, family, 0, 0, 0, vuFlags, CurrentID++);
        }

        public static Opcode Define(OperationFamily family, String op, OperandType operandFmt, ArithmeticOp arithmeticOp)
        {
            Count++;
            return new Opcode(op, 0, operandFmt, family, arithmeticOp, 0, 0, 0, CurrentID++);
        }

        public static Opcode Define(OperationFamily family, String op, OperandType operandFmt, ExecutionFlags flags)
        {
            Count++;
            return new Opcode(op, flags, operandFmt, family, 0, 0, 0, 0, CurrentID++);
        }

        public static Opcode Define(OperationFamily family, String op, OperandType operandFmt, ArithmeticOp arithmeticOp, ExecutionFlags flags)
        {
            Count++;
            return new Opcode(op, flags, operandFmt, family, arithmeticOp, 0, 0, 0, CurrentID++);
        }

        public static Opcode Define(OperationFamily family, String op, OperandType operandFmt, ArithmeticOp arithmeticOp, ExecutionFlags flags, RegBoundType target)
        {
            Count++;
            return new Opcode(op, flags, operandFmt, family, arithmeticOp, 0, target, 0, CurrentID++);
        }

        public static Opcode Define(OperationFamily family, String op, OperandType operandFmt, RegBoundType source, RegBoundType target, ExecutionFlags executionFlags = ExecutionFlags.None)
        {
            Count++;
            return new Opcode(op, executionFlags, operandFmt, family, 0, source, target, 0, CurrentID++);
        }

        public static Opcode Define(OperationFamily tableFamily)
        {
            Count++;
            return new Opcode(null, 0, 0, tableFamily, 0, 0, 0, 0, 0);
        }

        public static Opcode DefineNull()
        {
            Count++;
            return new Opcode(null, 0, 0, OperationFamily.Null, 0, 0, 0, 0, 0);
        }

        public static CallTable CreateCallTable() {
            return new CallTable();
        }

        public class CallTable {
            private OPCALL[] m_Table;
            private Dictionary<int, OPCALL> m_MappedEntries = new Dictionary<int, OPCALL>();

            public CallTable() {
            }

            public CallTable Map(Action<DecodedInstruction> instAction, params Opcode[] opcodes)
            {
                foreach (var op in opcodes)
                {   
                    m_MappedEntries.Add(op.ID, instAction);
                }

                return this;
            }

            public void Finish() {
                m_Table = new OPCALL[Count];
                
                foreach (var x in m_MappedEntries) {
                    m_Table[x.Key] = x.Value;
                }

                m_MappedEntries.Clear();
            }

            public Action<DecodedInstruction> this[DecodedInstruction instruction] {
                get {
                    return m_Table[instruction.Op.ID];
                }
            }
        }
    }

    public enum OperationFamily
    {
        Null,   // Unimplemented type of opcode
        Branch, // Branches and jumps
        Load,   // Load values from memory
        Store,  // Store values to memory
        Reg,    // Register based operations (no memory access)
        Cop0,   // Cop0 register operations
        VU,     // Vector Unit
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
        TBL_COP2,
        TBL_BC1,
        TBL_FPU,
        TBL_VU,
        TBL_VU_LOAD,
        TBL_VU_STORE
    }
}
