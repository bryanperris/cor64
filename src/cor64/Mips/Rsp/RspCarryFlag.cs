namespace cor64.Mips.Rsp {
    /// <summary>
    /// This is the RSP VCO (Vectory Carry Out) register
    /// </summary>
    public class RspCarryFlag {
        private readonly byte[] BITS = {
            0b00000001,
            0b00000010,
            0b00000100,
            0b00001000,
            0b00010000,
            0b00100000,
            0b01000000,
            0b10000000
        };

        public void SetCarryBit(int offset, byte value) {
            RegValue &= (ushort) ~(1 << offset);
            RegValue |= (ushort) ((1 & value) << offset);
        }

        public void SetCarryBit(int offset, bool value) {
            unsafe {
                SetCarryBit(offset, *(byte *)&value);
            }
        }

        public byte GetCarryBit(int offset) {
            var value = (byte)RegValue;
            value &= BITS[offset];
            value >>= offset;
            value &= 1;
            return value;
        }
        
        public bool GetCarryBitBool(int offset) {
            unsafe {
                var b = GetCarryBit(offset);
                return *(bool *)&b;
            }
        }

        public void SetNotEqualBit(int offset, byte value) {
            offset += 8;
            RegValue &= (ushort) ~(1 << offset);
            RegValue |= (ushort) ((1 & value) << offset);
        }

        public void SetNotEqualBit(int offset, bool value) {
            unsafe {
                SetNotEqualBit(offset, *(byte *)&value);
            }
        }

        public byte GetNotEqualBit(int offset) {
            var value = (byte)(RegValue >> 8);
            value &= BITS[offset];
            value >>= offset;
            value &= 1;
            return value;
        }

        public bool GetNotEqualBool(int offset) {
            unsafe {
                var b = GetNotEqualBit(offset);
                return *(bool *)&b;
            }
        }

        public void ClearCarryBits() {
            RegValue &= 0xFF00;
        }

        public void ClearNotEqualBits() {
            RegValue &= 0x00FF;
        }

        public void Clear() {
            RegValue = 0;
        }

        public ushort RegValue { get; set; }
    }
}