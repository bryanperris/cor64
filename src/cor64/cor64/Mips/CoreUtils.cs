using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace cor64.Mips
{
    public static class CoreUtils
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ComputeBranchTargetOffset(uint targetImm)
        {
            /* Compute the target address the real way */

            /* Process it as a 64-bit value */
            /* Sign extend first */
            ulong target = (ulong)(short)targetImm;

            /* Word alignment */
            target <<= 2;

            /* Combine the sign extension with the shifted offset */
            return (target & ~0x3FFFFUL) | (target & 0x3FFFF); 
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ComputeBranchPC(bool dwordMode, ulong pc, ulong targetOffset)
        {
            ulong target = targetOffset;

            if (dwordMode)
            {
                /* Mask out the upper 32-bits in 32-bit mode */
                target &= 0xFFFFFFFF;
                target = (uint)((int)pc + (int)targetOffset);
            }
            else
            {
                target = (ulong)((long)pc + (long)targetOffset);
            }

            return target + 4;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool ComputeBranchCondition(
            bool dwordMode, 
            ulong source, 
            ulong target, 
            bool copState, 
            RegBoundType copSelect, 
            ArithmeticOp compareOp)
        {
            bool condition = false;

            bool EQ_ZERO()
            {
                return source == 0;
            }

            bool GT_ZERO()
            {
                if (dwordMode)
                {
                    ulong v = source;
                    return (v >> 63) == 0 && v > 0;
                }
                else
                {
                    uint v = (uint)source;
                    return (v >> 31) == 0 && v > 0;
                }
            }

            bool LT_ZERO()
            {
                if (dwordMode)
                {
                    ulong v = source;
                    return (v >> 63) == 1 && v > 0;
                }
                else
                {
                    uint v = (uint)source;
                    return (v >> 31) == 1 && v > 0;
                }
            }

            bool COP_ENABLED(bool compare)
            {
                switch (copSelect)
                {
                    case RegBoundType.Cp1:
                        {
                            if (copState)
                            {
                                return compare;
                            }
                            else
                            {
                                return !compare;
                            }
                        }

                    default: throw new NotSupportedException("MIPS does not support this kind of unit");
                }
            }

            switch (compareOp)
            {
                default: throw new NotSupportedException("MIPS does not support this branch operation");
                case ArithmeticOp.EQUAL: condition = (source == target); break;
                case ArithmeticOp.NOT_EQUAL: condition = (source != target); break;
                case ArithmeticOp.GREATER_THAN: condition = GT_ZERO(); break;
                case ArithmeticOp.LESS_THAN: condition = LT_ZERO(); break;
                case ArithmeticOp.GREATER_THAN_OR_EQUAL: condition = (EQ_ZERO() || GT_ZERO()); break;
                case ArithmeticOp.LESS_THAN_OR_EQUAL: condition = (EQ_ZERO() || LT_ZERO()); break;
                case ArithmeticOp.FALSE: condition = COP_ENABLED(false); break;
                case ArithmeticOp.TRUE: condition = COP_ENABLED(true); break;
            }

            return condition;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ComputeTargetPC(bool dwordMode, bool isRegister, ulong pc, ulong source, uint targetImm)
        {
            ulong target = 0;

            if (!isRegister)
            {
                if (dwordMode)
                {
                    ulong offset = targetImm << 2;
                    target = offset | (pc & 0xFFFFFFFFF0000000);
                }
                else
                {
                    uint offset = targetImm << 2;
                    target = offset | (pc & 0xF0000000);
                }
            }
            else
            {
                target = source;

                if (!dwordMode)
                {
                    target &= 0xFFFFFFFF;
                }
            }

            return target;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt128 Multiply64_Unsigned(ulong a, ulong b)
        {
            ulong A_HI = a >> 32;
            ulong A_LO = a & 0xFFFFFFFF;
            ulong B_HI = b >> 32;
            ulong B_LO = b & 0xFFFFFFFF;

            /* Product parts */
            ulong P1 = A_LO * B_LO;
            ulong P2 = A_HI * B_LO;
            ulong P3 = A_LO * B_HI;
            ulong P4 = A_HI * B_HI;

            uint CARRY = (uint)(((P1 >> 32) + (uint)P2 + (uint)P3) >> 32);

            ulong TEMP = (P2 >> 32) + (P3 >> 32);

            UInt128 v;
            v.lo = P1 + (P2 << 32) + (P3 << 32);
            v.hi = P4 + TEMP + CARRY;

            return v;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static UInt128 Multiply64_Signed(ulong a, ulong b)
        {
            ulong A_HI = (ulong)(int)(uint)(a >> 32);
            ulong A_LO = a & 0xFFFFFFFF;
            ulong B_HI = (ulong)(int)(uint)(b >> 32);
            ulong B_LO = b & 0xFFFFFFFF;

            /* Product parts */
            ulong P1 = A_LO * B_LO;
            ulong P2 = A_HI * B_LO;
            ulong P3 = A_LO * B_HI;
            ulong P4 = A_HI * B_HI;

            uint CARRY = (uint)(((P1 >> 32) + (uint)P2 + (uint)P3) >> 32);

            ulong TEMP = (ulong)(int)(uint)((P2 >> 32) + (P3 >> 32));

            UInt128 v;
            v.lo = P1 + (P2 << 32) + (P3 << 32);
            v.hi = P4 + TEMP + CARRY;

            return v;
        }
    }
}
