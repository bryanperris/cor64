namespace cor64 {
    public class IPLType {
        public IPLType(SecurityChipsetType cic, uint hashcode) {
            Cic = cic;
            HashCode = hashcode;
            Seed = 0;
            Name = "Unknown IPL";

            switch (cic)
            {
                default: break;
				case SecurityChipsetType.X101:
				case SecurityChipsetType.X102: Seed = 0x3F; break;
				case SecurityChipsetType.X103: Seed = 0x78; break;
				case SecurityChipsetType.X105: Seed = 0x91; break;
				case SecurityChipsetType.X106: Seed = 0x85; break;
            }

            switch (cic)
            {
				case SecurityChipsetType.Custom: Name = "Custom CIC"; break;
				case SecurityChipsetType.X101:   Name = "CIC-X101"; break;
				case SecurityChipsetType.X102:   Name = "CIC-X102"; break;
				case SecurityChipsetType.X103:   Name = "CIC-X103"; break;
				case SecurityChipsetType.X105:   Name = "CIC-X105"; break;
				case SecurityChipsetType.X106:   Name = "CIC-X106"; break;
                default: break;
            }
        }

        public SecurityChipsetType Cic { get; }

        public uint HashCode { get; }

        public uint Seed { get; }

        public string Name { get; }

        public override string ToString() {
            return Name;
        }
    }
}