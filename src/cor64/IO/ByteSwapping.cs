using System.Runtime.InteropServices;
using System;
using System.IO;
using NLog;

namespace cor64.IO
{
    // This will be the central point for endianess control of the emulator
    // Native named methods will mean the appropiate endianess is established based host
    // Conditions (N64 is big endian)
    // Little-Endian: 32-bit byteswapping
    // Big-Endian: None
    // Catridge byteswapping depends on input endianess and host endianess
    public static class N64Endianess {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public static void PrintEndianess() {
            #if HOST_LITTLE_ENDIAN
                Console.WriteLine("Emulator Endianess: Little");
            #else
                #if LITTLE_ENDIAN_EXECUTION
                    Console.WriteLine("Platform Endianess: Big with little ALU");
                #else
                    Console.WriteLine("Emulator Endianess: Big");
                #endif
            #endif
        }

        public static int Address8(int address) {
            // NATIVE-ENDIANESS: LITTLE
            #if HOST_LITTLE_ENDIAN
            return ByteSwapping.Address8(address);
            #else
            return address;
            #endif
        }

        public static int Address16(int address) {
            // NATIVE-ENDIANESS: LITTLE
            #if HOST_LITTLE_ENDIAN
            return ByteSwapping.Address16(address);
            #else
            return address;
            #endif
        }

        public static long Address8(long address) {
            #if HOST_LITTLE_ENDIAN
            // NATIVE-ENDIANESS: LITTLE
            return ByteSwapping.Address8(address);
            #else
            return address;
            #endif
        }

        public static long Address16(long address) {
            #if HOST_LITTLE_ENDIAN
            // NATIVE-ENDIANESS: LITTLE
            return ByteSwapping.Address16(address);
            #else
            return address;
            #endif
        }

        public static void ByteArray(byte[] buffer, int offset, int count) {
            #if HOST_LITTLE_ENDIAN
            // NATIVE-ENDIANESS: LITTLE
            ByteSwapping.SwapBuffer(buffer, offset, count);
            #else
            return;
            #endif
        }

        public static ushort JoyconRead(ushort value) {
            #if HOST_LITTLE_ENDIAN
                return value.ByteSwapped();
            #else
                #if LITTLE_ENDIAN_EXECUTION
                    return value.ByteSwapped();
                #else
                    return value;
                #endif
            #endif
        }

        public static ushort ReadPtr16(IntPtr ptr)
        {
            #if HOST_LITTLE_ENDIAN
                return ptr.AsType_16();
            #else
                #if LITTLE_ENDIAN_EXECUTION
                    return ptr.AsType_16Swp();
                #else
                    return ptr.AsType_16();
                #endif
            #endif
        }

        public static void WritePtr16(IntPtr ptr, ushort value) {
            #if HOST_LITTLE_ENDIAN
                ptr.AsType_16(value);
            #else
                #if LITTLE_ENDIAN_EXECUTION
                    ptr.AsType_16Swp(value);
                #else
                    ptr.AsType_16(value);
                #endif
            #endif
        }

        public static ushort U16(ushort value) {
            #if HOST_LITTLE_ENDIAN
                return value;
            #else
                #if LITTLE_ENDIAN_EXECUTION
                    return CommonExtensions.ByteSwapped(value);
                #else
                    return value;
                #endif
            #endif
        }

        public static uint U32(uint value) {
            #if HOST_LITTLE_ENDIAN
                return value;
            #else
                #if LITTLE_ENDIAN_EXECUTION
                    return CommonExtensions.ByteSwapped(value);
                #else
                    return value;
                #endif
            #endif
        }

        public static ulong U64(ulong value) {
            #if HOST_LITTLE_ENDIAN
                return value;
            #else
                #if LITTLE_ENDIAN_EXECUTION
                    return CommonExtensions.ByteSwapped(value);
                #else
                    return value;
                #endif
            #endif
        }

        public static uint ReadPtr32(IntPtr ptr)
        {
            #if HOST_LITTLE_ENDIAN
                return ptr.AsType_32();
            #else
                #if LITTLE_ENDIAN_EXECUTION
                    return ptr.AsType_32Swp();
                #else
                    return ptr.AsType_32();
                #endif
            #endif
        }

