using System.Runtime.InteropServices;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using cor64.Debugging;
using cor64.Mips;
using cor64.PIF;
using cor64.RCP;
using NLog;
using static cor64.N64CTS;
using cor64.IO;

/*
    0x0000 0000 to 0x03EF FFFF RDRAM Memory
    0x03F0 0000 to 0x03FF FFFF RDRAM Registers
    0x0400 0000 to 0x040F FFFF SP Registers
    0x0410 0000 to 0x041F FFFF DP Command Registers
    0x0420 0000 to 0x042F FFFF DP Span Registers
    0x0430 0000 to 0x043F FFFF MIPS Interface (MI) Registers
    0x0440 0000 to 0x044F FFFF Video Interface (VI) Registers
    0x0450 0000 to 0x045F FFFF Audio Interface (AI) Registers
    0x0460 0000 to 0x046F FFFF Peripheral Interface (PI) Registers
    0x0470 0000 to 0x047F FFFF RDRAM Interface (RI) Registers
    0x0480 0000 to 0x048F FFFF Serial Interface (SI) Registers
    0x0490 0000 to 0x04FF FFFF Unused
    0x0500 0000 to 0x05FF FFFF Cartridge Domain 2 Address 1
    0x0600 0000 to 0x07FF FFFF Cartridge Domain 1 Address 1
    0x0800 0000 to 0x0FFF FFFF Cartridge Domain 2 Address 2
    0x1000 0000 to 0x1FBF FFFF Cartridge Domain 1 Address 2
    0x1FC0 0000 to 0x1FC0 07BF PIF ROM
    0x1FC0 07C0 to 0x1FC0 07FF PIF RAM
    0x1FC0 0800 to 0x1FCF FFFF Reserved
    0x1FD0 0000 to 0x7FFF FFFF Cartridge Domain 1 Address 3
    0x8000 0000 to 0xFFFF FFFF External SysAD Device
*/

namespace cor64
{
    public class N64MemoryController : IMemoryAccess
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private MemMappedCart m_CartRom; // Domain 1, Address 2 region
        private readonly static MemoryDebugger s_MemDebugger = new MemoryDebugger();
        private readonly N64MemoryDevice[] m_MemMap = new N64MemoryDevice[0xFFFF];
        private readonly uint[] m_BaseMap = new uint[0xFFFF];
        // private readonly UnifiedMemModel<N64MemoryDevice> m_MemModel = new();
        private readonly object m_WriteLock = new();

        [ThreadStatic] private readonly static byte[] s_ReadSingle = { 0 };
        [ThreadStatic] private readonly static byte[] s_WriteSingle = { 0 };

        private readonly UnmanagedBuffer m_ValueBuffer = new UnmanagedBuffer(8);

        public N64MemoryController()
        {
            DummyMemory internalDummy = new DummyMemory(8, "Internal dummy (should never hit this)", this);
            for (int i = 0; i < m_MemMap.Length; i++)
            {
                m_MemMap[i] = internalDummy;
            }

            Cart = new DummyMemory(0xFC00000, "Dummy cartridge", this);
            RDRAM = new Rdram(this);
            RIRegs = new RdramInterface(this);
            RDRAMRegs = new RdramRegisters(this);
            PIF = new PIFController(this);
            DiskDriveRegisters = new DummyMemory(0x1000000, "Cartridge Domain 2, Address 1", this);
            Unused_0490 = new DummyMemory(0x00700000, "Unused 0490", this);
            Unused_0600 = new DummyMemory(0x02000000, "Cartridge Domain 1 Address 1", this);
            Unused_1FD0 = new DummyMemory(0x1000000, "Cartridge Domain 1 Address 3", this);  /* 0x60300000 is too large */
        }

        public void Init()
        {
            RDRAM?.Init();
            RDRAMRegs?.Init();
            SPRegs?.Init();
            DPCmdRegs?.Init();
            DPSpanRegs?.Init();
            MIRegs?.Init();
            VIRegs?.Init();
            AIRegs?.Init();
            PIRegs?.Init();
            RIRegs?.Init();
            SIRegs?.Init();
            Cart?.Init();
            PIF?.Init();
            DiskDriveRegisters?.Init();
            Unused_0490.Init();
            Unused_0600.Init();
            Unused_1FD0.Init();

            Map(RDRAM, 0x0000, 0x03EF);
            Map(RDRAMRegs, 0x03F0, 0x03FF);
            Map(SPRegs, 0x0400, 0x040F);
            Map(DPCmdRegs, 0x0410, 0x041F);
            Map(DPSpanRegs, 0x0420, 0x042F);
            Map(MIRegs, 0x0430, 0x043F);
            Map(VIRegs, 0x0440, 0x044F);
            Map(AIRegs, 0x0450, 0x045F);
            Map(PIRegs, 0x0460, 0x046F);
            Map(RIRegs, 0x0470, 0x047F);
            Map(SIRegs, 0x0480, 0x048F);
            Map(Unused_0490, 0x0490, 0x04FF);
            Map(DiskDriveRegisters, 0x0500, 0x05FF);
            Map(Unused_0600, 0x0600, 0x07FF);
            Map(Cart, 0x1000, 0x1FBF);
            Map(PIF, 0x1FC0, 0x1FCF);
            Map(Unused_1FD0, 0x1FD0, 0x7FFF);
        }

