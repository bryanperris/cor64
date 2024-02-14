using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace cor64.IO
{
    /* This block should be mapped to Domain 1, Address 2 region of memory */
    /*  0x10000000 to 0x1FBFFFFF size=00FBFFFFF */
    public class MemMappedCart : N64MemoryDevice
    {
        //private Stream m_SourceStream;
        private readonly UnmanagedBuffer m_RomBuffer;
        private readonly int m_RealSize;
        private readonly UnmanagedBuffer m_IsViewerMem = new(4 + 200);

        public MemMappedCart(UnmanagedBuffer romBuffer, N64MemoryController controller) : base(controller, 0xFC00000)
        {
            m_RomBuffer = romBuffer;
            m_RealSize = romBuffer.Size;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                //m_SourceStream.Dispose();
            }

            base.Dispose(disposing);
        }

        public IntPtr GetCartridgePointer() => m_RomBuffer.GetPointer();

        private void PrintFirstWord() {
            StringBuilder firstWordHex = new StringBuilder();
            firstWordHex.Append("Rom first word: ");
            for (int i = 0; i < 4; i++) {
                // firstWordHex.Append(Marshal.ReadByte(m_RomBuffer.GetPointer().Offset(i)).ToString("X2"));
                firstWordHex.Append(Read8(i).ToString("X2"));
            }
            Console.WriteLine(firstWordHex.ToString());
        }

        public void SelfTest(Stream romSource)
        {
            PrintFirstWord();

            string hashA;
            {
                var romSourceHash = SHA256.Create();
                romSource.Position = 0;
                hashA = BitConverter.ToString(romSourceHash.ComputeHash(romSource));
            }

            string hashB;
            {
                byte[] buffer = new byte[m_RomBuffer.Size];
                Marshal.Copy(m_RomBuffer.GetPointer(), buffer, 0, buffer.Length);
                hashB = BitConverter.ToString(SHA256.HashData(buffer));
            }

            string hashC;
            {
                byte[] buffer = new byte[m_RomBuffer.Size];
                for (int i = 0; i < m_RomBuffer.Size; i++) {
                    buffer[i] = Read8(i);
                }
                hashC = BitConverter.ToString(SHA256.HashData(buffer));
            }

            if (hashA != hashB) throw new Exception("Cartridge stream does not match with memory copy!");
            if (hashA != hashC) throw new Exception("Cartridge stream does not match with self read!");
            if (hashB != hashC) throw new Exception("Cartridge memory copy doesn't match with self read");

            Console.WriteLine("Cartridge Domain 2, Address 2 passes checksum tests");
        }

        protected override N64AlignedPtr DynamicReadMap(int offset)
        {
            // ISViewer memory mapping
            #if ENABLE_ISVIEWER
            if (offset == 0x03FF0014) {
                return N64AlignedPtr.FromPtr(m_IsViewerMem.GetPointer());
            }

            if (offset >= 0x03FF0020 && offset < (0x03FF0020 + 200)) {
                int off = offset - 0x03FF0020;
                return N64AlignedPtr.FromPtr(m_IsViewerMem.GetPointer() + (4 + off));
            }
            #endif

            // Map actual ROM data
            if (offset < m_RealSize) {
                nint ptr = m_RomBuffer.GetPointer() + offset;
                // return new  romPtr + memoryOffset;
                // Console.WriteLine("Cart Read Map: {0:X8} to {1}", Marshal.ReadInt32(ptr), offset);
                return N64AlignedPtr.FromPtr(ptr);
            }

            return N64AlignedPtr.DUMMY;
        }

        protected override N64AlignedPtr DynamicWriteMap(int offset)
        {
            #if ENABLE_ISVIEWER
            if (offset == 0x03FF0014) {
                return N64AlignedPtr.FromPtr(m_IsViewerMem.GetPointer());
            }
            else if (offset >= 0x03FF0020 && offset < (0x03FF0020 + 200)) {
                int off = offset - 0x03FF0020;
                return N64AlignedPtr.FromPtr(m_IsViewerMem.GetPointer().Offset(4 + off));
            }
            #endif

            return N64AlignedPtr.DUMMY;
        }

        public override string Name => "Cartridge";
    }
}
