
using System.Runtime.CompilerServices;

namespace cor64.Mips.Rsp {
    public static class VectorLoadStoreHelper {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static long ComputeAlignedVectorAddress(uint baseAddress, int offset) {
            return (baseAddress + offset) & 0xFFF;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LoadByteIntoVector(DataMemory dataMemory, long address, RspVector vector, int element) {
            dataMemory.ReadData(address, 1);
            vector.PackedU8(element, dataMemory.Data8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StoreByteFromVector(DataMemory dataMemory, long address, RspVector vector, int element) {
            dataMemory.Data8 = vector.PackedU8(element);
            dataMemory.WriteData(address, 1);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LoadU16IntoVector(DataMemory dataMemory, long address, RspVector vector, int element) {
            dataMemory.ReadData(address, 2);
            vector.PackedU16(element, dataMemory.Data16);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StoreU16FromVector(DataMemory dataMemory, long address, RspVector vector, int element) {
            dataMemory.Data16 = vector.PackedU16(element);
            dataMemory.WriteData(address, 2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LoadU32IntoVector(DataMemory dataMemory, long address, RspVector vector, int element) {
            dataMemory.ReadData(address, 4);
            vector.PackedU32(element, dataMemory.Data32);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StoreU32FromVector(DataMemory dataMemory, long address, RspVector vector, int element) {
            dataMemory.Data32 = vector.PackedU32(element);
            dataMemory.WriteData(address, 4);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LoadDoubleIntoVector(DataMemory dataMemory, long address, RspVector vector, int element) {
            dataMemory.ReadData(address, 8);
            vector.PackedU64(element,  dataMemory.Data64);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StoreDoubleFromVector(DataMemory dataMemory, long address, RspVector vector, int element) {
            dataMemory.Data64 = vector.PackedU64(element);
            dataMemory.WriteData(address, 8);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LoadU128IntoVector(DataMemory dataMemory, long address, RspVector vector) {
            vector.QWord = dataMemory.ReadData128(address);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StoreU128FromVector(DataMemory dataMemory, long address, RspVector vector) {
            dataMemory.WriteData128(address, vector.QWord);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LoadPacked_Forth(DataMemory dataMemory, long address, RspVector vector, int element) {
            for (int i = 0; i < 4; i++) {
                dataMemory.ReadData(address + (i * 4), 1);
                vector.PackedU16(element + (i * 2), dataMemory.Data8);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StorePacked_Forth(DataMemory dataMemory, long address, RspVector vector, int element) {
            for (int i = 0; i < 4; i++) {
                dataMemory.Data8 = (byte)vector.PackedU16(element + (i * 2));
                dataMemory.WriteData(address + (i * 4), 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LoadPacked_Half(DataMemory dataMemory, long address, RspVector vector) {
            for (int i = 0; i < 8; i++) {
                dataMemory.ReadData(address + (i * 2), 1);
                vector.PackedU16(i * 2, dataMemory.Data8);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StorePacked_Half(DataMemory dataMemory, long address, RspVector vector) {
            for (int i = 0; i < 8; i++) {
                dataMemory.Data8 = (byte)vector.PackedU16(i * 2);
                dataMemory.WriteData(address + (i * 2), 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LoadPacked_Unsigned(DataMemory dataMemory, long address, RspVector vector) {
            for (int i = 0; i < 8; i++) {
                dataMemory.ReadData(address + i, 1);
                vector.PackedU16(i * 2, dataMemory.Data8);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StorePacked_Unsigned(DataMemory dataMemory, long address, RspVector vector) {
            for (int i = 0; i < 8; i++) {
                dataMemory.Data8 = (byte)vector.PackedU16(i * 2);
               dataMemory.WriteData(address + i, 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LoadPacked_Upper(DataMemory dataMemory, long address, RspVector vector) {
            for (int i = 0; i < 8; i++) {
                dataMemory.ReadData(address + i, 1);
                int value = dataMemory.Data8;
                value <<= 8;
                vector.PackedU16(i * 2, (ushort)value);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StorePacked_Upper(DataMemory dataMemory, long address, RspVector vector) {
            for (int i = 0; i < 8; i++) {
                dataMemory.Data8 = (byte)(vector.PackedU16(i * 2) >> 8);
                dataMemory.WriteData(address + i, 1);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void LoadTranposed(DataMemory dataMemory, long address, RspVector[] vecRegs, int target, int element) {
            int length = 8;

            if(length > 32 - target)
                length = 32 - target;

            address = ((address + 8) & 0xFF0) + (element & 1);

            for (int i = 0; i < length; i++) {
                var destElement = ((8 - (element >> 1) + i) << 1) & 0xF;

                dataMemory.ReadData(address++, 1);
                vecRegs[target + i].PackedU8(destElement,  dataMemory.Data8);

                dataMemory.ReadData(address++, 1);
                vecRegs[target + i].PackedU8(destElement + 1,  dataMemory.Data8);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void StoreTranposed(DataMemory dataMemory, long address, RspVector[] vecRegs, int target, int element) {
            int length = 8;

            if (length > 32 - target)
                length = 32 - target;

            length <<= 1;

            var destElement = element >> 1;

            for (int i = 0; i < length; i+=2) {
                    dataMemory.Data8 = vecRegs[target + destElement].PackedU8(i);
                    dataMemory.WriteData(address++, 1);

                    dataMemory.Data8 = vecRegs[target + destElement].PackedU8(i + 1);
                    dataMemory.WriteData(address++, 1);

                    destElement++;
                    destElement &= 7;
            }
        }
    }
}