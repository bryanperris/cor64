using System;
using System.Runtime.CompilerServices;

namespace cor64.Mips
{
    public static class InstructionHelper
    {
        private const int SIZE_8 = (int)ExecutionFlags.Data8;
        private const int SIZE_16 = (int)ExecutionFlags.Data16;
        private const int SIZE_32 = (int)ExecutionFlags.Data32;
        private const int SIZE_64 = (int)ExecutionFlags.Data64;
        private const int SIZE_128 = (int)ExecutionFlags.Data128;
        private const int SIZE_MASK = (SIZE_8 | SIZE_16 | SIZE_32 | SIZE_64 | SIZE_128);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsUnsigned(this DecodedInstruction inst)
        {
            return (inst.Op.Flags & ExecutionFlags.Unsigned) == ExecutionFlags.Unsigned;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsImmediate(this DecodedInstruction inst)
        {
            return (inst.Op.Flags & ExecutionFlags.Immediate) == ExecutionFlags.Immediate;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsVariableShift(this DecodedInstruction inst)
        {
            return (inst.Op.Flags & ExecutionFlags.VarShift) == ExecutionFlags.VarShift;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsShift32(this DecodedInstruction inst)
        {
            return (inst.Op.Flags & ExecutionFlags.Shift32) == ExecutionFlags.Shift32;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLeft(this DecodedInstruction inst)
        {
            return (inst.Op.Flags & ExecutionFlags.Left) == ExecutionFlags.Left;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRight(this DecodedInstruction inst)
        {
            return (inst.Op.Flags & ExecutionFlags.Right) == ExecutionFlags.Right;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLikely(this DecodedInstruction inst)
        {
            return (inst.Op.Flags & ExecutionFlags.Likely) == ExecutionFlags.Likely;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsLink(this DecodedInstruction inst)
        {
            return (inst.Op.Flags & ExecutionFlags.Link) == ExecutionFlags.Link;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsRegister(this DecodedInstruction inst)
        {
            return (inst.Op.Flags & ExecutionFlags.Register) == ExecutionFlags.Register;
        }

        /// <summary>
        /// Returns the size in bytes based on execution flags
        /// </summary>
        /// <param name="inst"></param>
        /// <returns></returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int DataSize(this DecodedInstruction inst)
        {
            switch ((int)inst.Op.Flags & SIZE_MASK)
            {
                case SIZE_8:  return 1;
                case SIZE_16: return 2;
                case SIZE_32: return 4;
                case SIZE_64: return 8;
                case SIZE_128: return 16;
                default: return 0;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsData32(this DecodedInstruction inst)
        {
            return (inst.Op.Flags & ExecutionFlags.Data32) == ExecutionFlags.Data32;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool IsData64(this DecodedInstruction inst)
        {
            return (inst.Op.Flags & ExecutionFlags.Data64) == ExecutionFlags.Data64;
        }
    }
}