        public static void WritePtr32(IntPtr ptr, uint value) {
            #if HOST_LITTLE_ENDIAN
                ptr.AsType_32(value);
            #else
                #if LITTLE_ENDIAN_EXECUTION
                    ptr.AsType_32Swp(value);
                #else
                    ptr.AsType_32(value);
                #endif
            #endif
        }

        public static ulong ReadPtr64(IntPtr ptr)
        {
            #if HOST_LITTLE_ENDIAN
                return ptr.AsType_64();
            #else
                #if LITTLE_ENDIAN_EXECUTION
                    return ptr.AsType_64Swp();
                #else
                    return ptr.AsType_64();
                #endif
            #endif
        }

        public static void WritePtr64(IntPtr ptr, ulong value) {
            #if HOST_LITTLE_ENDIAN
                ptr.AsType_64(value);
            #else
                #if LITTLE_ENDIAN_EXECUTION
                    ptr.AsType_64Swp(value);
                #else
                    ptr.AsType_64(value);
                #endif
            #endif
        }

        public static ulong U64From32(uint first, uint second) {
            #if !HOST_LITTLE_ENDIAN && LITTLE_ENDIAN_EXECUTION
            return ((ulong)second << 32) | first; // If we access big-endian via little-endian pointer
            #else
            return ((ulong)first << 32) | second;
            #endif
        }

        public static (uint, uint) U64To32(ulong value) {
            #if !HOST_LITTLE_ENDIAN && LITTLE_ENDIAN_EXECUTION
            return ((uint)value, (uint)(value >> 32)); // If we access big-endian via little-endian pointer
            #else
            return ((uint)(value >> 32), (uint)value);
            #endif
        }
    }

    public static class ByteSwapping {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public static int Address8(int address) {
            return address ^ 3;
        }

        public static int Address16(int address) {
            return address ^ 2;
        }

        public static long Address8(long address) {
            return address ^ 3;
        }

        public static long Address16(long address) {
            return address ^ 2;
        }

        public static void SwapBuffer(byte[] buffer, int offset, int count) {
            if (offset >= buffer.Length) throw new ArgumentOutOfRangeException(nameof(offset));
			if (offset + count > buffer.Length) throw new ArgumentOutOfRangeException(nameof(count));
			byte[] swapped = new byte[count];
            for (int i = 0; i < count; i++) {
				int swap = (offset + i) ^ 3;
				swapped[i] = (swap < buffer.Length) ? buffer[swap] : (byte)0;
            }
			Array.Copy(swapped, 0, buffer, offset, count);
        }
    }

    public abstract class ByteSwapStream : Stream
    {
        private Stream m_BaseStream;

        protected ByteSwapStream(Stream baseStream)
        {
            if (baseStream == null)
                throw new ArgumentNullException(nameof(baseStream));

            m_BaseStream = baseStream;
        }

        public sealed override bool CanRead
        {
            get { return m_BaseStream.CanRead; }
        }

        public sealed override bool CanSeek
        {
            get { return m_BaseStream.CanSeek; }
        }

        public sealed override bool CanWrite
        {
            get { return m_BaseStream.CanWrite; }
        }

        public sealed override void Flush()
        {
            m_BaseStream.Flush();
        }

        public sealed override long Length
        {
            get { return m_BaseStream.Length; }
        }

        public sealed override long Position
        {
            get {
                return m_BaseStream.Position;
            }
            set {
                m_BaseStream.Position = value;
            }
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException(nameof(buffer));

            /* Copy a block of data that isn't swapped */
            Byte[] innerBuffer = new Byte[count];

            try {
                m_BaseStream.Read(innerBuffer, 0, count);
            }
            catch (Exception) {
                throw;
            }

            /* TODO: Some error checkign when larger byte swappers crashes on small buffers */

            /* Read into the new buffer swapped */
            for (int i = offset; i < count; i++) {
                buffer[i] = innerBuffer[(Int32)ComputeNextSwapPosition(i - offset)];
            }

            return count;
        }

        public sealed override long Seek(long offset, SeekOrigin origin)
        {
            return m_BaseStream.Seek(offset, origin);
        }

        public sealed override void SetLength(long value)
        {
            m_BaseStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            Byte[] innerBuffer = new Byte[count];

            /* Write the data to inner buffer as unswapped */
            for (int i = offset; i < count; i++) {
                innerBuffer[(Int32)ComputeNextSwapPosition(i - offset)] = buffer[i];
            }

            try {
                m_BaseStream.Write(innerBuffer, 0, count);
            }
            catch { throw; }
        }

