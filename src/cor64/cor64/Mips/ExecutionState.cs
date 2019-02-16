using cor64.IO;
using cor64.Mips.R4300I;
using cor64.Mips.R4300I.CP0;
using System;
using System.Globalization;

namespace cor64.Mips
{
    public class ExecutionState
    {
        /* Mips Register Files */
        private ulong[] m_GeneralRegisters;
        private PinnedBuffer m_FpuRegisters;
        private PinnedBuffer m_FcrRegisters; // FPU Control registers
        private ulong m_LoRegister;
        private ulong m_HiRegister;
        private bool m_LLBit;

        /* TODO: Cop0 and Cop1 states could be here too */

        public ExecutionState()
        {
            m_GeneralRegisters = new ulong[32];
            m_FpuRegisters = new PinnedBuffer(8 * 32);
            m_FcrRegisters = new PinnedBuffer(4 * 32);
            GPR_32 = new GPR32Accessor(this);
            GPR_64 = new GPR64Accessor(this);
            FPR_32 = new FPRAccessor_32(this);
            FPR_64 = new FPRAccessor_64(this);
            FPR_FLOAT = new FPRAccessor_Float(this);
            FPR_DOUBLE = new FPRAccessor_Double(this);
        }

        public GPR32Accessor GPR_32 { get; }

        public GPR64Accessor GPR_64 { get; }

        public FPRAccessor_32 FPR_32 { get; }

        public FPRAccessor_64 FPR_64 { get; }

        public FPRAccessor_Float FPR_FLOAT { get; }

        public FPRAccessor_Double FPR_DOUBLE { get; }

        public ulong Lo
        {
            get => m_LoRegister;
            set => m_LoRegister = value;
        }

        public ulong Hi
        {
            get => m_HiRegister;
            set => m_HiRegister = value;
        }

        public bool LLBit
        {
            get => m_LLBit;
            set => m_LLBit = value;
        }

        public abstract class Accessor<T>
        {
            internal protected Accessor(ExecutionState state)
            {
                State = state;
            }

            protected ExecutionState State { get; }

            public abstract T this[int index] { get; set; }
        }

        public sealed class GPR64Accessor : Accessor<ulong>
        {
            public GPR64Accessor(ExecutionState state) : base(state)
            {
            }

            public override ulong this[int index]
            {
                get => State.m_GeneralRegisters[index];
                set
                {
                    if (index > 0)
                    {
                        State.m_GeneralRegisters[index] = value;
                    }
                }
            }

            public ulong[] GetRef()
            {
                return State.m_GeneralRegisters;
            }
        }

        public sealed class GPR32Accessor : Accessor<uint>
        {
            public GPR32Accessor(ExecutionState state) : base(state)
            {
            }

            public override uint this[int index]
            {
                get => (uint)State.m_GeneralRegisters[index];
                set
                {
                    if (index > 0)
                    {
                        State.m_GeneralRegisters[index] = (uint)value;
                    }
                }
            }
        }

        public unsafe sealed class FPRAccessor_Float : Accessor<float>
        {
            private float* m_Ptr;

            public FPRAccessor_Float(ExecutionState state) : base(state)
            {
                m_Ptr = (float*)state.m_FpuRegisters.GetPointer();
            }

            public override float this[int index]
            {
                get => *(m_Ptr + index);
                set => *(m_Ptr + index) = value;
            }
        }

        public unsafe sealed class FPRAccessor_Double : Accessor<double>
        {
            private double* m_Ptr;

            public FPRAccessor_Double(ExecutionState state) : base(state)
            {
                m_Ptr = (double*)state.m_FpuRegisters.GetPointer();
            }

            public override double this[int index]
            {
                get => *(m_Ptr + index);
                set => *(m_Ptr + index) = value;
            }
        }

        public unsafe sealed class FPRAccessor_32 : Accessor<uint>
        {
            private uint* m_Ptr;

            public FPRAccessor_32(ExecutionState state) : base(state)
            {
                m_Ptr = (uint*)state.m_FpuRegisters.GetPointer();
            }

            public override uint this[int index]
            {
                get => *(m_Ptr + index);
                set => *(m_Ptr + index) = value;
            }
        }

        public unsafe sealed class FPRAccessor_64 : Accessor<ulong>
        {
            private ulong* m_Ptr;

            public FPRAccessor_64(ExecutionState state) : base(state)
            {
                m_Ptr = (ulong*)state.m_FpuRegisters.GetPointer();
            }

            public override ulong this[int index]
            {
                get => *(m_Ptr + index);
                set => *(m_Ptr + index) = value;
            }
        }
    }
}
