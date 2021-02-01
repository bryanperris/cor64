using System;
using cor64.Mips.R4300I;

namespace cor64.HLE {
    public static class CpuStructHelper {
        public static byte DataRead8(this InterpreterBaseR4300I core, uint address) {
            core.DataMem.ReadData(address, 1);
            return core.DataMem.Data8;
        }

        public static uint DataRead32(this InterpreterBaseR4300I core, uint address) {
            core.DataMem.ReadData(address, 4);
            return core.DataMem.Data32;
        }

        public static ushort DataRead16(this InterpreterBaseR4300I core, uint address) {
            core.DataMem.ReadData(address, 2);
            return core.DataMem.Data16;
        }
    }
}