        private void Map(N64MemoryDevice t, int start, int end)
        {
            uint b = (uint)start << 16;

            for (int i = start; i <= end; i++)
            {
                m_MemMap[i] = t;
                m_BaseMap[i] = b;
            }
        }

        public void WriteLockEnter()
        {
            Monitor.Enter(m_WriteLock);
        }

        public void WriteLockExit()
        {
            Monitor.Exit(m_WriteLock);
        }

        [Conditional("SAFE_MEMORY_ACCESS")]
        private void ThrowOnBadIndex(int index)
        {
            if (index >= m_MemMap.Length) throw new InvalidOperationException("N64 memory access is out of range");
        }

        private N64MemoryDevice GetDevice(long address, out long deviceOffset)
        {
            int index = (int)(address >> 16);

            ThrowOnBadIndex(index);

            deviceOffset = address - m_BaseMap[index];

            return m_MemMap[index];
        }

        // private void DebugValue32(long address, byte[] buffer, uint testValue, bool isRead) {
        //     unsafe {
        //         fixed (byte * ptr = &buffer[0]) {
        //             IntPtr p = (IntPtr)ptr;
        //             var v = p.AsType_32Swp();

        //             if (v == testValue) {
        //                 if (isRead) {
        //                     Console.WriteLine("MEM32 DEBUG READ: {0:X8} {1:X8}", (uint)address, v);
        //                 }
        //                 else {
        //                     Console.WriteLine("MEM32 DEBUG WRITE: {0:X8} {1:X8}", (uint)address, v);
        //                 }
        //             }
        //         }
        //     }
        // }

        // public byte ReadPhysical8(long address) {
        //     var device = GetDevice((int)address, out int deviceOffset);
        //     return device.SmartRead<byte>(deviceOffset);
        // }

        // public void WritePhysical(long address, byte value) {
        //     var device = GetDevice((int)address, out int deviceOffset);
        //     device.SmartWrite(deviceOffset, value);
        // }

        // public ushort ReadPhysical16(long address) {
        //     var device = GetDevice((int)address, out int deviceOffset);
        //     return device.SmartRead<ushort>(deviceOffset);
        // }

        // public void WritePhysical(long address, ushort value) {
        //     var device = GetDevice((int)address, out int deviceOffset);
        //     device.SmartWrite(deviceOffset, value);
        // }

        // public uint ReadPhysical32(long address) {
        //     var device = GetDevice((int)address, out int deviceOffset);
        //     return device.SmartRead<uint>(deviceOffset);
        // }

        // public void WritePhysical(long address, uint value) {
        //     var device = GetDevice((int)address, out int deviceOffset);
        //     device.SmartWrite(deviceOffset, value);
        // }

        // public unsafe ulong ReadPhysical64(long address) {
        //     var device = GetDevice((int)address, out int deviceOffset);
        //     return device.SmartRead<ulong>(deviceOffset);
        // }

        // public unsafe void WritePhysical(long address, ulong value) {
        //     var device = GetDevice((int)address, out int deviceOffset);
        //     device.SmartWrite(deviceOffset, value);
        // }

        // public unsafe void Read(long address, byte[] buffer, int offset, int count)
        // {
        //     var device = GetDevice((int)address, out int deviceOffset);

        //     // try {

        //     fixed (byte * bufferPtr = &buffer[offset]) {
        //         nint ptr = (nint)bufferPtr;

        //         switch (count) {
        //             case 1: buffer[offset] = device.Read8(N64Endianess.Address8(deviceOffset)); break;
        //             case 2: ptr.AsType_16(device.SmartRead<ushort>(N64Endianess.Address16(deviceOffset))); break;
        //             case 4: ptr.AsType_32(device.SmartRead<uint>(deviceOffset)); break;
        //             case 8: ptr.AsType_64(device.SmartRead<ulong>(deviceOffset)); break;
        //             default: {
        //                 for (int i = 0; i < count; i++) {
        //                     buffer[i + offset] = device.Read8(deviceOffset + i);
        //                 }
        //                 break;
        //             }
        //         }
        //     }
        //     // }
        //     // catch {
        //     //     throw new Exception(String.Format("N64Controller Bad Read: {0:X8} @{1}", address, device));
        //     // }

