namespace cor64 {
    public class IPLType {
        public IPLType(LockoutChipType cic, uint hashcode) {
            Cic = cic;
            HashCode = hashcode;
            Seed = 0;
            Name = "Unknown IPL";

            switch (cic)
            {
                default: break;
				case LockoutChipType.X101:
				case LockoutChipType.X102: Seed = 0x3F; break;
				case LockoutChipType.X103: Seed = 0x78; break;
				case LockoutChipType.X105: Seed = 0x91; break;
				case LockoutChipType.X106: Seed = 0x85; break;
            }

            switch (cic)
            {
				case LockoutChipType.Custom: Name = "Custom CIC"; break;
				case LockoutChipType.X101:   Name = "CIC-NUS-X101"; break;
				case LockoutChipType.X102:   Name = "CIC-NUS-X102"; break;
				case LockoutChipType.X103:   Name = "CIC-NUS-X103"; break;
				case LockoutChipType.X105:   Name = "CIC-NUS-X105"; break;
				case LockoutChipType.X106:   Name = "CIC-NUS-X106"; break;
                default: break;
            }
        }

        public LockoutChipType Cic { get; }

        public uint HashCode { get; }

        public uint Seed { get; }

        public string Name { get; }

        public override string ToString() {
            return Name;
        }
    }
}