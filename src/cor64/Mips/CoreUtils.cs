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

            if (!dwordMode)
            {
                /* Mask out the upper 32-bits in 32-bit mode */
                target &= 0xFFFFFFFF;
                target = (uint)((int)pc + (int)targetOffset);
            }
            else
            {
                target = (ulong)((long)pc + (long)targetOffset);
            }

            target += 4;

            return target;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ComputeTargetPC(bool isRegister, ulong pc, ulong source, uint targetImm)
        {
            ulong target = 0;

            if (!isRegister)
            {
                uint offset = targetImm << 2;
                target = offset | (pc & 0xF0000000);
            }
            else
            {
                target = source &= 0xFFFFFFFF;
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
