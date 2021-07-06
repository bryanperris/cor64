using cor64.IO;
using cor64.Mips.R4300I;
using cor64.Mips.R4300I.TLB;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace cor64.Mips
{
    public unsafe class ExecutionState
    {
        private const int SIZE_GPR = 64 * 32;
        private const int SIZE_HILO = 62 * 2;

        public FloatControlRegister FCR { get; } = new FloatControlRegister();

        private readonly UnmanagedBuffer m_RegMem;
        private readonly ulong* m_GprRegs64;
        private readonly uint* m_GprRegs32;
        private readonly ulong* m_Hi;
        private readonly ulong* m_Lo;
        private readonly bool* m_LLbit;

        public ExecutionState()
        {
            m_RegMem = new UnmanagedBuffer(SIZE_GPR + SIZE_HILO);
            m_RegMem.Clear();

            IntPtr ptr = m_RegMem.GetPointer();

            m_GprRegs64 = (ulong*)ptr;
            m_GprRegs32 = (uint*)ptr;

            ptr += SIZE_GPR;

            m_Hi = (ulong*)ptr;
            ptr += 8;
            m_Lo = (ulong*)ptr;
            ptr += 8;

            m_LLbit = (bool*)ptr;
        }

        public IntPtr GetGprRef()
        {
            return (IntPtr)m_GprRegs64;
        }

        [Conditional("VERIFICATION")]
        [DebuggerStepThrough]
        private static void Check(int i)
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

            return m_GprRegs64[i];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public uint GetGpr32(int i)
        {
            Check(i);

            return m_GprRegs32[i << 1];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetGpr64(int i, ulong value)
        {
            if (i == 0) return;

            Check(i);

            m_GprRegs64[i] = value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetGpr32(int i, uint value)
        {
            if (i == 0) return;

            Check(i);

            m_GprRegs32[i << 1] = value;
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

        public bool LLBit
        {
            get => m_LLbit[0];
            set => m_LLbit[0] = value;
        }
    }
}