        public sealed override int ReadByte()
        {
            return m_BaseStream.ReadByte();
        }

        public sealed override void WriteByte(byte value)
        {
            m_BaseStream.WriteByte(value);
        }

        protected abstract Int64 ComputeNextSwapPosition(Int64 position);
    }

    public sealed class Swap16Stream : ByteSwapStream
    {
        public Swap16Stream(Stream baseStream) : base(baseStream)
        {
        }

        protected override long ComputeNextSwapPosition(long position)
        {
            return (position & unchecked(0x7FFFFFFFFFFFFFFEL)) + 1 - (position % 2);
        }
    }

    public sealed class Swap32Stream : ByteSwapStream
    {
        public Swap32Stream(Stream baseStream) : base(baseStream)
        {
        }

        protected override long ComputeNextSwapPosition(long position)
        {
            return (position & unchecked(0x7FFFFFFFFFFFFFFCL)) + 3 - (position % 4);
        }
    }

    public sealed class Swap64Stream : ByteSwapStream
    {
        public Swap64Stream(Stream baseStream) : base(baseStream)
        {
        }

        protected override long ComputeNextSwapPosition(long position)
        {
            return (position & unchecked(0x7FFFFFFFFFFFFFF8L)) + 7 - (position % 8);
        }
    }

    public sealed class SwapAutoStream : ByteSwapStream {
        private readonly Swap16Stream m_S16;
        private readonly Swap32Stream m_S32;
        private readonly Swap64Stream m_S64;
        private readonly Stream m_BaseStream;

        public SwapAutoStream(Stream baseStream) : base(baseStream)
        {
            m_BaseStream = baseStream;
            m_S16 = new Swap16Stream(baseStream);
            m_S32 = new Swap32Stream(baseStream);
            m_S64 = new Swap64Stream(baseStream);
        }

        public override int Read(byte[] buffer, int offset, int count) {
            Stream s;

            switch (count) {
                case 1: s = m_BaseStream; break;
                case 2: s = m_S16; break;
                case 4: s = m_S32; break;
                case 8: s = m_S64; break;
                default: return base.Read(buffer, offset, count);
            }

            return s.Read(buffer, offset, count);
        }

        public override void Write(byte[] buffer, int offset, int count) {
            Stream s = null;

            switch (count) {
                case 2: s = m_S16; break;
                case 4: s = m_S32; break;
                case 8: s = m_S64; break;
                default: {
                    base.Write(buffer, offset, count);
                    return;
                }
            }

            s.Write(buffer, offset, count);
        }

        protected override long ComputeNextSwapPosition(long position)
        {
            return position;
        }
    }


    public sealed class SwappingSteam : Stream
    {
        // This class is to convert BE to LE

        private readonly Stream m_BaseStream;

        public SwappingSteam(Stream baseStream)
        {
            m_BaseStream = baseStream;
        }

        public override bool CanRead => m_BaseStream.CanRead;

        public override bool CanSeek => m_BaseStream.CanSeek;

        public override bool CanWrite => m_BaseStream.CanWrite;

        public override long Length => m_BaseStream.Length;

        public override long Position {
            get => m_BaseStream.Position;
            set => m_BaseStream.Position = value;
        }

        public override void Flush()
        {
            m_BaseStream.Flush();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            int result = m_BaseStream.Read(buffer, offset, count);
            ByteSwapping.SwapBuffer(buffer, offset, count);
            return result;
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            return m_BaseStream.Seek(offset, origin);
        }

        public override void SetLength(long value)
        {
            m_BaseStream.SetLength(value);
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            byte[] swapped = new byte[count];
            Array.Copy(buffer, offset, swapped, 0, count);
            ByteSwapping.SwapBuffer(swapped, 0, count);
            m_BaseStream.Write(swapped, 0, count);
        }

        private void SwapPosition() {
            m_BaseStream.Position = ByteSwapping.Address8(m_BaseStream.Position);
        }

        public override int ReadByte()
        {
            SwapPosition();
            var read = m_BaseStream.ReadByte();
            SwapPosition();
            return read;
        }

        public override void WriteByte(byte value)
        {
            SwapPosition();
            base.WriteByte(value);
            SwapPosition();
        }
    }
}