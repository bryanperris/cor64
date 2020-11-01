using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace cor64.IO
{
    public static class PtrHelper
    {
        public static void UnsafeRead(this IntPtr ptr, byte[] buffer, int offset, int length)
        {
            unsafe
            {
                byte* p = (byte*)ptr;

                for (int i = 0; i < length; i++)
                {
                    buffer[offset + i] = *p;
                    p++;
                }
            }
        }

        public static void UnsafeWrite(this IntPtr ptr, byte[] buffer, int offset, int length)
        {
            unsafe
            {
                byte* p = (byte*)ptr;

                for (int i = 0; i < length; i++)
                {
                    *p = buffer[offset + i];
                    p++;
                }
            }
        }

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static IntPtr Offset(this IntPtr ptr, int offset)
		{
			return IntPtr.Add(ptr, offset);
		}

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AsType_8(this IntPtr ptr, byte value)
        {
            unsafe
            {
                *((byte*)ptr) = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static byte AsType_8(this IntPtr ptr)
        {
            unsafe
            {
                return *((byte*)ptr);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AsType_16(this IntPtr ptr, ushort value)
        {
            unsafe
            {
                *((ushort*)ptr) = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AsType_16Swp(this IntPtr ptr, ushort value)
        {
            unsafe
            {
                *((ushort*)ptr) = value.ByteSwapped();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort AsType_16(this IntPtr ptr)
        {
            unsafe
            {
                return *((ushort*)ptr);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ushort AsType_16Swp(this IntPtr ptr)
        {
            unsafe
            {
                return (*((ushort*)ptr)).ByteSwapped();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AsType_32(this IntPtr ptr, uint value)
        {
            unsafe
            {
                *((uint*)ptr) = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AsType_32Swp(this IntPtr ptr, uint value)
        {
            unsafe
            {
                *((uint*)ptr) = value.ByteSwapped();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint AsType_32(this IntPtr ptr)
        {
            unsafe
            {
                return *((uint*)ptr);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint AsType_32Swp(this IntPtr ptr)
        {
            unsafe
            {
                return (*((uint*)ptr)).ByteSwapped();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AsType_64(this IntPtr ptr, ulong value)
        {
            unsafe
            {
                *((ulong*)ptr) = value;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AsType_64Swp(this IntPtr ptr, ulong value)
        {
            unsafe
            {
                *((ulong*)ptr) = value.ByteSwapped();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong AsType_64(this IntPtr ptr)
        {
            unsafe
            {
                return *((ulong*)ptr);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong AsType_64Swp(this IntPtr ptr)
        {
            unsafe
            {
                return (*((ulong*)ptr)).ByteSwapped();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void AsBytes(this IntPtr ptr, byte[] buffer)
        {
            unsafe
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    buffer[i] = *((byte*)ptr);
                    ptr = IntPtr.Add(ptr, 1);
                }
            }
        }
    }
}
