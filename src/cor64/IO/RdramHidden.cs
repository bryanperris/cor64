namespace cor64.IO {
    public interface RdramHidden
    {
        void HiddenWrite(int address, byte value);

        byte HiddenRead(int address);

        int HiddenLength { get; }
    }
}