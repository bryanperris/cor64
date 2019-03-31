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
        private FpuRegisters m_FpuRegisters;
        private FloatControlRegister m_FpuCtrl;
        private ulong m_LoRegister;
        private ulong m_HiRegister;
        private bool m_LLBit;

        /* TODO: Cop0 could be here too */

        public ExecutionState()
        {
            m_GeneralRegisters = new ulong[32];
            m_FpuRegisters = new FpuRegisters();
            m_FpuCtrl = new FloatControlRegister();
            GPR_32 = new GPR32Accessor(this);
            GPR_64 = new GPR64Accessor(this);
        }

        public GPR32Accessor GPR_32 { get; }

        public GPR64Accessor GPR_64 { get; }

        public FpuRegisters FPR => m_FpuRegisters;

        /* This is really FCR31 */
        public FloatControlRegister FCR => m_FpuCtrl;

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
    }
}
