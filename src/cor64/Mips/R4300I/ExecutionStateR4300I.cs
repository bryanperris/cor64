using System;
using System.Runtime.CompilerServices;

namespace cor64.Mips.R4300I {
    public sealed class ExecutionStateR4300I : ExecutionState {
        // TODO: Move Cop0 state here too
        // TODO: Move FPU control here

        private readonly FpuState m_FpuState;

        public ExecutionStateR4300I(ControlRegisters cop0Regs) : base() {
            m_FpuState = new FpuState(cop0Regs.Status);
        }

        public FpuState FPR => m_FpuState;
    }
}