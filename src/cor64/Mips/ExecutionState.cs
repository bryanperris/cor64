using cor64.IO;
using cor64.Mips.R4300I;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace cor64.Mips
{
    public unsafe class ExecutionState
    {
        const int SIZE_GPR = 64 * 32;
        const int SIZE_FPR = 32 * 2 * 4;
        const int SIZE_HILO = 62 * 2;
        const int SIZE_LLBIT = 1;
        const int STACK_POINTER = 29;

        public FloatControlRegister FCR { get; } = new FloatControlRegister();
        public ControlRegisters Cp0 { get; } = new ControlRegisters();

        private PinnedBuffer m_RegMem;
        private ulong* m_GprRegs64;
        private uint* m_GprRegs32;
        private double* m_FprRegsDouble;
        private float* m_FprRegsSingle;
        private ulong* m_FprRegsDword;
        private uint* m_FprRegsWord;
        private ulong* m_Hi;
        private ulong* m_Lo;
        private bool* m_LLbit;

        private CoreDebugger m_CoreDbg;
        private StackMonitor m_StackMonitor = new StackMonitor();

        public ExecutionState()
        {
            m_RegMem = new PinnedBuffer(SIZE_GPR + SIZE_FPR + SIZE_HILO);
            m_RegMem.Clear();

            IntPtr ptr = m_RegMem.GetPointer();

            m_GprRegs64 = (ulong*)ptr;
            m_GprRegs32 = (uint*)ptr;

            ptr += SIZE_GPR;

            m_FprRegsDouble = (double*)ptr;
            m_FprRegsSingle = (float*)ptr;
            m_FprRegsWord = (uint*)ptr;
            m_FprRegsDword = (ulong*)ptr;

            ptr += SIZE_FPR;

            m_Hi = (ulong*)ptr;
            ptr += 8;
            m_Lo = (ulong*)ptr;
            ptr += 8;

            m_LLbit = (bool*)ptr;
        }

        public void SetCoreDebugger(CoreDebugger debugger)
        {
            this.m_CoreDbg = debugger;
        }

        public IntPtr GetGprRef()
        {
            return (IntPtr)m_GprRegs64;
        }

        [Conditional("DEBUG")]
        private void StackCheck(int i, ulong value)
        {
            if (i == STACK_POINTER)
            {
                m_StackMonitor.PointerUpdate(value);
            }
        }

        [Conditional("DEBUG")]
        [DebuggerStepThrough]
        private void Check(int i)
        {
            if (i < 0)
            {
                throw new IndexOutOfRangeException();
            }

            if (i >= 32)
            {
                throw new IndexOutOfRangeException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong GetGpr64(int i)
        {
            Check(i);

            if (m_CoreDbg != null)
            {
                m_CoreDbg.TestForGprBreakpoint(i, false);
            }

            return m_GprRegs64[i];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint GetGpr32(int i)
        {
            Check(i);

            if (m_CoreDbg != null)
            {
                m_CoreDbg.TestForGprBreakpoint(i, false);
            }

            return m_GprRegs32[i << 1];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetGpr64(int i, ulong value)
        {
            if (i == 0) return;

            Check(i);

            m_GprRegs64[i] = value;

            StackCheck(i, value);

            if (m_CoreDbg != null)
            {
                m_CoreDbg.TestForGprBreakpoint(i, true);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetGpr32(int i, uint value)
        {
            if (i == 0) return;

            Check(i);

            m_GprRegs32[i << 1] = value;

            StackCheck(i, value);

            if (m_CoreDbg != null)
            {
                m_CoreDbg.TestForGprBreakpoint(i, true);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong GetHi()
        {
            return m_Hi[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetHi(ulong value)
        {
            m_Hi[0] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong GetLo()
        {
            return m_Lo[0];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetLo(ulong value)
        {
            m_Lo[0] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public double GetFprD(int i)
        {
            Check(i);

            return m_FprRegsDouble[i];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFprD(int i, double value)
        {
            Check(i);

            m_FprRegsDouble[i] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong GetFprDW(int i)
        {
            Check(i);

            return m_FprRegsDword[i];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFprDW(int i, ulong value)
        {
            Check(i);

            m_FprRegsDword[i] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public float GetFprS(int i)
        {
            Check(i);

            return m_FprRegsSingle[i << 1];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFprS(int i, float value)
        {
            Check(i);

            m_FprRegsSingle[i << 1] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint GetFprW(int i)
        {
            Check(i);

            return m_FprRegsWord[i << 1];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetFprW(int i, uint value)
        {
            Check(i);

            m_FprRegsWord[i << 1] = value;
        }

        public bool LLBit
        {
            get => m_LLbit[0];
            set => m_LLbit[0] = value;
        }

        public StackMonitor Stack => m_StackMonitor;
    }
}