        //     device.DispatchReadNotifications(deviceOffset, count);
        // }

        // public unsafe void Write(long address, byte[] buffer, int offset, int count)
        // {
        //     var device = GetDevice((int)address, out int deviceOffset);

        //     fixed (byte * bufferPtr = &buffer[offset]) {
        //         nint ptr = (nint)bufferPtr;

        //         switch (count) {
        //             case 1: device.Write8(N64Endianess.Address8(deviceOffset), buffer[offset]); break;
        //             case 2: device.SmartWrite(N64Endianess.Address16(deviceOffset), ptr.AsType_16()); break;
        //             case 4: device.SmartWrite(deviceOffset, ptr.AsType_32()); break;
        //             case 8: device.SmartWrite(deviceOffset, ptr.AsType_64()); break;
        //             default: {
        //                 for (int i = 0; i < count; i++) {
        //                     device.Write8(deviceOffset + i, buffer[i + offset]);
        //                 }
        //                 break;
        //             }
        //         }
        //     }
        //     device.DispatchWriteNotifications(deviceOffset, count);
        // }

        // public N64MemoryStream CreateMemoryStream()
        // {
        //     var stream = new N64MemoryStream(this, 0, 0xFFFFFFFFU);
        //     return stream;
        // }

        // public N64MemoryStream CreateMemoryStream(long baseAdress, long size)
        // {
        //     var stream = new N64MemoryStream(this, baseAdress, size);
        //     return stream;
        // }

        public void MountCartridge(Cartridge cartridge)
        {
            // CLEANUP: Should just keep a single copy of ROM
            var romMemory = new UnmanagedBuffer((int)cartridge.RomStream.Length);
            byte[] buffer = new byte[romMemory.Size];
            cartridge.RomStream.Position = 0;
            cartridge.RomStream.Read(buffer, 0, buffer.Length);
            romMemory.CopyInto(buffer);
            m_CartRom = new MemMappedCart(romMemory, this);
            Cart = m_CartRom;
        }

        // public void AttachDmaEngine_PI(DmaEngine engine) {
        //     engine.AttachMemory(RDRAM, Cart);
        // }

        // public void AttachDmaEngine_SP(DmaEngine engine) {
        //     engine.AttachMemory(RDRAM, SPRegs);
        // }

        // public void AttachDmaEngine_SI(DmaEngine engine) {
        //     engine.AttachMemory(RDRAM, PIF);
        // }

        // public int MemoryCopy(uint srcAddress, uint destAddress, int len) {
        //     int count = 0;

        //     byte[] buffer = new byte[4];

        //     for (int i = 0; i < (len / 4); i++)
        //     {
        //         var src = srcAddress + (i * 4);
        //         var dst = destAddress + (i * 4);

        //         Read(src, buffer, 0, 4);
        //         Write(dst, buffer, 0, 4);
        //         count += 4;
        //     }

        //     return count;
        // }

        // public int MemoryCopyUnaligned(uint srcAddress, uint destAddress, int len) {
        //     int count = 0;

        //     byte[] buffer = new byte[1];

        //     for (int i = 0; i < len; i++)
        //     {
        //         var src = srcAddress + i;
        //         var dst = destAddress + i;

        //         Read(src, buffer, 0, 1);

        //         Write(dst, buffer, 0, 1);

        //         count++;
        //     }

        //     return count;
        // }

        private static bool AlignmetCheck(long address, int size)
        {
            return (address & (size - 1)) == 0;
        }

        private unsafe T UnalignedRead<T>(N64MemoryDevice device, long offset) where T : unmanaged
        {
            T value = default;
            byte* ptr = (byte*)&value;
            int readOffset = (int)offset;

            for (int i = 0; i < sizeof(T); i++)
            {
                *ptr++ = device.Read8(readOffset++);
            }

            return value;
        }

        private unsafe void UnalignedWrite<T>(N64MemoryDevice device, long offset, T value) where T : unmanaged
        {
            byte* ptr = (byte*)&value;
            int writeOffset = (int)offset;

            for (int i = 0; i < sizeof(T); i++)
            {
                device.Write8(writeOffset++, *ptr++);
            }
        }

        // TODO: Maybe directly access a pointer provided by N64MemoryDevice class
        // TODO: Maybe the endianess controlled part should be done here?
        // TODO: Should the bounds check performed here because we know the real address?

