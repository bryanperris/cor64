using System;
using cor64.Mips.R4300I;

namespace cor64.HLE {
    public static class CpuStructHelper {
        public static byte DataRead8(this InterpreterBaseR4300I core, uint address) {
            return core.PhysicalMemory.U8(address);
        }

        public static uint DataRead32(this InterpreterBaseR4300I core, uint address) {
            return core.PhysicalMemory.U32(address);
        }

        public static ushort DataRead16(this InterpreterBaseR4300I core, uint address) {
            return core.PhysicalMemory.U16(address);
        }
    }
}