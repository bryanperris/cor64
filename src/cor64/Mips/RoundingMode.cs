using System;
namespace cor64.Mips
{
    public enum RoundingMode : byte
    {
        Nearest = 0,
        TowardZero,
        TowardPostive,
        TowardNegative
    }
}
