using System;
using cor64.IO;

/* FPR Notes
-------------
There are 32 floating-point registers
FR Mode = 1: Each FPR is 64-bit wide, doubles/singles fit in nicely
FR Mode = 0: Each FPR is 32-bit wide, doubles use 2 FPRs where least falls into an even reg

Half Mode Notes:
    * All singles in 32-bit mode should access only even FPRs as to any FPR
    * Doubles are laid out as such: [E: Least 32bits][O: Most 32bits]
*/

/* 64-bit LE layout
   |        FPR0        ||        FPR1        |
   | 32-bits || 32-bits || 32-bits || 32-bits |
   |        64-bits     ||        64-bits     |
*/

/* 32-bit LE layout
   |        FPR0        ||        FPR1        ||        FPR2        ||        FPR3        |
   | 32-bits || ------- || 32-bits || ------- || 32-bits || ------- || 32-bits || ------- |
   |                  64-bits                 ||                  64-bits                 |
                         *                                           *
*/

namespace cor64.Mips.R4300I {
    public unsafe class FpuState {
        private const int SIZE = 32 * 8;
        private readonly UnmanagedBuffer m_Buffer = new UnmanagedBuffer(SIZE);
        private readonly StatusRegister m_Status;
        private readonly double* m_RegsDouble;
        private readonly float* m_RegsSingle;
        private readonly ulong* m_RegsDword;
        private readonly uint* m_RegsWord;

        private bool FRMode => m_Status.FRMode;

        public FpuState(StatusRegister sr) {
            m_Status = sr;
            m_RegsDouble = (double *)m_Buffer.GetPointer();
            m_RegsSingle = (float *)m_Buffer.GetPointer();
            m_RegsDword = (ulong *)m_Buffer.GetPointer();
            m_RegsWord = (uint *)m_Buffer.GetPointer();
        }

        public static bool IsEven(int select) => select % 2 == 0;

        public float ReadFloatSingle(int select) {
            #if FORCE_FPU_32
            return m_RegsSingle[select];
            #endif

            return m_RegsSingle[select << 1];
        }

        public void WriteFloatSingle(int select, float value) {
            #if FORCE_FPU_32
            m_RegsSingle[select] = value;
            return;
            #endif

            if (!FRMode && !IsEven(select)) {
                var s = select & 2;
                var dw = *(ulong *)&value;
                m_RegsDword[s] = ((ulong)dw << 32) | (uint)m_RegsDword[s];
            }
            else {
                m_RegsSingle[select << 1] = value;
            }
        }

        public uint ReadScalarWord(int select) {
             #if FORCE_FPU_32
            return m_RegsWord[select];
            #endif

            return m_RegsWord[select << 1];
        }

        public void WriteScalarWord(int select, uint value) {
            #if FORCE_FPU_32
            m_RegsWord[select] = value;
            return;
            #endif

            if (!FRMode && !IsEven(select)) {
                var s = select & 2;
                m_RegsDword[s] = ((ulong)value << 32) | (uint)m_RegsDword[s];
            }
            else {
                m_RegsWord[select << 1] = value;
            }
        }

        public double ReadFloatDouble(int select) {
            #if FORCE_FPU_32
            return m_RegsDouble[select];
            #endif

            if (!FRMode) {
                if (!IsEven(select)) return 0.0d;

                ulong lo = m_RegsWord[select + 0]; // Lo - Even
                ulong hi = m_RegsWord[select + 1]; // Hi - Odd
                ulong dw = (hi << 32) | lo;
                return *(double *)&dw;
            }
            else {
                return m_RegsDouble[select];
            }
        }

        public ulong ReadScalarDword(int select) {
            #if FORCE_FPU_32
            return m_RegsDword[select];
            #endif

            if (!FRMode) {
                if (!IsEven(select)) return 0L;

                ulong lo = m_RegsWord[select + 0]; // Lo - Even
                ulong hi = m_RegsWord[select + 1]; // Hi - Odd
                return (hi << 32) | lo;
            }
            else {
                return m_RegsDword[select];
            }
        }

        public void WriteFloatDouble(int select, double value) {
            #if FORCE_FPU_32
            m_RegsDouble[select] = value;
            #endif

            if (!FRMode && !IsEven(select)) {
                m_RegsDouble[select - 1] = value;
                m_RegsDouble[select - 0] = value;
            }
            else {
                m_RegsDouble[select] = value;
            }
        }

        public void WriteScalarDword(int select, ulong value) {
            #if FORCE_FPU_32
            m_RegsDword[select] = value;
            #endif

            if (!FRMode && !IsEven(select)) {
                m_RegsDword[select - 1] = value;
                m_RegsDword[select - 0] = value;
            }
            else {
                m_RegsDword[select] = value;
            }
        }

        public uint DebugRead32(int select) {
            return m_RegsWord[select];
        }

        public void DebugWrite32(int select, uint value) {
            m_RegsWord[select] = value;
        }
    }
}