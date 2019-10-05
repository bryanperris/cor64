using System;
namespace cor64
{
    /* CIC Chipset types */
    public enum SecurityChipsetType
    {
		Unknown,
        Custom,
        X101,
        X102,
        X103,
        X105,
        X106
    }

	public static class SecurityChipsetTypeExtensions
    {
		public static String GetGoodName(this SecurityChipsetType type)
        {
            switch (type)
            {
				case SecurityChipsetType.Custom: return "Custom CIC";
				case SecurityChipsetType.X101: return "CIC-X101";
				case SecurityChipsetType.X102: return "CIC-X102";
				case SecurityChipsetType.X103: return "CIC-X103";
				case SecurityChipsetType.X105: return "CIC-X105";
				case SecurityChipsetType.X106: return "CIC-X106";
                default: return type.ToString();
            }
        }

		public static Int32 Seed(this SecurityChipsetType cic)
        {
            switch (cic)
            {
                default: return 0;
				case SecurityChipsetType.X101:
				case SecurityChipsetType.X102: return 0x3F;
				case SecurityChipsetType.X103: return 0x78;
				case SecurityChipsetType.X105: return 0x91;
				case SecurityChipsetType.X106: return 0x85;
            }
        }
    }
}