        #region Data Access
        public sbyte S8(long address)
        {
            return (sbyte)U8(address);
        }

        public short S16(long address)
        {
            return (short)U16(address);
        }

        public int S32(long address)
        {
            return (int)U32(address);
        }

        public long S64(long address)
        {
            return (long)U64(address);
        }

        public byte DirectReadByte(long address) {
            return GetDevice(address, out long offset).Read8((int)offset);
        }

        public void DirectWriteByte(long address, byte value) {
            GetDevice(address, out long offset).Write8((int)offset, value);
        }

        public byte U8(long address)
        {
            return GetDevice(address, out long offset).Read8(N64Endianess.Address8((int)offset));
        }

        public ushort U16(long address)
        {
            var device = GetDevice(address, out long offset);

            if (AlignmetCheck(address, 2))
            {
                return N64Endianess.U16(device.Read16(N64Endianess.Address16((int)offset)));
            }
            else
            {
                return N64Endianess.U16(UnalignedRead<ushort>(device, N64Endianess.Address16(offset)));
            }
        }

        public uint U32(long address)
        {
            var device = GetDevice(address, out long offset);

            if (AlignmetCheck(address, 4))
            {
                return N64Endianess.U32(device.Read32((int)offset));
            }
            else
            {
                return N64Endianess.U32(UnalignedRead<uint>(device, offset));
            }
        }

        public ulong U64(long address)
        {
            var device = GetDevice(address, out long offset);

            if (AlignmetCheck(address, 8))
            {
                return N64Endianess.U64(device.Read64((int)offset));
            }
            else
            {
                return N64Endianess.U64(UnalignedRead<uint>(device, offset));
            }
        }

        public void S8(long address, sbyte value) { U8(address, (byte)value); }
        public void S16(long address, short value) { U16(address, (ushort)value); }
        public void S32(long address, int value) { U32(address, (uint)value); }
        public void S64(long address, long value) { U64(address, (ulong)value); }


        public void U8(long address, byte value)
        {
            GetDevice(address, out long offset).Write8(N64Endianess.Address8((int)offset), value);
        }

        public void U16(long address, ushort value)
        {
            var device = GetDevice(address, out long offset);

            value = N64Endianess.U16(value);

            if (AlignmetCheck(address, 2))
            {
                device.Write16(N64Endianess.Address16((int)offset)
                , value);
            }
            else
            {
                UnalignedWrite(device, N64Endianess.Address16(offset), value);
            }
        }

        public void U32(long address, uint value)
        {
            var device = GetDevice(address, out long offset);

            value = N64Endianess.U32(value);

            if (AlignmetCheck(address, 4))
            {
                device.Write32((int)offset, value);
            }
            else
            {
                UnalignedWrite(device, offset, value);
            }

            device.DispatchWriteNotifications((int)offset, 4);
        }

        public void U64(long address, ulong value)
        {
            var device = GetDevice(address, out long offset);

            value = N64Endianess.U64(value);

            if (AlignmetCheck(address, 8))
            {
                device.Write64((int)offset, value);
            }
            else
            {
                UnalignedWrite(device, offset, value);
            }
        }

        #endregion

        public void MountRcpInterfaces(
            SPInterface sp,
            DPCInterface dpc,
            N64MemoryDevice dps,
            MipsInterface mi,
            Video vi,
            Audio ai,
            ParallelInterface pi,
            SerialController si
        ) {
            SPRegs = sp;
            DPCmdRegs = dpc;
            DPSpanRegs = dps;
            MIRegs = mi;
            VIRegs = vi;
            AIRegs = ai;
            PIRegs = pi;
            SIRegs = si;
        }


        public Rdram RDRAM { get; private set; }

        public RdramRegisters RDRAMRegs { get; private set; }

        public SPInterface SPRegs { get; private set; }

        public DPCInterface DPCmdRegs { get; private set; }

        public N64MemoryDevice DPSpanRegs { get; private set; }

        public MipsInterface MIRegs { get; private set; }

        public Video VIRegs { get; private set; }

        public Audio AIRegs { get; private set; }

        public ParallelInterface PIRegs { get; private set; }

        public RdramInterface RIRegs { get; private set; }

        public SerialController SIRegs { get; private set; }

        public N64MemoryDevice Cart { get; private set; }

        public PIFController PIF { get; private set; }

        public DummyMemory DiskDriveRegisters { get; private set; }

        public DummyMemory Unused_0490 { get; private set; }
        public DummyMemory Unused_0600 { get; private set; }
        public DummyMemory Unused_1FD0 { get; private set; }
    }
}
