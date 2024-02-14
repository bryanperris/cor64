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

        public bool FullMode => m_Status.FRMode;

        public FpuState(StatusRegister sr) {
            m_Status = sr;
            m_RegsDouble = (double *)m_Buffer.GetPointer();
            m_RegsSingle = (float *)m_Buffer.GetPointer();
            m_RegsDword = (ulong *)m_Buffer.GetPointer();
            m_RegsWord = (uint *)m_Buffer.GetPointer();
        }

        // TODO: When FR bit is toggled, we must rebuild the FPRs

        public static bool IsEven(int select) => select % 2 == 0;

        private static int AlignIndex64(int index) {
            if ((index & 1) != 0) return index - 1;
            else return index;
        }

        private static int AlignIndex32(int index) {
            return AlignIndex64(index) + index;
        }

        public float ReadFloatSingle(int select) {
            #if FORCE_FPU_64
            return m_RegsSingle[select];
            #endif

            if (!FullMode) {
                return m_RegsSingle[AlignIndex32(select)];
            }
            else {
                return m_RegsSingle[select << 1];
            }
        }

        private static uint AsBits(float value) {
            unsafe {
                return *(uint *)&value;
            }
        }

        public void WriteFloatSingle(int select, float value) {
            #if FORCE_FPU_64
            m_RegsSingle[select] = value;
            return;
            #endif

            if (!FullMode) {
                m_RegsDword[AlignIndex64(select)] = 0;
                m_RegsSingle[AlignIndex32(select)] = value;
            }
            else {
                // m_RegsDword[select] = 0;
                // m_RegsSingle[select << 1] = value;
                m_RegsDword[select] = AsBits(value);
            }
        }

        public uint ReadScalarWord(int select) {
             #if FORCE_FPU_64
            return m_RegsWord[select];
            #endif

            if (!FullMode) {
                return m_RegsWord[AlignIndex32(select)];
            }
            else {
                return m_RegsWord[select << 1];
            }
        }

        public void WriteScalarWord(int select, uint value) {
            #if FORCE_FPU_64
            m_RegsWord[select] = value;
            return;
            #endif

            if (!FullMode) {
                m_RegsWord[AlignIndex32(select)] = value;
            }
            else {
                m_RegsWord[select << 1] = value;
            }
        }

        public double ReadFloatDouble(int select) {
            #if FORCE_FPU_64
            return m_RegsDouble[select];
            #endif

            if(!FullMode) select = AlignIndex64(select);

            return m_RegsDouble[select];
        }

        public ulong ReadScalarDword(int select) {
            #if FORCE_FPU_64
            return m_RegsDword[select];
            #endif

            if (!FullMode) {
                return m_RegsDword[AlignIndex64(select)];
            }
            else {
                return m_RegsDword[select];
            }
        }

        public void WriteFloatDouble(int select, double value) {
            #if FORCE_FPU_64
            m_RegsDouble[select] = value;
            #endif

            if (!FullMode) select = AlignIndex64(select);

            m_RegsDouble[select] = value;
        }

        public void WriteScalarDword(int select, ulong value) {
            #if FORCE_FPU_64
            m_RegsDword[select] = value;
            #endif

            if (!FullMode) select = AlignIndex64(select);

            m_RegsDword[select] = value;
        }

        public uint DebugRead32(int select) {
            return m_RegsWord[select];
        }

        public void DebugWrite32(int select, uint value) {
            m_RegsWord[select] = value;
        }
    }
}