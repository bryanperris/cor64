namespace cor64 {

    // CLEANUP: what is this even for?
    public static class Probe {
        // public static uint Probe_ReadMem32(this N64System system, long address) {
        //     byte[] buffer = new byte[4];
        //     system.SystemMemory.Read(address, buffer, 0, buffer.Length);
        //     uint read = buffer.ToUInt32();

        //     #if !HOST_LITTLE_ENDIAN
        //     read = read.ByteSwapped();
        //     #endif

        //     return read;
        // }
    }
}