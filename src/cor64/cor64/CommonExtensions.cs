using System;
using System.Runtime.CompilerServices;

namespace cor64
{
    public static class CommonExtensions
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort ByteSwapped(this ushort value)
        {
            return (ushort)(((value & 0x00FF) << 8) |  ((value & 0xFF00) >> 8));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint ByteSwapped(this uint value)
        {
            return (uint) ((value & 0x000000FF) << 24) | ((value & 0x0000FF00) << 8) | ((value & 0x00FF0000) >> 8) | ((value & 0xFF000000) >> 24);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong ByteSwapped(this ulong value)
        {
            return (ulong) ((value& 0x00000000000000FFUL) << 56) |
                           ((value& 0x000000000000FF00UL) << 40) |
                           ((value& 0x0000000000FF0000UL) << 24) |
                           ((value& 0x00000000FF000000UL) << 8) |
                           ((value& 0x000000FF00000000UL) >> 8) |
                           ((value& 0x0000FF0000000000UL) >> 24) |
                           ((value& 0x00FF000000000000UL) >> 40) |
                           ((value& 0xFF00000000000000UL) >> 56);
        }
    }
}
