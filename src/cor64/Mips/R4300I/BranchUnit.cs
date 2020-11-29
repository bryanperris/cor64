using System;

namespace cor64.Mips.R4300I {

    public class BranchUnit {
        private class State {
            public bool take;
            public bool delaySlot;
            public bool unconditional;
            public ulong target;
            public bool nullify;

            public void Reset() {
                take = false;
                delaySlot = false;
                unconditional = false;
                target = 0;
                nullify = false;
            }
        }

        private readonly State m_InterruptState = new State();
        private readonly State m_NormalState = new State();

        private State m_ActiveState;

        public BranchUnit() {
            m_ActiveState = m_NormalState;
        }

        public void SwitchInterrupt() {
            m_ActiveState = m_InterruptState;
            m_ActiveState.Reset();
        }

        public void SwitchNormal() {
            m_ActiveState = m_NormalState;
        }

        public void ResetAll() {
            m_InterruptState.Reset();
            m_NormalState.Reset();
        }

        public bool Take {
            get => m_ActiveState.take;
            set => m_ActiveState.take = value;
        }

        public bool DelaySlot {
            get => m_ActiveState.delaySlot;
            set => m_ActiveState.delaySlot = value;
        }

        public bool Unconditonal {
            get => m_ActiveState.unconditional;
            set => m_ActiveState.unconditional = value;
        }

        public ulong Target {
            get => m_ActiveState.target;
            set => m_ActiveState.target = value;
        }

        public bool NullifyNext {
            get => m_ActiveState.nullify;
            set => m_ActiveState.nullify = value;
        }
    }
}