
using System.Runtime.CompilerServices;
using cor64.IO;

namespace cor64.Mips.Rsp {
    public static class VectorLoadStoreHelper {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ComputeAlignedVectorAddress(uint baseAddress, int offset) {
            return (baseAddress + offset) & 0xFFF;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LoadByteIntoVector(IMemoryAccess dataMemory, long address, RspVector vector, int element) {
            vector.PackedU8(element, dataMemory.U8(address));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StoreByteFromVector(IMemoryAccess dataMemory, long address, RspVector vector, int element) {
            dataMemory.U8(address, vector.PackedU8(element));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LoadU16IntoVector(IMemoryAccess dataMemory, long address, RspVector vector, int element) {
            LoadDataBytesIntoVector(dataMemory, address, vector, element, 2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StoreU16FromVector(IMemoryAccess dataMemory, long address, RspVector vector, int element) {
            StoreDataBytesFromVector(dataMemory, address, vector, element, 2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LoadU32IntoVector(IMemoryAccess dataMemory, long address, RspVector vector, int element) {
            LoadDataBytesIntoVector(dataMemory, address, vector, element, 4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StoreU32FromVector(IMemoryAccess dataMemory, long address, RspVector vector, int element) {
            StoreDataBytesFromVector(dataMemory, address, vector, element, 4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LoadU64IntoVector(IMemoryAccess dataMemory, long address, RspVector vector, int element) {
            LoadDataBytesIntoVector(dataMemory, address, vector, element, 8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StoreU64FromVector(IMemoryAccess dataMemory, long address, RspVector vector, int element) {
            StoreDataBytesFromVector(dataMemory, address, vector, element, 8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LoadDataBytesIntoVector(IMemoryAccess dataMemory, long address, RspVector vector, int element, int dataLength) {
            if (dataLength > 1) {
                // Odd element specified, just return
                if ((element & 1) != 0) {
                    return;
                }

                for (int i = 0; i < dataLength; i++) {
                    vector.PackedU8((element + i) & 0xF, dataMemory.U8(address++ & 0xFFF));
                }
            }
            else {
                vector.PackedU8(element & 0xF, dataMemory.U8(address & 0xFFF));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StoreDataBytesFromVector(IMemoryAccess dataMemory, long address, RspVector vector, int element, int dataLength) {
            for (int i = 0; i < dataLength; i++) {
                dataMemory.U8(address++ & 0xFFF, vector.PackedU8((element + i) & 0xF));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LoadU128IntoVector(IMemoryAccess dataMemory, long address, RspVector vector, int element) {
           LoadDataBytesIntoVector(dataMemory, address, vector, element, 16);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StoreU128FromVector(IMemoryAccess dataMemory, long address, RspVector vector, int element) {
            StoreDataBytesFromVector(dataMemory, address, vector, element, 16);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LoadPacked_Forth(IMemoryAccess dataMemory, long address, RspVector vector, int element) {
            // TODO: Untested and likely broken and unused
            for (int i = 0; i < 4; i++) {
                vector.PackedU16(element + (i * 2), dataMemory.U8(address + (i * 4)));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StorePacked_Forth(IMemoryAccess dataMemory, long address, RspVector vector, int element) {
            // TODO: Untested and likely broken and unused
            for (int i = 0; i < 4; i++) {
                dataMemory.U8(address + (i * 4), (byte)vector.PackedU16(element + (i * 2)));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LoadPacked_Half(IMemoryAccess dataMemory, long address, RspVector vector) {
            for (int i = 0; i < 8; i++) {
                vector.PackedU16(i, dataMemory.U8(address + i));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StorePacked_Half(IMemoryAccess dataMemory, long address, RspVector vector) {
            for (int i = 0; i < 8; i++) {
                dataMemory.U8(address + i, (byte)vector.PackedU16(i));
            }
        }

        // [MethodImpl(MethodImplOptions.AggressiveInlining)]
        // public static void LoadPacked_Unsigned(IMemoryAccess dataMemory, long address, RspVector vector, int element) {
        //     long addr = address;
        //     if (element != 0) {
        //         for (int i = 0; i < 8; i++) {
        //             vector.PackedU16(i, (byte)(dataMemory.U16(addr & 0xFFF) << 7));
        //              addr -= 16 * ((element - i - 1 == 0x0) ? 1 : 0);
        //             ++addr;
        //         }
        //     }
        // }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StorePacked_Unsigned(IMemoryAccess dataMemory, long address, RspVector vector) {
            for (int i = 0; i < 8; i++) {
                dataMemory.U8(address + i, (byte)(vector.PackedU16(i) >> 7));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LoadPacked_Upper(IMemoryAccess dataMemory, long address, RspVector vector) {
            for (int i = 0; i < 8; i++) {
                vector.PackedU16(i, (ushort)(short)(dataMemory.U8(address + i) << 8));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StorePacked_Upper(IMemoryAccess dataMemory, long address, RspVector vector) {
            for (int i = 0; i < 8; i++) {
                dataMemory.U8(address + i, (byte)(vector.PackedU16(i) >> 8));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LoadTranposed(IMemoryAccess dataMemory, long address, RspVector[] vecRegs, int target, int element) {
            int length = 8;

            if(length > 32 - target)
                length = 32 - target;

            address = ((address + 8) & 0xFF0) + (element & 1);

            for (int i = 0; i < length; i++) {
                var destElement = ((8 - (element >> 1) + i) << 1) & 0xF;
                vecRegs[target + i].PackedU8(destElement,  dataMemory.U8(address++));
                vecRegs[target + i].PackedU8(destElement + 1,  dataMemory.U8(address++));
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StoreTranposed(IMemoryAccess dataMemory, long address, RspVector[] vecRegs, int target, int element) {
            int length = 8;

            if (length > 32 - target)
                length = 32 - target;

            length <<= 1;

            var destElement = element >> 1;

            for (int i = 0; i < length; i+=2) {
                    dataMemory.U8(address++, vecRegs[target + destElement].PackedU8(i));
                    dataMemory.U8(address++, vecRegs[target + destElement].PackedU8(i+1));
                    destElement++;
                    destElement &= 7;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StoreRest(IMemoryAccess dataMemory, long address, RspVector[] vecRegs, int target, int element) {
            int length = 8;

            throw new System.InvalidOperationException("SRV not yet working");

            // if (length > 32 - target)
            //     length = 32 - target;

            // length <<= 1;

            // var destElement = element >> 1;

            // for (int i = 0; i < length; i+=2) {
            //         dataMemory.Data8 = vecRegs[target + destElement].PackedU8(i);
            //         dataMemory.WriteData(address++, 1);

            //         dataMemory.Data8 = vecRegs[target + destElement].PackedU8(i + 1);
            //         dataMemory.WriteData(address++, 1);

            //         destElement++;
            //         destElement &= 7;
            // }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StoreWrapped(IMemoryAccess dataMemory, long address, RspVector[] vecRegs, int target, int element) {

            // TODO: Implement the store vector wrapped

            throw new System.InvalidOperationException("SWU not yet working");

            // int length = 8;

            // if (length > 32 - target)
            //     length = 32 - target;

            // length <<= 1;

            // for (int i = 0; i < length; i+=2) {
            //         int slice = (i + (element >> 1)) & 0x7;

            //         dataMemory.Data8 = vecRegs[target].PackedU8(slice);
            //         dataMemory.WriteData(address++, 1);

            //         dataMemory.Data8 = vecRegs[target].PackedU8(slice + 1);
            //         dataMemory.WriteData(address++, 1);
            // }
        }
    }
}