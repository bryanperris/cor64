using cor64.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

/** Notes:
 * 32 single float registers
 * 16 double float registers, all even ones are double.
 * Quiet NaN has the highest-significant bit set
 * Signaling NaN has the highest-significant bit set to zero
 */
namespace cor64.Mips
{
    public class FpuRegisters
    {
        private PinnedBuffer m_RegMem;
        public readonly RTypeWord S32;
        public readonly RTypeDword S64;
        public readonly RTypeSingle F32;
        public readonly RTypeDouble F64;

        /* NaN Signaling Table */
        private bool[] m_STable;

        public bool ConditionFlag { get; set; }

        public FpuRegisters()
        {
            m_RegMem = new PinnedBuffer(32 * 8);
            m_RegMem.Clear();
            S32 = new RTypeWord(m_RegMem.GetPointer());
            S64 = new RTypeDword(m_RegMem.GetPointer());
            F32 = new RTypeSingle(m_RegMem.GetPointer());
            F64 = new RTypeDouble(m_RegMem.GetPointer());
            m_STable = new bool[32];
        }

        /// <summary>
        /// Used to track which written NaNs are signaling
        /// </summary>
        public bool[] SignalNaNTable => m_STable;

        public unsafe sealed class RTypeSingle
        {
            private float*[] m_PtrTable;

            public RTypeSingle(IntPtr ptr)
            {
                var p = (float*)ptr;

                m_PtrTable = new float*[31];

                for (int i = 0; i < 31; i++)
                {
                    m_PtrTable[i] = (p + (i * 2));
                }
            }

            public float this[int index]
            {
                get => *m_PtrTable[index];
                set => *m_PtrTable[index] = value;
            }
        }

        public unsafe sealed class RTypeDouble
        {
            private double*[] m_PtrTable;

            public RTypeDouble(IntPtr ptr)
            {
                var p = (double*)ptr;

                m_PtrTable = new double*[32];

                for (int i = 0; i < 32; i++)
                {
                    m_PtrTable[i] = (p + i);
                }
            }

            public double this[int index]
            {
                get => *m_PtrTable[index];
                set => *m_PtrTable[index] = value;
            }
        }

        public unsafe sealed class RTypeWord
        {
            private uint*[] m_PtrTable;

            public RTypeWord(IntPtr ptr)
            {
                var p = (uint*)ptr;

                m_PtrTable = new uint*[32];

                for (int i = 0; i < 32; i++)
                {
                    m_PtrTable[i] = (p + (i * 2));
                }
            }

            public uint this[int index]
            {
                get => *m_PtrTable[index];
                set => *m_PtrTable[index] = value;
            }
        }

        public unsafe sealed class RTypeDword
        {
            private ulong*[] m_PtrTable;

            public RTypeDword(IntPtr ptr)
            {
                var p = (ulong*)ptr;

                m_PtrTable = new ulong*[32];

                for (int i = 0; i < 32; i++)
                {
                    m_PtrTable[i] = (p + i);
                }
            }

            public ulong this[int index]
            {
                get => *m_PtrTable[index];
                set => *m_PtrTable[index] = value;
            }
        }
    }
}
