using cor64.IO;

namespace cor64.Mips.Rsp {
    public class RspCompareFlag {

        public bool GetBool(int offset) {
            byte b = GetBootBit(offset);

            unsafe {
                return *(bool *)&b;
            }
        }

        public void SetBool(int offset, bool value) {
            unsafe {
                byte b = *(byte *)&value;
                SetBoolBit(offset, b);
            }
        }
        
        private void SetBoolBit(int offset, byte value) {
            Value &= (byte)~(1 << offset);
            Value |= (byte)((value & 1) << offset);
        }

        private byte GetBootBit(int offset) {
            return (byte)((Value >> offset) & 1);
        }

        public byte this [ int offset ] {
            get => GetBootBit(offset);
            set => SetBoolBit(offset, value);
        }

        public void Clear() {
            Value = 0;
        }

        public byte Value { get; set; }
    }
}