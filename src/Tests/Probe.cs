namespace cor64 {
    public static class Probe {
        public static uint Probe_ReadMem32(this N64System system, long address) {
            byte[] buffer = new byte[4];
            system.DeviceMemory.Read(address, buffer, 0, buffer.Length);
            
            #if LITTLE_ENDIAN
            return buffer.ToUInt32();
            #else
            return buffer.ToUInt32().ByteSwapped();
            #endif
        }
    }
}