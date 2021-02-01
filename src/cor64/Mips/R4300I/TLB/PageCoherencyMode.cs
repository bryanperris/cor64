namespace cor64.Mips.R4300I.TLB
{
    public enum PageCoherencyMode : byte
    {
        Reserved_0 = 0,
        Reserved_1 = 1,
        Uncached = 2,
        Noncoherent = 3,
        Exclusive = 4,
        Sharable = 5,
        Update = 6,
        Reserved_7 = 7
    }
}